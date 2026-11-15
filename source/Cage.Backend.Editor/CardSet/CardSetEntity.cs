using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.CardSet;

public class CardSetEntity : ITableEntity, IVersionedTitleEntity
{
  public required string PartitionKey { get; set; }
  public required string RowKey { get; set; }
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }
  public required string Name { get; set; }
  public bool IsDeleted { get; set; }
}
