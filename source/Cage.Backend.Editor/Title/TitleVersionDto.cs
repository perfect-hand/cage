namespace Cage.Backend.Editor.Title;

public class TitleVersionDto
{
  public string? Id { get; set; }
  public required string Name { get; set; }
  public string? ParentVersionId { get; set; }
  public bool IsReadOnly { get; set; }
  public DateTimeOffset CreatedAt { get; set; }
}
