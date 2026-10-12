using System.Net;
using System.Net.Http.Json;
using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class TitleEndpointTests : EndpointTestBase, IClassFixture<WebApplicationFactory<Program>>
{
  public TitleEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  private async Task<string> AddOrganizationWithTestUser(bool withUser = true)
  {
    var organizationId = Guid.NewGuid().ToString();
    if (withUser)
    {
      var tableServiceClient = new TableServiceClient(azuriteContainer.GetConnectionString());
      tableServiceClient.CreateTableIfNotExists(OrganizationDao.UsersInOrganizationTableName);
      var usersInOrganizationTableClient = new TableClient(azuriteContainer.GetConnectionString(), OrganizationDao.UsersInOrganizationTableName);
      await usersInOrganizationTableClient.AddEntityAsync(new UserInOrganizationEntity
      {
        PartitionKey = organizationId,
        RowKey = TestAuthHandler.TestUser,
        OrganizationId = organizationId,
        OrganizationName = "Organization"
      });
    }
    return organizationId;
  }

  [Fact]
  public async Task GetsAllTitlesOfOrganization()
  {
    // GIVEN
    var organizationId = await AddOrganizationWithTestUser();
    var otherOrganizationId = Guid.NewGuid().ToString();

    var tableServiceClient = new TableServiceClient(azuriteContainer.GetConnectionString());
    tableServiceClient.CreateTableIfNotExists(TitleDao.TitlesTableName);
    var titlesTableClient = new TableClient(azuriteContainer.GetConnectionString(), TitleDao.TitlesTableName);

    var title = new TitleEntity { PartitionKey = organizationId, RowKey = Guid.NewGuid().ToString(), Name = "Title1" };
    await titlesTableClient.AddEntityAsync(title);
    await titlesTableClient.AddEntityAsync(new TitleEntity { PartitionKey = otherOrganizationId, RowKey = Guid.NewGuid().ToString(), Name = "Other" });

    // WHEN
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles");

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var titles = await response.Content.ReadFromJsonAsync<List<TitleDto>>();

    Assert.NotNull(titles);
    var foundTitle = Assert.Single(titles);
    Assert.Equal(title.RowKey, foundTitle.Id);
    Assert.Equal(title.Name, foundTitle.Name);
  }

  [Fact]
  public async Task FailsToGetAllTitlesWithoutAuthentication()
  {
    var response = await unauthorizedHttpClient.GetAsync($"/organizations/{Guid.NewGuid()}/titles");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToGetAllTitlesOfForeignOrganization()
  {
    var organizationId = await AddOrganizationWithTestUser(withUser: false);
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesTitle()
  {
    // GIVEN
    var organizationId = await AddOrganizationWithTestUser();
    var dto = new TitleDto { Name = "New Title" };

    // WHEN
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var createdTitle = await response.Content.ReadFromJsonAsync<TitleDto>();

    Assert.NotNull(createdTitle);
    Assert.NotNull(createdTitle.Id);
    Assert.Equal(dto.Name, createdTitle.Name);

    var titles = await httpClient.GetFromJsonAsync<List<TitleDto>>($"/organizations/{organizationId}/titles");
    Assert.NotNull(titles);
    Assert.Contains(titles, t => t.Id == createdTitle.Id);
  }

  [Fact]
  public async Task FailsToCreateTitleWithoutAuthentication()
  {
    var dto = new TitleDto { Name = "New Title" };
    var response = await unauthorizedHttpClient.PostAsJsonAsync($"/organizations/{Guid.NewGuid()}/titles", dto);
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTitleInForeignOrganization()
  {
    var organizationId = await AddOrganizationWithTestUser(withUser: false);
    var dto = new TitleDto { Name = "New Title" };
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
