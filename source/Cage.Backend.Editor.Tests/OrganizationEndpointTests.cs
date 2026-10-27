using System.Net;
using System.Net.Http.Json;
using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;
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
    var tableServiceClient = new TableServiceClient(azuriteContainer.GetConnectionString());

    tableServiceClient.CreateTableIfNotExists(OrganizationDao.OrganizationsTableName);

    var organizationsTableClient = new TableClient(azuriteContainer.GetConnectionString(), OrganizationDao.OrganizationsTableName);

    var organization1 = new OrganizationEntity
    {
      PartitionKey = "Organization",
      RowKey = Guid.NewGuid().ToString(),
      Name = "Organization1"
    };
    var organization2 = new OrganizationEntity
    {
      PartitionKey = "Organization",
      RowKey = Guid.NewGuid().ToString(),
      Name = "Organization2"
    };

    await organizationsTableClient.AddEntityAsync(organization1);
    await organizationsTableClient.AddEntityAsync(organization2);

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

    var organizations = await httpClient.GetFromJsonAsync<List<OrganizationDto>>("/organizations");
    Assert.NotNull(organizations);
    Assert.Contains(organizations, o => o.Id == createdOrganization.Id);
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
    var organization = new OrganizationEntity
    {
      PartitionKey = "Organization",
      RowKey = Guid.NewGuid().ToString(),
      Name = "Old name"
    };
    await new OrganizationDao().AddOrganization(organization);
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organization.RowKey, organization.Name);

    var response = await httpClient.PutAsJsonAsync($"/organizations/{organization.RowKey}", new OrganizationDto { Name = "New name" });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var organizations = await httpClient.GetFromJsonAsync<List<OrganizationDto>>("/organizations");
    Assert.NotNull(organizations);
    var updatedOrganization = Assert.Single(organizations);
    Assert.Equal("New name", updatedOrganization.Name);
  }

  [Fact]
  public async Task FailsToUpdateOrganizationNameOverMaximumLength()
  {
    var organization = new OrganizationEntity
    {
      PartitionKey = "Organization",
      RowKey = Guid.NewGuid().ToString(),
      Name = "Organization"
    };
    await new OrganizationDao().AddOrganization(organization);
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organization.RowKey, organization.Name);

    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organization.RowKey}",
        new OrganizationDto { Name = new string('a', OrganizationService.MaximumOrganizationNameLength + 1) });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task DeletesOrganizationAndItsTitles()
  {
    var organization = new OrganizationEntity
    {
      PartitionKey = "Organization",
      RowKey = Guid.NewGuid().ToString(),
      Name = "Organization"
    };
    await new OrganizationDao().AddOrganization(organization);
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organization.RowKey, organization.Name);
    await testDao.AddTitle(organization.RowKey, "Title");

    var response = await httpClient.DeleteAsync($"/organizations/{organization.RowKey}");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var organizations = await httpClient.GetFromJsonAsync<List<OrganizationDto>>("/organizations");
    Assert.NotNull(organizations);
    Assert.Empty(organizations);

    var titlesTableClient = new TableClient(azuriteContainer.GetConnectionString(), TitleDao.TitlesTableName);
    Assert.Empty(titlesTableClient.Query<TitleEntity>(entity => entity.PartitionKey == organization.RowKey));
  }

  [Fact]
  public async Task FailsToDeleteForeignOrganization()
  {
    var organization = new OrganizationEntity
    {
      PartitionKey = "Organization",
      RowKey = Guid.NewGuid().ToString(),
      Name = "Organization"
    };
    await new OrganizationDao().AddOrganization(organization);

    var response = await httpClient.DeleteAsync($"/organizations/{organization.RowKey}");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
