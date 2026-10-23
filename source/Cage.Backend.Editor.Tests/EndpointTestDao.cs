using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;

namespace Cage.Backend.Editor.Tests;

public class EndpointTestDao
{
  private readonly string connectionString;

  public EndpointTestDao(string connectionString)
  {
    this.connectionString = connectionString;
  }

  public async Task AddUserToOrganization(string userId, string organizationId, string organizationName)
  {
    var tableServiceClient = new TableServiceClient(connectionString);
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.UsersInOrganizationTableName);
    var usersInOrganizationTableClient = new TableClient(connectionString, OrganizationDao.UsersInOrganizationTableName);

    await usersInOrganizationTableClient.AddEntityAsync(new UserInOrganizationEntity
    {
      PartitionKey = organizationId,
      RowKey = userId,
      OrganizationId = organizationId,
      OrganizationName = organizationName
    });
    await usersInOrganizationTableClient.AddEntityAsync(new UserInOrganizationEntity
    {
      PartitionKey = userId,
      RowKey = organizationId,
      OrganizationId = organizationId,
      OrganizationName = organizationName
    });
  }
}
