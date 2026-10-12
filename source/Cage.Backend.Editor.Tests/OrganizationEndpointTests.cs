using System.Net;
using System.Net.Http.Json;
using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class OrganizationEndpointTests : EndpointTestBase, IClassFixture<WebApplicationFactory<Program>>
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
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.UsersInOrganizationTableName);

    var organizationsTableClient = new TableClient(azuriteContainer.GetConnectionString(), OrganizationDao.OrganizationsTableName);
    var usersInOrganizationTableClient = new TableClient(azuriteContainer.GetConnectionString(), OrganizationDao.UsersInOrganizationTableName);

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

    await usersInOrganizationTableClient.AddEntityAsync(new UserInOrganizationEntity
    {
      PartitionKey = TestAuthHandler.TestUser,
      RowKey = organization1.RowKey,
      OrganizationId = organization1.RowKey,
      OrganizationName = organization1.Name
    });
    await usersInOrganizationTableClient.AddEntityAsync(new UserInOrganizationEntity
    {
      PartitionKey = organization1.RowKey,
      RowKey = TestAuthHandler.TestUser,
      OrganizationId = organization1.RowKey,
      OrganizationName = organization1.Name
    });

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
}
