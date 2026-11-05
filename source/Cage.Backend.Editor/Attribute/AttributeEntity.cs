using Azure;
using Azure.Data.Tables;
using Cage.Simulation.Common;
using Cage.Simulation.Types;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Attribute;

public class AttributeEntity : ITableEntity, IVersionedTitleEntity
{
  public required string PartitionKey { get; set; }
  public required string RowKey { get; set; }
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public required string Name { get; set; }
  public CageType Type { get; set; }
  public bool IsDeleted { get; set; }

  public override string ToString()
  {
    return ToStringBuilder.For(this)
        .Add(nameof(PartitionKey), PartitionKey)
        .Add(nameof(RowKey), RowKey)
        .Add(nameof(Name), Name)
        .Add(nameof(Type), Type)
        .ToString();
  }
}
