using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Card;

public class CardDao : VersionedTitleEntityDao<CardEntity>
{
  public const string CardsTableName = "Cards";

  private readonly TableClient cardsTableClient;

  public CardDao()
  {
    cardsTableClient = CreateTableClient(CardsTableName);
  }

  public override async Task Add(CardEntity card)
  {
    await cardsTableClient.AddEntityAsync(card);
  }

  public override Task<List<CardEntity>> QueryByTitleVersion(string titleVersionId)
  {
    return Task.FromResult(cardsTableClient.Query<CardEntity>(entity => entity.PartitionKey == titleVersionId).ToList());
  }

  public override async Task<CardEntity?> Get(string titleVersionId, string cardId)
  {
    var response = await cardsTableClient.GetEntityIfExistsAsync<CardEntity>(titleVersionId, cardId);
    return response.HasValue ? response.Value : null;
  }

  public override async Task Update(string titleVersionId, CardEntity card)
  {
    card.PartitionKey = titleVersionId;
    await cardsTableClient.UpsertEntityAsync(card, TableUpdateMode.Replace);
  }

  public override async Task<bool> Delete(string titleVersionId, string cardId)
  {
    var card = await cardsTableClient.GetEntityIfExistsAsync<CardEntity>(titleVersionId, cardId);
    if (!card.HasValue)
    {
      return false;
    }

    await cardsTableClient.DeleteEntityAsync(titleVersionId, cardId);
    return true;
  }

  public override async Task DeleteByTitleVersion(string titleVersionId)
  {
    foreach (var card in await QueryByTitleVersion(titleVersionId))
    {
      await cardsTableClient.DeleteEntityAsync(titleVersionId, card.RowKey);
    }
  }
}
