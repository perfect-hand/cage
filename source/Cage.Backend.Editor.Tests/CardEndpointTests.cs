using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.Card;
using Cage.Backend.Editor.CardSet;
using Cage.Backend.Editor.CardType;
using Cage.Simulation.Types;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class CardEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public CardEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetsCardsInCardSet()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    var cardId = await testDao.AddCard(versionId, cardSetId, cardTypeId, "Knight");

    // WHEN
    var response = await httpClient.GetAsync(CardsPath(organizationId, titleId, versionId, cardSetId));

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var cards = await response.Content.ReadFromJsonAsync<List<CardDto>>();
    var card = Assert.Single(Assert.IsType<List<CardDto>>(cards));
    Assert.Equal(cardId, card.Id);
    Assert.Equal("Knight", card.Name);
    Assert.Equal(cardTypeId, card.CardTypeId);
  }

  [Fact]
  public async Task FailsToGetCardsWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");

    // WHEN
    var response = await unauthorizedHttpClient.GetAsync(CardsPath(organizationId, titleId, versionId, cardSetId));

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToGetCardsFromForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardSetId = await testDao.AddCardSet(foreignVersionId, "Characters");

    // WHEN
    var response = await httpClient.GetAsync(CardsPath(foreignOrganizationId, foreignTitleId, foreignVersionId, cardSetId));

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesCardWithAttributesAndTags()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    await testDao.AddAttribute(versionId, "Health", CageType.Int);
    await testDao.AddTag(versionId, "Legendary");
    var dto = new CardDto
    {
      Name = "Knight",
      CardTypeId = cardTypeId,
      Attributes = new Dictionary<string, TypedValue> { ["Health"] = new(8) },
      Tags = ["Legendary", "Legendary"]
    };

    // WHEN
    var response = await httpClient.PostAsJsonAsync(CardsPath(organizationId, titleId, versionId, cardSetId), dto);

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var created = await response.Content.ReadFromJsonAsync<CardDto>();
    Assert.NotNull(created?.Id);
    Assert.Equal("Knight", created.Name);
    Assert.Equal(cardTypeId, created.CardTypeId);
    Assert.Equal(8, created.Attributes["Health"].AsInt());
    Assert.Equal(new[] { "Legendary" }, created.Tags);
    var stored = Assert.Single(testDao.QueryCardsByTitleVersion(versionId));
    Assert.Equal(created.Id, stored.RowKey);
    Assert.Equal(cardSetId, stored.CardSetId);
  }

  [Fact]
  public async Task FailsToCreateCardWithInvalidCardType()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardsPath(organizationId, titleId, versionId, cardSetId),
        new CardDto { Name = "Knight", CardTypeId = Guid.NewGuid().ToString() });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Empty(testDao.QueryCardsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToCreateCardWithAttributeOfWrongType()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    await testDao.AddAttribute(versionId, "Health", CageType.Int);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardsPath(organizationId, titleId, versionId, cardSetId),
        new CardDto
        {
          Name = "Knight",
          CardTypeId = cardTypeId,
          Attributes = new Dictionary<string, TypedValue> { ["Health"] = new("eight") }
        });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Empty(testDao.QueryCardsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToCreateCardWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardsPath(organizationId, titleId, versionId, cardSetId),
        new CardDto { Name = new string('a', CardService.MaximumCardNameLength + 1), CardTypeId = cardTypeId });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumCardsInCardSet()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    for (var i = 0; i < CardService.MaximumCardsPerCardSet; i++)
    {
      await testDao.AddCard(versionId, cardSetId, cardTypeId, $"Card{i}");
    }

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardsPath(organizationId, titleId, versionId, cardSetId),
        new CardDto { Name = "Excess", CardTypeId = cardTypeId });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateCardWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");

    // WHEN
    var response = await unauthorizedHttpClient.PostAsJsonAsync(
        CardsPath(organizationId, titleId, versionId, cardSetId),
        new CardDto { Name = "Knight", CardTypeId = cardTypeId });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Empty(testDao.QueryCardsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToCreateCardInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardSetId = await testDao.AddCardSet(foreignVersionId, "Characters");
    var cardTypeId = await testDao.AddCardType(foreignVersionId, "Hero");

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        CardsPath(foreignOrganizationId, foreignTitleId, foreignVersionId, cardSetId),
        new CardDto { Name = "Knight", CardTypeId = cardTypeId });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Empty(testDao.QueryCardsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task UpdatesCard()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    var cardId = await testDao.AddCard(versionId, cardSetId, cardTypeId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardsPath(organizationId, titleId, versionId, cardSetId)}/{cardId}",
        new CardDto { Name = "New", CardTypeId = cardTypeId });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("New", Assert.Single(testDao.QueryCardsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardSetId = await testDao.AddCardSet(foreignVersionId, "Characters");
    var cardTypeId = await testDao.AddCardType(foreignVersionId, "Hero");
    var cardId = await testDao.AddCard(foreignVersionId, cardSetId, cardTypeId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardsPath(foreignOrganizationId, foreignTitleId, foreignVersionId, cardSetId)}/{cardId}",
        new CardDto { Name = "New", CardTypeId = cardTypeId });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardsByTitleVersion(foreignVersionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    var cardId = await testDao.AddCard(versionId, cardSetId, cardTypeId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardsPath(organizationId, titleId, versionId, cardSetId)}/{cardId}",
        new CardDto
        {
          Name = new string('a', CardService.MaximumCardNameLength + 1),
          CardTypeId = cardTypeId
        });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateCardWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    var cardId = await testDao.AddCard(versionId, cardSetId, cardTypeId, "Old");

    // WHEN
    var response = await unauthorizedHttpClient.PutAsJsonAsync(
        $"{CardsPath(organizationId, titleId, versionId, cardSetId)}/{cardId}",
        new CardDto { Name = "New", CardTypeId = cardTypeId });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryCardsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task DeletesCard()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    var cardId = await testDao.AddCard(versionId, cardSetId, cardTypeId, "Knight");

    // WHEN
    var response = await httpClient.DeleteAsync($"{CardsPath(organizationId, titleId, versionId, cardSetId)}/{cardId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardsByTitleVersion(versionId));
  }

  [Fact]
  public async Task CannotDeleteCardSetContainingCards()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    await testDao.AddCard(versionId, cardSetId, cardTypeId, "Knight");

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/card-sets/{cardSetId}");

    // THEN
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    Assert.Single(testDao.QueryCardSetsByTitleVersion(versionId));
  }

  [Fact]
  public async Task CannotDeleteCardTypeAssignedToCards()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    await testDao.AddCard(versionId, cardSetId, cardTypeId, "Knight");

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/card-types/{cardTypeId}");

    // THEN
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    Assert.Single(testDao.QueryCardTypesByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToDeleteCardWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(versionId, "Characters");
    var cardTypeId = await testDao.AddCardType(versionId, "Hero");
    var cardId = await testDao.AddCard(versionId, cardSetId, cardTypeId, "Knight");

    // WHEN
    var response = await unauthorizedHttpClient.DeleteAsync(
        $"{CardsPath(organizationId, titleId, versionId, cardSetId)}/{cardId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Single(testDao.QueryCardsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToDeleteCardInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var cardSetId = await testDao.AddCardSet(foreignVersionId, "Characters");
    var cardTypeId = await testDao.AddCardType(foreignVersionId, "Hero");
    var cardId = await testDao.AddCard(foreignVersionId, cardSetId, cardTypeId, "Knight");

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"{CardsPath(foreignOrganizationId, foreignTitleId, foreignVersionId, cardSetId)}/{cardId}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Single(testDao.QueryCardsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task ChildVersionKeepsCardValuesWhenParentCardIsUpdated()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(parentVersionId, "Characters");
    var cardTypeId = await testDao.AddCardType(parentVersionId, "Hero");
    var cardId = await testDao.AddCard(parentVersionId, cardSetId, cardTypeId, "Knight");
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{CardsPath(organizationId, titleId, parentVersionId, cardSetId)}/{cardId}",
        new CardDto { Name = "Paladin", CardTypeId = cardTypeId });
    var childResponse = await httpClient.GetAsync(CardsPath(organizationId, titleId, childVersionId, cardSetId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    var cards = await childResponse.Content.ReadFromJsonAsync<List<CardDto>>();
    Assert.Equal("Knight", Assert.Single(Assert.IsType<List<CardDto>>(cards)).Name);
  }

  [Fact]
  public async Task ParentCardCreatedAfterForkIsHiddenFromChildVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var cardSetId = await testDao.AddCardSet(parentVersionId, "Characters");
    var cardTypeId = await testDao.AddCardType(parentVersionId, "Hero");
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var createResponse = await httpClient.PostAsJsonAsync(
        CardsPath(organizationId, titleId, parentVersionId, cardSetId),
        new CardDto { Name = "Knight", CardTypeId = cardTypeId });
    var childResponse = await httpClient.GetAsync(CardsPath(organizationId, titleId, childVersionId, cardSetId));

    // THEN
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    var cards = await childResponse.Content.ReadFromJsonAsync<List<CardDto>>();
    Assert.Empty(Assert.IsType<List<CardDto>>(cards));
  }

  [Fact]
  public async Task DeletesCardsWhenDeletingTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var rootVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", rootVersionId);
    var cardSetId = await testDao.AddCardSet(childVersionId, "Characters");
    var cardTypeId = await testDao.AddCardType(childVersionId, "Hero");
    await testDao.AddCard(childVersionId, cardSetId, cardTypeId, "Knight");

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryCardsByTitleVersion(childVersionId));
  }

  private static string CardsPath(string organizationId, string titleId, string versionId, string cardSetId)
  {
    return $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/card-sets/{cardSetId}/cards";
  }
}
