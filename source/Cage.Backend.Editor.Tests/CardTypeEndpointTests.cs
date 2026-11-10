using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.CardType;
using Cage.Simulation.Types;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class CardTypeEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public CardTypeEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetsAllCardTypesOfTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(versionId, "Resource");

    // WHEN
    var response = await httpClient.GetAsync(CardTypesPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var cardTypes = await response.Content.ReadFromJsonAsync<List<CardTypeDto>>();
    var cardType = Assert.Single(Assert.IsType<List<CardTypeDto>>(cardTypes));
    Assert.Equal(cardTypeId, cardType.Id);
    Assert.Equal("Resource", cardType.Name);
  }

  [Fact]
  public async Task FailsToGetCardTypesWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await unauthorizedHttpClient.GetAsync(CardTypesPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToGetCardTypesOfTitleInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Foreign");
    var titleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.GetAsync(CardTypesPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesCardTypeWithAttributesAndTags()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    await testDao.AddAttribute(versionId, "Costs", CageType.Int);
    await testDao.AddTag(versionId, "Resource");
    var dto = new CardTypeDto
    {
      Name = "Monster",
      Attributes = new Dictionary<string, TypedValue> { ["Costs"] = new(1) },
      Tags = ["Resource", "Resource"]
    };

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardTypesPath(organizationId, titleId, versionId), dto);

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var created = await response.Content.ReadFromJsonAsync<CardTypeDto>();
    Assert.NotNull(created?.Id);
    Assert.Equal("Monster", created.Name);
    Assert.Equal(1, created.Attributes["Costs"].AsInt());
    Assert.Equal(new[] { "Resource" }, created.Tags);
    var stored = Assert.Single(testDao.QueryCardTypesByTitleVersion(versionId));
    Assert.Equal(created.Id, stored.RowKey);
  }

  [Fact]
  public async Task FailsToCreateCardTypeWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var dto = new CardTypeDto
    {
      Name = new string('a', CardTypeService.MaximumCardTypeNameLength + 1)
    };

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardTypesPath(organizationId, titleId, versionId), dto);

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateCardTypeWithWhitespaceName()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardTypesPath(organizationId, titleId, versionId),
        new CardTypeDto { Name = " " });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumCardTypesInTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    for (var i = 0; i < CardTypeService.MaximumCardTypesPerTitle; i++)
    {
      await testDao.AddCardType(versionId, $"CardType{i}");
    }

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardTypesPath(organizationId, titleId, versionId),
        new CardTypeDto { Name = "Excess" });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateCardTypeWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await unauthorizedHttpClient.PostAsJsonAsync(
        CardTypesPath(organizationId, titleId, versionId),
        new CardTypeDto { Name = "Resource" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Empty(testDao.QueryCardTypesByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToCreateCardTypeInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, foreignOrganizationId, "Foreign");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardTypesPath(foreignOrganizationId, foreignTitleId, foreignVersionId),
        new CardTypeDto { Name = "Resource" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Empty(testDao.QueryCardTypesByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task UpdatesCardType()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(versionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardTypesPath(organizationId, titleId, versionId)}/{cardTypeId}",
        new CardTypeDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("New", Assert.Single(testDao.QueryCardTypesByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardTypeWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(versionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardTypesPath(organizationId, titleId, versionId)}/{cardTypeId}",
        new CardTypeDto { Name = new string('a', CardTypeService.MaximumCardTypeNameLength + 1) });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardTypesByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardTypeWithCyclicParentReference()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var parentId = await testDao.AddCardType(versionId, "Parent");
    var childId = await testDao.AddCardType(versionId, "Child", parentId);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardTypesPath(organizationId, titleId, versionId)}/{parentId}",
        new CardTypeDto { Name = "Parent", ParentId = childId });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Null(testDao.QueryCardTypesByTitleVersion(versionId).Single(cardType => cardType.RowKey == parentId).ParentId);
  }

  [Fact]
  public async Task FailsToUpdateCardTypeWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(versionId, "Old");

    // WHEN
    var response = await unauthorizedHttpClient.PutAsJsonAsync(
        $"{CardTypesPath(organizationId, titleId, versionId)}/{cardTypeId}",
        new CardTypeDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardTypesByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardTypeInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardTypeId = await testDao.AddCardType(foreignVersionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardTypesPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{cardTypeId}",
        new CardTypeDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardTypesByTitleVersion(foreignVersionId)).Name);
  }

  [Fact]
  public async Task DeletesCardType()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(versionId, "Resource");

    // WHEN
    var response = await httpClient.DeleteAsync($"{CardTypesPath(organizationId, titleId, versionId)}/{cardTypeId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardTypesByTitleVersion(versionId));
  }

  [Fact]
  public async Task ChildVersionKeepsParentCardTypeWhenParentCardTypeIsDeleted()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(parentVersionId, "Monster");
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var deleteResponse = await httpClient.DeleteAsync(
        $"{CardTypesPath(organizationId, titleId, parentVersionId)}/{cardTypeId}");
    var childResponse = await httpClient.GetAsync(CardTypesPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    var childCardTypes = await childResponse.Content.ReadFromJsonAsync<List<CardTypeDto>>();
    var childCardType = Assert.Single(Assert.IsType<List<CardTypeDto>>(childCardTypes));
    Assert.Equal(cardTypeId, childCardType.Id);
    Assert.Equal("Monster", childCardType.Name);
  }

  [Fact]
  public async Task FailsToDeleteCardTypeWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(versionId, "Resource");

    // WHEN
    var response = await unauthorizedHttpClient.DeleteAsync($"{CardTypesPath(organizationId, titleId, versionId)}/{cardTypeId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Single(testDao.QueryCardTypesByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToDeleteCardTypeInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardTypeId = await testDao.AddCardType(foreignVersionId, "Resource");

    // WHEN
    var response = await httpClient.DeleteAsync($"{CardTypesPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{cardTypeId}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Single(testDao.QueryCardTypesByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task FailsToDeleteCardTypeWithChildCardTypes()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var parentId = await testDao.AddCardType(versionId, "Parent");
    await testDao.AddCardType(versionId, "Child", parentId);

    // WHEN
    var response = await httpClient.DeleteAsync($"{CardTypesPath(organizationId, titleId, versionId)}/{parentId}");

    // THEN
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    Assert.Equal(2, testDao.QueryCardTypesByTitleVersion(versionId).Count);
  }

  [Fact]
  public async Task ParentCardTypeCreatedAfterForkIsHiddenFromChildVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var createResponse = await httpClient.PostAsJsonAsync(
        CardTypesPath(organizationId, titleId, parentVersionId),
        new CardTypeDto { Name = "Late" });
    var childResponse = await httpClient.GetAsync(CardTypesPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    Assert.Empty(Assert.IsType<List<CardTypeDto>>(await childResponse.Content.ReadFromJsonAsync<List<CardTypeDto>>()));
  }

  [Fact]
  public async Task ChildVersionKeepsParentCardTypeValueWhenParentIsUpdated()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var cardTypeId = await testDao.AddCardType(parentVersionId, "Resource");
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync(
        $"{CardTypesPath(organizationId, titleId, parentVersionId)}/{cardTypeId}",
        new CardTypeDto { Name = "Monster" });
    var childResponse = await httpClient.GetAsync(CardTypesPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    var childCardTypes = await childResponse.Content.ReadFromJsonAsync<List<CardTypeDto>>();
    Assert.Equal("Resource", Assert.Single(Assert.IsType<List<CardTypeDto>>(childCardTypes)).Name);
  }

  [Fact]
  public async Task DeletesCardTypesWhenDeletingTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var rootVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", rootVersionId);
    await testDao.AddCardType(childVersionId, "Monster");

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardTypesByTitleVersion(childVersionId));
  }

  [Fact]
  public async Task DeletesCardTypesWhenDeletingTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    await testDao.AddCardType(versionId, "Resource");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardTypesByTitleVersion(versionId));
  }

  private static string CardTypesPath(string organizationId, string titleId, string versionId)
  {
    return $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/card-types";
  }
}
