using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.CardSet;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class CardSetEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public CardSetEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetsAllCardSetsOfTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");

    // WHEN
    var response = await httpClient.GetAsync(CardSetsPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var cardSets = await response.Content.ReadFromJsonAsync<List<CardSetDto>>();
    var cardSet = Assert.Single(Assert.IsType<List<CardSetDto>>(cardSets));
    Assert.Equal(cardSetId, cardSet.Id);
    Assert.Equal("Characters", cardSet.Name);
  }

  [Fact]
  public async Task GetsCardCountsForCardSets()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    await testDao.AddCard(versionId, cardSetId, cardTypeId, "Knight");
    await testDao.AddCard(versionId, cardSetId, cardTypeId, "Wizard");

    // WHEN
    var response = await httpClient.GetAsync(CardSetsPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var cardSets = await response.Content.ReadFromJsonAsync<List<CardSetDto>>();
    Assert.Equal(2, Assert.Single(Assert.IsType<List<CardSetDto>>(cardSets)).CardCount);
  }

  [Fact]
  public async Task FailsToGetCardSetsWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await unauthorizedHttpClient.GetAsync(CardSetsPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToGetCardSetsFromForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    await testDao.AddCardSet(foreignVersionId, "Characters");

    // WHEN
    var response = await httpClient.GetAsync(CardSetsPath(foreignOrganizationId, foreignTitleId, foreignVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesCardSet()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardSetsPath(organizationId, titleId, versionId), new CardSetDto { Name = "Characters" });

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var created = await response.Content.ReadFromJsonAsync<CardSetDto>();
    Assert.NotNull(created?.Id);
    Assert.Equal("Characters", created.Name);
    Assert.Equal(created.Id, Assert.Single(testDao.QueryCardSetsByTitleVersion(versionId)).RowKey);
  }

  [Fact]
  public async Task FailsToCreateCardSetWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var dto = new CardSetDto { Name = new string('a', CardSetService.MaximumCardSetNameLength + 1) };

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardSetsPath(organizationId, titleId, versionId), dto);

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateCardSetWithWhitespaceName()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardSetsPath(organizationId, titleId, versionId), new CardSetDto { Name = " " });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumCardSetsInTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    for (var i = 0; i < CardSetService.MaximumCardSetsPerTitle; i++)
    {
      await testDao.AddCardSet(versionId, $"Card set {i}");
    }

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardSetsPath(organizationId, titleId, versionId), new CardSetDto { Name = "Excess" });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateCardSetWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await unauthorizedHttpClient.PostAsJsonAsync(CardSetsPath(organizationId, titleId, versionId), new CardSetDto { Name = "Characters" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Empty(testDao.QueryCardSetsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToCreateCardSetInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, foreignOrganizationId, "Foreign");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardSetsPath(foreignOrganizationId, foreignTitleId, foreignVersionId), new CardSetDto { Name = "Characters" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Empty(testDao.QueryCardSetsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task UpdatesCardSet()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync($"{CardSetsPath(organizationId, titleId, versionId)}/{cardSetId}", new CardSetDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("New", Assert.Single(testDao.QueryCardSetsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardSetWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardSetsPath(organizationId, titleId, versionId)}/{cardSetId}",
        new CardSetDto { Name = new string('a', CardSetService.MaximumCardSetNameLength + 1) });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardSetsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardSetWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Old");

    // WHEN
    var response = await unauthorizedHttpClient.PutAsJsonAsync(
        $"{CardSetsPath(organizationId, titleId, versionId)}/{cardSetId}",
        new CardSetDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardSetsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardSetInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardSetId = await testDao.AddCardSet(foreignVersionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardSetsPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{cardSetId}",
        new CardSetDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardSetsByTitleVersion(foreignVersionId)).Name);
  }

  [Fact]
  public async Task DeletesCardSet()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");

    // WHEN
    var response = await httpClient.DeleteAsync($"{CardSetsPath(organizationId, titleId, versionId)}/{cardSetId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardSetsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToDeleteCardSetWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");

    // WHEN
    var response = await unauthorizedHttpClient.DeleteAsync($"{CardSetsPath(organizationId, titleId, versionId)}/{cardSetId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Single(testDao.QueryCardSetsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToDeleteCardSetInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardSetId = await testDao.AddCardSet(foreignVersionId, "Characters");

    // WHEN
    var response = await httpClient.DeleteAsync($"{CardSetsPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{cardSetId}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Single(testDao.QueryCardSetsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task ChildVersionInheritsCardSetsAndKeepsParentValuesWhenUpdated()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(parentVersionId, "Characters");
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardSetsPath(organizationId, titleId, parentVersionId)}/{cardSetId}",
        new CardSetDto { Name = "Heroes" });
    var childResponse = await httpClient.GetAsync(CardSetsPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var cardSets = await childResponse.Content.ReadFromJsonAsync<List<CardSetDto>>();
    Assert.Equal("Characters", Assert.Single(Assert.IsType<List<CardSetDto>>(cardSets)).Name);
  }

  [Fact]
  public async Task ChildVersionKeepsCardSetWhenParentCardSetIsDeleted()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(parentVersionId, "Characters");
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var deleteResponse = await httpClient.DeleteAsync(
        $"{CardSetsPath(organizationId, titleId, parentVersionId)}/{cardSetId}");
    var childResponse = await httpClient.GetAsync(CardSetsPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
    var cardSets = await childResponse.Content.ReadFromJsonAsync<List<CardSetDto>>();
    var cardSet = Assert.Single(Assert.IsType<List<CardSetDto>>(cardSets));
    Assert.Equal(cardSetId, cardSet.Id);
    Assert.Equal("Characters", cardSet.Name);
  }

  [Fact]
  public async Task ParentCardSetCreatedAfterForkIsHiddenFromChildVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var createResponse = await httpClient.PostAsJsonAsync(
        CardSetsPath(organizationId, titleId, parentVersionId),
        new CardSetDto { Name = "Late" });
    var childResponse = await httpClient.GetAsync(CardSetsPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    var cardSets = await childResponse.Content.ReadFromJsonAsync<List<CardSetDto>>();
    Assert.Empty(Assert.IsType<List<CardSetDto>>(cardSets));
  }

  [Fact]
  public async Task DeletesCardSetsWhenDeletingTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var rootVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", rootVersionId);
    await testDao.AddCardSet(childVersionId, "Characters");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardSetsByTitleVersion(childVersionId));
  }

  [Fact]
  public async Task DeletesCardSetsWhenDeletingTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    await testDao.AddCardSet(versionId, "Characters");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardSetsByTitleVersion(versionId));
  }

  private static string CardSetsPath(string organizationId, string titleId, string versionId)
  {
    return $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/card-sets";
  }
}
