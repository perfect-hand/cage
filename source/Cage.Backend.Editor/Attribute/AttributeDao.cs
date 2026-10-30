using Azure;
using Azure.Data.Tables;
using Cage.Simulation.Types;

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

  public async Task<List<AttributeEntity>> QueryByTitle(string titleId)
  {
    return attributesTableClient.Query<AttributeEntity>(ent => ent.PartitionKey == titleId).ToList();
  }

  public async Task<bool> UpdateAttribute(string titleId, string attributeId, string name, CageType type)
  {
    var attributeResponse = await attributesTableClient.GetEntityIfExistsAsync<AttributeEntity>(titleId, attributeId);
    if (!attributeResponse.HasValue)
    {
      return false;
    }

    var attribute = attributeResponse.Value!;
    attribute.Name = name;
    attribute.Type = type;
    await attributesTableClient.UpdateEntityAsync(attribute, ETag.All, TableUpdateMode.Merge);
    return true;
  }

  public async Task<bool> DeleteAttribute(string titleId, string attributeId)
  {
    var attributeResponse = await attributesTableClient.GetEntityIfExistsAsync<AttributeEntity>(titleId, attributeId);
    if (!attributeResponse.HasValue)
    {
      return false;
    }

    await attributesTableClient.DeleteEntityAsync(titleId, attributeId);
    return true;
  }

  public async Task DeleteByTitle(string titleId)
  {
    var attributes = await QueryByTitle(titleId);
    foreach (var attribute in attributes)
    {
      await attributesTableClient.DeleteEntityAsync(titleId, attribute.RowKey);
    }
  }
}
