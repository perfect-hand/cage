using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Common;

namespace Cage.Backend.Editor.Organization;

public class OrganizationEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public required string Name { get; set; }

    public override string ToString()
    {
        return ToStringBuilder.For(this)
            .Add(nameof(PartitionKey), PartitionKey)
            .Add(nameof(RowKey), RowKey)
            .Add(nameof(Name), Name)
            .ToString();
    }
}
