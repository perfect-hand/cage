using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Types;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class TitleVersionEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public TitleVersionEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task ChangesToVersionAttributesDoNotAffectParentVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Health", CageType.Int);
    var createResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Draft", ParentVersionId = titleId });
    var createdVersion = await createResponse.Content.ReadFromJsonAsync<TitleVersionDto>();
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    Assert.NotNull(createdVersion?.Id);

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{createdVersion.Id}/attributes/{attributeId}",
        new AttributeDto { Name = "Health Points", Type = CageType.String });
    var createAttributeResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{createdVersion.Id}/attributes",
        new AttributeDto { Name = "Armor", Type = CageType.Int });
    var deleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{createdVersion.Id}/attributes/{attributeId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, createAttributeResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    var parentAttributes = Assert.Single(testDao.QueryAttributesByTitle(titleId));
    Assert.Equal(attributeId, parentAttributes.RowKey);
    Assert.Equal("Health", parentAttributes.Name);
    var childAttributesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{createdVersion.Id}/attributes");
    var childAttributes = await childAttributesResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.Equal(HttpStatusCode.OK, childAttributesResponse.StatusCode);
    Assert.Equal("Armor", Assert.Single(Assert.IsType<List<AttributeDto>>(childAttributes)).Name);
  }

  [Fact]
  public async Task ChangesToParentAfterForkDoNotAffectChildVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var attributeId = await testDao.AddAttribute(titleId, "Health", CageType.Int);
    var createResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Draft", ParentVersionId = titleId });
    var version = await createResponse.Content.ReadFromJsonAsync<TitleVersionDto>();
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    Assert.NotNull(version?.Id);
    await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{version.Id}/attributes",
        new AttributeDto { Name = "Armor", Type = CageType.Int });

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/attributes/{attributeId}",
        new AttributeDto { Name = "Updated Health", Type = CageType.String });
    var childResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{version.Id}/attributes");
    var childAttributes = await childResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
    var childAttributeList = Assert.IsType<List<AttributeDto>>(childAttributes);
    Assert.Equal(2, childAttributeList.Count);
    var childAttribute = Assert.Single(childAttributeList, attribute => attribute.Id == attributeId);
    Assert.Equal(attributeId, childAttribute.Id);
    Assert.Equal("Health", childAttribute.Name);
    Assert.Equal(CageType.Int, childAttribute.Type);
    var deleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{version.Id}/attributes/{attributeId}");
    var emptyChildResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{version.Id}/attributes");
    var emptyChildAttributes = await emptyChildResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    Assert.Equal("Armor", Assert.Single(Assert.IsType<List<AttributeDto>>(emptyChildAttributes)).Name);
    Assert.Equal("Updated Health", Assert.Single(testDao.QueryAttributesByTitle(titleId)).Name);
  }

  [Fact]
  public async Task DeletingVersionDoesNotChangeOtherVersions()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    await testDao.AddAttribute(titleId, "Health", CageType.Int);
    var createResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Draft", ParentVersionId = titleId });
    var version = await createResponse.Content.ReadFromJsonAsync<TitleVersionDto>();
    Assert.NotNull(version?.Id);
    await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{version.Id}/attributes",
        new AttributeDto { Name = "Armor", Type = CageType.Int });
    var childResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Next", ParentVersionId = version.Id });
    var child = await childResponse.Content.ReadFromJsonAsync<TitleVersionDto>();
    Assert.NotNull(child?.Id);

    // WHEN
    var deleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{version.Id}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    var versionsResponse = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");
    var versions = await versionsResponse.Content.ReadFromJsonAsync<List<TitleVersionDto>>();
    Assert.Contains(Assert.IsType<List<TitleVersionDto>>(versions), item => item.Id == child.Id);
    var childAttributesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{child.Id}/attributes");
    var childAttributes = await childAttributesResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.Equal(2, Assert.IsType<List<AttributeDto>>(childAttributes).Count);
    var childDeleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{child.Id}");
    Assert.Equal(HttpStatusCode.NoContent, childDeleteResponse.StatusCode);
    var remainingVersionsResponse = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");
    var remainingVersions = await remainingVersionsResponse.Content.ReadFromJsonAsync<List<TitleVersionDto>>();
    Assert.Single(Assert.IsType<List<TitleVersionDto>>(remainingVersions));
    var parentAttributesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{titleId}/attributes");
    var parentAttributes = await parentAttributesResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.Equal("Health", Assert.Single(Assert.IsType<List<AttributeDto>>(parentAttributes)).Name);
  }

  [Fact]
  public async Task RenamesVersionAndRejectsRequestsWithoutOrganizationAccess()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var createResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Draft", ParentVersionId = titleId });
    var version = await createResponse.Content.ReadFromJsonAsync<TitleVersionDto>();
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    Assert.NotNull(version?.Id);

    // WHEN
    var renameResponse = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{version.Id}",
        new TitleVersionDto { Name = "Release" });
    var unauthorizedResponse = await unauthorizedHttpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions");
    var foreignResponse = await httpClient.GetAsync(
        $"/organizations/{Guid.NewGuid()}/titles/{titleId}/versions");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, renameResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
    var versionsResponse = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");
    var versions = await versionsResponse.Content.ReadFromJsonAsync<List<TitleVersionDto>>();
    Assert.Contains(Assert.IsType<List<TitleVersionDto>>(versions), item => item.Id == version.Id && item.Name == "Release");
  }

  [Fact]
  public async Task CannotDeleteTheOnlyVersionOfATitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    var initialVersion = Assert.Single(testDao.QueryVersionsByTitle(titleId));
    Assert.Equal("1.0", initialVersion.Name);
    Assert.False(initialVersion.IsDeleted);
  }

  [Fact]
  public async Task ReadOnlyVersionCannotBeChangedButCanBeUsedAsParent()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var lockResponse = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{titleId}/read-only",
        new TitleVersionReadOnlyDto { IsReadOnly = true });

    // WHEN
    var rejectedResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{titleId}/attributes",
        new AttributeDto { Name = "Health", Type = CageType.Int });
    var childResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Next", ParentVersionId = titleId });
    var child = await childResponse.Content.ReadFromJsonAsync<TitleVersionDto>();
    var acceptedResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{child?.Id}/attributes",
        new AttributeDto { Name = "Health", Type = CageType.Int });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Conflict, rejectedResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, childResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, acceptedResponse.StatusCode);
  }
}
