using System.Net;
using System.Net.Http.Json;
using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

[Collection("Endpoint tests")]
public class TitleEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public TitleEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetsAllTitlesOfOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
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
    var organizationId = Guid.NewGuid().ToString();
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesTitle()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var tableServiceClient = new TableServiceClient(azuriteContainer.GetConnectionString());
    tableServiceClient.CreateTableIfNotExists(TitleDao.TitlesTableName);
    var titlesTableClient = new TableClient(azuriteContainer.GetConnectionString(), TitleDao.TitlesTableName);
    for (var i = 0; i < 99; i++)
    {
      await titlesTableClient.AddEntityAsync(new TitleEntity
      {
        PartitionKey = organizationId,
        RowKey = Guid.NewGuid().ToString(),
        Name = $"Title{i}"
      });
    }

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
  public async Task CreatesTitleWithNameAtMaximumLength()
  {
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var dto = new TitleDto { Name = new string('a', 50) };

    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTitleWithNameOverMaximumLength()
  {
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var dto = new TitleDto { Name = new string('a', 51) };

    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumTitlesInOrganization()
  {
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var tableServiceClient = new TableServiceClient(azuriteContainer.GetConnectionString());
    tableServiceClient.CreateTableIfNotExists(TitleDao.TitlesTableName);
    var titlesTableClient = new TableClient(azuriteContainer.GetConnectionString(), TitleDao.TitlesTableName);

    for (var i = 0; i < 100; i++)
    {
      await titlesTableClient.AddEntityAsync(new TitleEntity
      {
        PartitionKey = organizationId,
        RowKey = Guid.NewGuid().ToString(),
        Name = $"Title{i}"
      });
    }

    var dto = new TitleDto { Name = "Another title" };
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    var organizationId = Guid.NewGuid().ToString();
    var dto = new TitleDto { Name = "New Title" };
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
