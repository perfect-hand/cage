using Azure;
using Azure.Data.Tables;
using Cage.Simulation.Common;

namespace Cage.Backend.Editor.Organization;

public class UserInOrganizationEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public required string OrganizationId { get; set; }
    public required string OrganizationName { get; set; }

    public override string ToString()
    {
        return ToStringBuilder.For(this)
            .Add(nameof(PartitionKey), PartitionKey)
            .Add(nameof(RowKey), RowKey)
            .Add(nameof(OrganizationId), OrganizationId)
            .Add(nameof(OrganizationName), OrganizationName)
            .ToString();
    }
}
