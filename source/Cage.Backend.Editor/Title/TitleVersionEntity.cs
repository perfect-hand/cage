using Azure;
using Azure.Data.Tables;

namespace Cage.Backend.Editor.Title;

public class TitleVersionEntity : ITableEntity
{
  public required string PartitionKey { get; set; }
  public required string RowKey { get; set; }
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public required string Name { get; set; }
  public string? ParentVersionId { get; set; }
  public bool IsReadOnly { get; set; }
  public bool IsDeleted { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
