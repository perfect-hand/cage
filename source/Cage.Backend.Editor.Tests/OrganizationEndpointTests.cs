using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.Organization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class OrganizationEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public OrganizationEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetsAllOrganizations()
  {
    // GIVEN
    var organization1 = await testDao.AddOrganization("Organization1");
    await testDao.AddOrganization("Organization2");

    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organization1.RowKey, organization1.Name);

    // WHEN
    var response = await httpClient.GetAsync("/organizations");

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var organizations = await response.Content.ReadFromJsonAsync<List<OrganizationDto>>();

    Assert.NotNull(organizations);
    var foundOrganization = Assert.Single(organizations);
    Assert.Equal(organization1.RowKey, foundOrganization.Id);
    Assert.Equal(organization1.Name, foundOrganization.Name);
  }

  [Fact]
  public async Task FailsToGetAllOrganizationsWithoutAuthentication()
  {
    var response = await unauthorizedHttpClient.GetAsync("/organizations");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task CreatesOrganization()
  {
    // GIVEN
    var dto = new OrganizationDto
    {
      Name = "New Organization"
    };

    // WHEN
    var response = await httpClient.PostAsJsonAsync("/organizations", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var createdOrganization = await response.Content.ReadFromJsonAsync<OrganizationDto>();

    Assert.NotNull(createdOrganization);
    Assert.NotNull(createdOrganization.Id);
    Assert.Equal(dto.Name, createdOrganization.Name);

    var organizations = testDao.QueryOrganizationsByUser(TestAuthHandler.TestUser);
    Assert.NotNull(organizations);
    Assert.Contains(organizations, o => o.OrganizationId == createdOrganization.Id);
    Assert.Equal(dto.Name, testDao.QueryOrganization(createdOrganization.Id!)?.Name);
  }

  [Fact]
  public async Task CreatesOrganizationWithNameAtMaximumLength()
  {
    var dto = new OrganizationDto { Name = new string('a', OrganizationService.MaximumOrganizationNameLength) };

    var response = await httpClient.PostAsJsonAsync("/organizations", dto);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateOrganizationWithNameOverMaximumLength()
  {
    var dto = new OrganizationDto { Name = new string('a', OrganizationService.MaximumOrganizationNameLength + 1) };

    var response = await httpClient.PostAsJsonAsync("/organizations", dto);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumOrganizations()
  {
    for (var i = 0; i < OrganizationService.MaximumOrganizationsPerUser; i++)
    {
      await testDao.AddUserToOrganization(TestAuthHandler.TestUser, Guid.NewGuid().ToString(), $"Organization{i}");
    }

    var dto = new OrganizationDto { Name = "Another organization" };
    var response = await httpClient.PostAsJsonAsync("/organizations", dto);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateOrganizationsWithoutAuthentication()
  {
    // GIVEN
    var dto = new OrganizationDto
    {
      Name = "New Organization"
    };

    // WHEN
    var response = await unauthorizedHttpClient.PostAsJsonAsync("/organizations", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task UpdatesOrganizationName()
  {
    // GIVEN
    var organization = await testDao.AddOrganization("Old name");
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organization.RowKey, organization.Name);

    // WHEN
    var response = await httpClient.PutAsJsonAsync($"/organizations/{organization.RowKey}", new OrganizationDto { Name = "New name" });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("New name", testDao.QueryOrganization(organization.RowKey)?.Name);
    var userOrganizations = testDao.QueryOrganizationsByUser(TestAuthHandler.TestUser);
    Assert.Equal("New name", Assert.Single(userOrganizations).OrganizationName);
  }

  [Fact]
  public async Task FailsToUpdateOrganizationNameOverMaximumLength()
  {
    // GIVEN
    var organization = await testDao.AddOrganization("Organization");
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organization.RowKey, organization.Name);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organization.RowKey}",
        new OrganizationDto { Name = new string('a', OrganizationService.MaximumOrganizationNameLength + 1) });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToUpdateForeignOrganization()
  {
    // GIVEN
    var organization = await testDao.AddOrganization("Organization");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organization.RowKey}",
        new OrganizationDto { Name = "New name" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task DeletesOrganizationAndItsTitles()
  {
    // GIVEN
    var organization = await testDao.AddOrganization("Organization");
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organization.RowKey, organization.Name);
    await testDao.AddTitle(organization.RowKey, "Title");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organization.RowKey}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Null(testDao.QueryOrganization(organization.RowKey));
    Assert.Empty(testDao.QueryOrganizationsByUser(TestAuthHandler.TestUser));
    Assert.Empty(testDao.QueryTitlesByOrganization(organization.RowKey));
  }

  [Fact]
  public async Task FailsToDeleteForeignOrganization()
  {
    // GIVEN
    var organization = await testDao.AddOrganization("Organization");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organization.RowKey}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
