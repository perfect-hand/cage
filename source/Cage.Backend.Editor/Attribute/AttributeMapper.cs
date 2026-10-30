namespace Cage.Backend.Editor.Attribute;

public class AttributeMapper
{
  public AttributeDto ToDto(AttributeEntity entity)
  {
    return new AttributeDto
    {
      Id = entity.RowKey,
      Name = entity.Name,
      Type = entity.Type
    };
  }

  public AttributeEntity ToEntity(AttributeDto dto, string titleId)
  {
    return new AttributeEntity
    {
      PartitionKey = titleId,
      RowKey = Guid.NewGuid().ToString(),
      Name = dto.Name,
      Type = dto.Type
    };
  }
}
