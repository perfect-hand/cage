using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.CardSet;

public class CardSetDao : VersionedTitleEntityDao<CardSetEntity>
{
  public const string CardSetsTableName = "CardSets";

  private readonly TableClient cardSetsTableClient;

  public CardSetDao()
  {
    cardSetsTableClient = CreateTableClient(CardSetsTableName);
  }

  public override async Task Add(CardSetEntity cardSet)
  {
    await cardSetsTableClient.AddEntityAsync(cardSet);
  }

  public override Task<List<CardSetEntity>> QueryByTitleVersion(string titleVersionId)
  {
    return Task.FromResult(cardSetsTableClient.Query<CardSetEntity>(entity => entity.PartitionKey == titleVersionId).ToList());
  }

  public override async Task<CardSetEntity?> Get(string titleVersionId, string cardSetId)
  {
    var response = await cardSetsTableClient.GetEntityIfExistsAsync<CardSetEntity>(titleVersionId, cardSetId);
    return response.HasValue ? response.Value : null;
  }

  public override async Task Update(string titleVersionId, CardSetEntity cardSet)
  {
    cardSet.PartitionKey = titleVersionId;
    await cardSetsTableClient.UpsertEntityAsync(cardSet, TableUpdateMode.Replace);
  }

  public override async Task<bool> Delete(string titleVersionId, string cardSetId)
  {
    var cardSet = await cardSetsTableClient.GetEntityIfExistsAsync<CardSetEntity>(titleVersionId, cardSetId);
    if (!cardSet.HasValue)
    {
      return false;
    }

    await cardSetsTableClient.DeleteEntityAsync(titleVersionId, cardSetId);
    return true;
  }

  public override async Task DeleteByTitleVersion(string titleVersionId)
  {
    foreach (var cardSet in await QueryByTitleVersion(titleVersionId))
    {
      await cardSetsTableClient.DeleteEntityAsync(titleVersionId, cardSet.RowKey);
    }
  }
}
