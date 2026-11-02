using Azure;
using Azure.Data.Tables;

namespace Cage.Backend.Editor.Attribute;

public class AttributeDao : TableDao
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
}
