namespace Cage.Backend.Editor.CardSet;

public class CardSetMapper
{
  public CardSetDto ToDto(CardSetEntity entity)
  {
    return new CardSetDto
    {
      Id = entity.RowKey,
      Name = entity.Name
    };
  }

  public CardSetEntity ToEntity(CardSetDto dto, string titleVersionId)
  {
    return new CardSetEntity
    {
      PartitionKey = titleVersionId,
      RowKey = Guid.NewGuid().ToString(),
      Name = dto.Name
    };
  }
}
