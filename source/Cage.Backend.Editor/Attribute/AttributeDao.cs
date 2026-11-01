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

  public async Task SetAttributeTitleVersion(string titleVersionId, AttributeEntity attribute)
  {
    attribute.PartitionKey = titleVersionId;
    await attributesTableClient.UpsertEntityAsync(attribute, TableUpdateMode.Replace);
  }

  public async Task AddVersionSnapshot(string snapshotId, List<AttributeEntity> attributes)
  {
    foreach (var attribute in attributes)
    {
      await attributesTableClient.AddEntityAsync(new AttributeEntity
      {
        PartitionKey = snapshotId,
        RowKey = attribute.RowKey,
        Name = attribute.Name,
        Type = attribute.Type
      });
    }
  }

  public async Task<bool> DeleteAttributeTitleVersion(string titleVersionId, string attributeId)
  {
    var attributeResponse = await attributesTableClient.GetEntityIfExistsAsync<AttributeEntity>(titleVersionId, attributeId);
    if (!attributeResponse.HasValue)
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
