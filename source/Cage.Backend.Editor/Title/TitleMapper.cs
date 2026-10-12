namespace Cage.Backend.Editor.Title;

public class TitleMapper
{
  public TitleDto ToDto(TitleEntity entity)
  {
    return new TitleDto
    {
      Id = entity.RowKey,
      Name = entity.Name
    };
  }

  public TitleEntity ToEntity(TitleDto dto, string organizationId)
  {
    return new TitleEntity
    {
      PartitionKey = organizationId,
      RowKey = Guid.NewGuid().ToString(),
      Name = dto.Name
    };
  }
}
