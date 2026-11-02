namespace Cage.Backend.Editor.Title;

public class TitleVersionMapper
{
  public TitleVersionDto ToDto(TitleVersionEntity entity)
  {
    return new TitleVersionDto
    {
      Id = entity.RowKey,
      Name = entity.Name,
      ParentVersionId = entity.ParentVersionId,
      IsReadOnly = entity.IsReadOnly
    };
  }

  public TitleVersionEntity ToEntity(TitleVersionDto dto, string titleId, string? parentVersionId)
  {
    return new TitleVersionEntity
    {
      PartitionKey = titleId,
      RowKey = Guid.NewGuid().ToString(),
      Name = dto.Name,
      ParentVersionId = parentVersionId
    };
  }
}
