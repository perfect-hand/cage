using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Common;

namespace Cage.Backend.Editor.CardType;

public class CardTypeEntity : ITableEntity, IVersionedTitleEntity
{
  public required string PartitionKey { get; set; }
  public required string RowKey { get; set; }
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public required string Name { get; set; }
  public string? ParentId { get; set; }
  public string AttributesJson { get; set; } = "{}";
  public string TagsJson { get; set; } = "[]";
  public bool IsDeleted { get; set; }

  public override string ToString()
  {
    return ToStringBuilder.For(this)
        .Add(nameof(PartitionKey), PartitionKey)
        .Add(nameof(RowKey), RowKey)
        .Add(nameof(Name), Name)
        .Add(nameof(ParentId), ParentId)
        .ToString();
  }
}
