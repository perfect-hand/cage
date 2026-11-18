using System.Text.Json;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.Card;

public class CardMapper
{
  public CardDto ToDto(CardEntity entity)
  {
    return new CardDto
    {
      Id = entity.RowKey,
      Name = entity.Name,
      CardTypeId = entity.CardTypeId,
      Attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(entity.AttributesJson) ?? new(),
      Tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? new()
    };
  }

  public CardEntity ToEntity(CardDto dto, string titleVersionId, string cardSetId)
  {
    return new CardEntity
    {
      PartitionKey = titleVersionId,
      RowKey = Guid.NewGuid().ToString(),
      Name = dto.Name,
      CardSetId = cardSetId,
      CardTypeId = dto.CardTypeId,
      AttributesJson = JsonSerializer.Serialize(dto.Attributes),
      TagsJson = JsonSerializer.Serialize(dto.Tags.Distinct(StringComparer.Ordinal))
    };
  }
}
