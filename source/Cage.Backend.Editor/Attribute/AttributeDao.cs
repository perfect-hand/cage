using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Attribute;

public class AttributeDao : VersionedTitleEntityDao<AttributeEntity>
{
  public const string AttributesTableName = "Attributes";

  private readonly TableClient attributesTableClient;

  public AttributeDao()
  {
    attributesTableClient = CreateTableClient(AttributesTableName);
  }

  public override async Task Add(AttributeEntity attribute)
  {
    await attributesTableClient.AddEntityAsync(attribute);
  }

  public override Task<List<AttributeEntity>> QueryByTitleVersion(string titleVersionId)
  {
    return Task.FromResult(attributesTableClient.Query<AttributeEntity>(ent => ent.PartitionKey == titleVersionId).ToList());
  }

  public override async Task<AttributeEntity?> Get(string titleVersionId, string attributeId)
  {
    var response = await attributesTableClient.GetEntityIfExistsAsync<AttributeEntity>(titleVersionId, attributeId);
    return response.HasValue ? response.Value : null;
  }

  public override async Task Update(string titleVersionId, AttributeEntity attribute)
  {
    attribute.PartitionKey = titleVersionId;
    await attributesTableClient.UpsertEntityAsync(attribute, TableUpdateMode.Replace);
  }

  public override async Task<bool> Delete(string titleVersionId, string attributeId)
  {
    var attribute = await attributesTableClient.GetEntityIfExistsAsync<AttributeEntity>(titleVersionId, attributeId);
    if (!attribute.HasValue)
    {
      return false;
    }

    await attributesTableClient.DeleteEntityAsync(titleVersionId, attributeId);
    return true;
  }

  public override async Task DeleteByTitleVersion(string titleVersionId)
  {
    var attributes = await QueryByTitleVersion(titleVersionId);
    foreach (var attribute in attributes)
    {
      await attributesTableClient.DeleteEntityAsync(titleVersionId, attribute.RowKey);
    }
  }
}
