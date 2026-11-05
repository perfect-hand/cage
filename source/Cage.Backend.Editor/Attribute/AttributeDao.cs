using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Attribute;

public class AttributeDao : TableDao, IVersionedTitleEntityDao<AttributeEntity>
{
  public const string AttributesTableName = "Attributes";

  private readonly TableClient attributesTableClient;

  public AttributeDao()
  {
    attributesTableClient = CreateTableClient(AttributesTableName);
  }

  public async Task AddAttribute(AttributeEntity attribute)
  {
    await attributesTableClient.AddEntityAsync(attribute);
  }

  public Task<List<AttributeEntity>> QueryByTitleVersion(string titleVersionId)
  {
    return Task.FromResult(attributesTableClient.Query<AttributeEntity>(ent => ent.PartitionKey == titleVersionId).ToList());
  }

  public async Task<AttributeEntity?> GetAttributeTitleVersion(string titleVersionId, string attributeId)
  {
    var response = await attributesTableClient.GetEntityIfExistsAsync<AttributeEntity>(titleVersionId, attributeId);
    return response.HasValue ? response.Value : null;
  }

  public async Task SetAttributeTitleVersion(string titleVersionId, AttributeEntity attribute)
  {
    attribute.PartitionKey = titleVersionId;
    await attributesTableClient.UpsertEntityAsync(attribute, TableUpdateMode.Replace);
  }

  public async Task<bool> DeleteAttributeTitleVersion(string titleVersionId, string attributeId)
  {
    var attribute = await attributesTableClient.GetEntityIfExistsAsync<AttributeEntity>(titleVersionId, attributeId);
    if (!attribute.HasValue)
    {
      return false;
    }

    await attributesTableClient.DeleteEntityAsync(titleVersionId, attributeId);
    return true;
  }

  public async Task DeleteByTitleVersion(string titleVersionId)
  {
    var attributes = await QueryByTitleVersion(titleVersionId);
    foreach (var attribute in attributes)
    {
      await attributesTableClient.DeleteEntityAsync(titleVersionId, attribute.RowKey);
    }
  }

  Task IVersionedTitleEntityDao<AttributeEntity>.Add(AttributeEntity attribute) => AddAttribute(attribute);
  Task<AttributeEntity?> IVersionedTitleEntityDao<AttributeEntity>.Get(string titleVersionId, string entityId) =>
      GetAttributeTitleVersion(titleVersionId, entityId);
  Task IVersionedTitleEntityDao<AttributeEntity>.Set(string titleVersionId, AttributeEntity attribute) =>
      SetAttributeTitleVersion(titleVersionId, attribute);
  Task<bool> IVersionedTitleEntityDao<AttributeEntity>.Delete(string titleVersionId, string entityId) =>
      DeleteAttributeTitleVersion(titleVersionId, entityId);
  Task IVersionedTitleEntityDao<AttributeEntity>.DeleteByTitleVersion(string titleVersionId) =>
      DeleteByTitleVersion(titleVersionId);
}
