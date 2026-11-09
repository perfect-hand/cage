using System.Text.Json;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.CardType;

public class CardTypeMapper
{
  public CardTypeDto ToDto(CardTypeEntity entity)
  {
    return new CardTypeDto
    {
      Id = entity.RowKey,
      Name = entity.Name,
      ParentId = entity.ParentId,
      Attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(entity.AttributesJson) ?? new(),
      Tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? new()
    };
  }

  public CardTypeEntity ToEntity(CardTypeDto dto, string titleVersionId)
  {
    return new CardTypeEntity
    {
      PartitionKey = titleVersionId,
      RowKey = Guid.NewGuid().ToString(),
      Name = dto.Name,
      ParentId = dto.ParentId,
      AttributesJson = JsonSerializer.Serialize(dto.Attributes),
      TagsJson = JsonSerializer.Serialize(dto.Tags.Distinct(StringComparer.Ordinal))
    };
  }
}
