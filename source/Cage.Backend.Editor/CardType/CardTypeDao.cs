using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.CardType;

public class CardTypeDao : VersionedTitleEntityDao<CardTypeEntity>
{
  public const string CardTypesTableName = "CardTypes";

  private readonly TableClient cardTypesTableClient;

  public CardTypeDao()
  {
    cardTypesTableClient = CreateTableClient(CardTypesTableName);
  }

  public override async Task Add(CardTypeEntity cardType)
  {
    await cardTypesTableClient.AddEntityAsync(cardType);
  }

  public override Task<List<CardTypeEntity>> QueryByTitleVersion(string titleVersionId)
  {
    return Task.FromResult(cardTypesTableClient.Query<CardTypeEntity>(entity => entity.PartitionKey == titleVersionId).ToList());
  }

  public override async Task<CardTypeEntity?> Get(string titleVersionId, string cardTypeId)
  {
    var response = await cardTypesTableClient.GetEntityIfExistsAsync<CardTypeEntity>(titleVersionId, cardTypeId);
    return response.HasValue ? response.Value : null;
  }

  public override async Task Update(string titleVersionId, CardTypeEntity cardType)
  {
    cardType.PartitionKey = titleVersionId;
    await cardTypesTableClient.UpsertEntityAsync(cardType, TableUpdateMode.Replace);
  }

  public override async Task<bool> Delete(string titleVersionId, string cardTypeId)
  {
    var cardType = await cardTypesTableClient.GetEntityIfExistsAsync<CardTypeEntity>(titleVersionId, cardTypeId);
    if (!cardType.HasValue)
    {
      return false;
    }

    await cardTypesTableClient.DeleteEntityAsync(titleVersionId, cardTypeId);
    return true;
  }

  public override async Task DeleteByTitleVersion(string titleVersionId)
  {
    var cardTypes = await QueryByTitleVersion(titleVersionId);
    foreach (var cardType in cardTypes)
    {
      await cardTypesTableClient.DeleteEntityAsync(titleVersionId, cardType.RowKey);
    }
  }
}
