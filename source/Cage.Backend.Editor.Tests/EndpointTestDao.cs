using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;

namespace Cage.Backend.Editor.Tests;

public class EndpointTestDao
{
  private readonly string connectionString;
  private readonly TableServiceClient tableServiceClient;

  public EndpointTestDao(string connectionString)
  {
    this.connectionString = connectionString;
    tableServiceClient = new TableServiceClient(connectionString);
  }

  public async Task AddUserToOrganization(string userId, string organizationId, string organizationName)
  {
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
