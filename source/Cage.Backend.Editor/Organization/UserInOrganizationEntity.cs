using Azure;
using Azure.Data.Tables;

namespace Cage.Backend.Editor.Organization;

public class UserInOrganizationEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public required string OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
}
