using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.Attribute;
using Cage.Simulation.Types;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class AttributeEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public AttributeEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetsAllAttributesOfTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Health", CageType.Int);
    await testDao.AddAttribute(Guid.NewGuid().ToString(), "Other", CageType.String);

    // WHEN
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/attributes");

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var attributes = await response.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.NotNull(attributes);
    var attribute = Assert.Single(attributes);
    Assert.Equal(attributeId, attribute.Id);
    Assert.Equal("Health", attribute.Name);
    Assert.Equal(CageType.Int, attribute.Type);
  }

  [Fact]
  public async Task FailsToGetAllAttributesWithoutAuthentication()
  {
    // GIVEN
    var organizationId = Guid.NewGuid();
    var titleId = Guid.NewGuid();

    // WHEN
    var response = await unauthorizedHttpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/attributes");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToGetAllAttributesOfTitleInOtherOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");

    // WHEN
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{foreignTitleId}/attributes");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesAttribute()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var dto = new AttributeDto { Name = "Health", Type = CageType.Int };

    // WHEN
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles/{titleId}/attributes", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var created = await response.Content.ReadFromJsonAsync<AttributeDto>();
    Assert.NotNull(created);
    Assert.NotNull(created.Id);
    Assert.Equal("Health", created.Name);
    Assert.Equal(CageType.Int, created.Type);

    var stored = Assert.Single(testDao.QueryAttributesByTitle(titleId));
    Assert.Equal(created.Id, stored.RowKey);
    Assert.Equal(CageType.Int, stored.Type);
  }

  [Fact]
  public async Task FailsToCreateAttributeWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var dto = new AttributeDto { Name = new string('a', AttributeService.MaximumAttributeNameLength + 1), Type = CageType.Int };

    // WHEN
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles/{titleId}/attributes", dto);

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumAttributesInTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    for (var i = 0; i < AttributeService.MaximumAttributesPerTitle; i++)
    {
      await testDao.AddAttribute(titleId, $"Attribute{i}", CageType.Int);
    }

    var dto = new AttributeDto { Name = "Another", Type = CageType.Int };

    // WHEN
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles/{titleId}/attributes", dto);

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateAttributeWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var dto = new AttributeDto { Name = "Health", Type = CageType.Int };

    // WHEN
    var response = await unauthorizedHttpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles/{titleId}/attributes", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Empty(testDao.QueryAttributesByTitle(titleId));
  }

  [Fact]
  public async Task FailsToCreateAttributeInForeignOrganization()
  {
    // GIVEN
    var dto = new AttributeDto { Name = "Health", Type = CageType.Int };

    // WHEN
    var response = await httpClient.PostAsJsonAsync($"/organizations/{Guid.NewGuid()}/titles/{Guid.NewGuid()}/attributes", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task UpdatesAttribute()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Old", CageType.Int);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/attributes/{attributeId}",
        new AttributeDto { Name = "New", Type = CageType.String });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var updated = Assert.Single(testDao.QueryAttributesByTitle(titleId));
    Assert.Equal("New", updated.Name);
    Assert.Equal(CageType.String, updated.Type);
  }

  [Fact]
  public async Task FailsToUpdateAttributeWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Old", CageType.Int);
    var dto = new AttributeDto { Name = new string('a', AttributeService.MaximumAttributeNameLength + 1), Type = CageType.Int };

    // WHEN
    var response = await httpClient.PutAsJsonAsync($"/organizations/{organizationId}/titles/{titleId}/attributes/{attributeId}", dto);

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryAttributesByTitle(titleId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateAttributeWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Old", CageType.Int);

    // WHEN
    var response = await unauthorizedHttpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/attributes/{attributeId}",
        new AttributeDto { Name = "New", Type = CageType.String });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryAttributesByTitle(titleId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateAttributeOfTitleInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var attributeId = await testDao.AddAttribute(foreignTitleId, "Old", CageType.Int);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{foreignTitleId}/attributes/{attributeId}",
        new AttributeDto { Name = "New", Type = CageType.String });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryAttributesByTitle(foreignTitleId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateMissingAttribute()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/attributes/{Guid.NewGuid()}",
        new AttributeDto { Name = "New", Type = CageType.String });

    // THEN
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task DeletesAttribute()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Health", CageType.Int);

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}/attributes/{attributeId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryAttributesByTitle(titleId));
  }

  [Fact]
  public async Task FailsToDeleteAttributeWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Health", CageType.Int);

    // WHEN
    var response = await unauthorizedHttpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}/attributes/{attributeId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Single(testDao.QueryAttributesByTitle(titleId));
  }

  [Fact]
  public async Task FailsToDeleteAttributeOfTitleInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var attributeId = await testDao.AddAttribute(foreignTitleId, "Health", CageType.Int);

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{foreignTitleId}/attributes/{attributeId}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Single(testDao.QueryAttributesByTitle(foreignTitleId));
  }

  [Fact]
  public async Task DeletesAttributesWhenDeletingTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    await testDao.AddAttribute(titleId, "Health", CageType.Int);

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryAttributesByTitle(titleId));
  }
}
