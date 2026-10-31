using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

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
    var titleId = await testDao.AddTitle(organizationId, "Title1");
    await testDao.AddTitle(otherOrganizationId, "Other");

    // WHEN
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles");

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var titles = await response.Content.ReadFromJsonAsync<List<TitleDto>>();

    Assert.NotNull(titles);
    var foundTitle = Assert.Single(titles);
    Assert.Equal(titleId, foundTitle.Id);
    Assert.Equal("Title1", foundTitle.Name);
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
    var dto = new TitleDto { Name = "New Title" };

    // WHEN
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var createdTitle = await response.Content.ReadFromJsonAsync<TitleDto>();

    Assert.NotNull(createdTitle);
    Assert.NotNull(createdTitle.Id);
    Assert.Equal(dto.Name, createdTitle.Name);

    Assert.Contains(testDao.QueryTitlesByOrganization(organizationId), title => title.RowKey == createdTitle.Id);
  }

  [Fact]
  public async Task CreatesTitleWithNameAtMaximumLength()
  {
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var dto = new TitleDto { Name = new string('a', TitleService.MaximumTitleNameLength) };

    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTitleWithNameOverMaximumLength()
  {
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var dto = new TitleDto { Name = new string('a', TitleService.MaximumTitleNameLength + 1) };

    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles", dto);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumTitlesInOrganization()
  {
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    for (var i = 0; i < TitleService.MaximumTitlesPerOrganization; i++)
    {
      await testDao.AddTitle(organizationId, $"Title{i}");
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

  [Fact]
  public async Task UpdatesTitleName()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var titleId = await testDao.AddTitle(organizationId, "Old name");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}",
        new TitleDto { Name = "New name" });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var updatedTitle = Assert.Single(testDao.QueryTitlesByOrganization(organizationId));
    Assert.Equal("New name", updatedTitle.Name);
  }

  [Fact]
  public async Task FailsToUpdateTitleNameOverMaximumLength()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var titleId = await testDao.AddTitle(organizationId, "Title");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}",
        new TitleDto { Name = new string('a', TitleService.MaximumTitleNameLength + 1) });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToUpdateTitleWithoutAuthentication()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var titleId = await testDao.AddTitle(organizationId, "Title");

    // WHEN
    var response = await unauthorizedHttpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}",
        new TitleDto { Name = "New name" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("Title", Assert.Single(testDao.QueryTitlesByOrganization(organizationId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateTitleInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    var titleId = await testDao.AddTitle(organizationId, "Title");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}",
        new TitleDto { Name = "New name" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Equal("Title", Assert.Single(testDao.QueryTitlesByOrganization(organizationId)).Name);
  }

  [Fact]
  public async Task DeletesTitle()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var titleId = await testDao.AddTitle(organizationId, "Title");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryTitlesByOrganization(organizationId));
  }

  [Fact]
  public async Task FailsToDeleteTitleWithoutAuthentication()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var titleId = await testDao.AddTitle(organizationId, "Title");

    // WHEN
    var response = await unauthorizedHttpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Single(testDao.QueryTitlesByOrganization(organizationId));
  }

  [Fact]
  public async Task FailsToDeleteTitleInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    var titleId = await testDao.AddTitle(organizationId, "Title");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Single(testDao.QueryTitlesByOrganization(organizationId));
  }
}
