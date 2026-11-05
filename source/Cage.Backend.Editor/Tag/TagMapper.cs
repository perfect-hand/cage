namespace Cage.Backend.Editor.Tag;

public class TagMapper
{
  public TagDto ToDto(TagEntity entity)
  {
    return new TagDto
    {
      Id = entity.RowKey,
      Name = entity.Name
    };
  }

  public TagEntity ToEntity(TagDto dto, string titleVersionId)
  {
    return new TagEntity
    {
      PartitionKey = titleVersionId,
      RowKey = Guid.NewGuid().ToString(),
      Name = dto.Name
    };
  }
}
