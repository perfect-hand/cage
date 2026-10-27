using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Tests;

public class EndpointTestDao
{
  private readonly TableClient organizationsTableClient;
  private readonly TableClient usersInOrganizationTableClient;
  private readonly TableClient titlesTableClient;

  public EndpointTestDao(string connectionString)
  {
    var tableServiceClient = new TableServiceClient(connectionString);
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.OrganizationsTableName);
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.UsersInOrganizationTableName);
    tableServiceClient.CreateTableIfNotExists(TitleDao.TitlesTableName);
    organizationsTableClient = tableServiceClient.GetTableClient(OrganizationDao.OrganizationsTableName);
    usersInOrganizationTableClient = tableServiceClient.GetTableClient(OrganizationDao.UsersInOrganizationTableName);
    titlesTableClient = tableServiceClient.GetTableClient(TitleDao.TitlesTableName);
  }

  public async Task<OrganizationEntity> AddOrganization(string name)
  {
    var organization = new OrganizationEntity
    {
      PartitionKey = "Organization",
      RowKey = Guid.NewGuid().ToString(),
      Name = name
    };
    await organizationsTableClient.AddEntityAsync(organization);
    return organization;
  }

  public async Task AddUserToOrganization(string userId, string organizationId, string organizationName)
  {
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

  public async Task<string> AddTitle(string organizationId, string name)
  {
    var title = new TitleEntity
    {
      PartitionKey = organizationId,
      RowKey = Guid.NewGuid().ToString(),
      Name = name
    };
    await titlesTableClient.AddEntityAsync(title);
    return title.RowKey;
  }

  public List<TitleEntity> QueryTitlesByOrganization(string organizationId)
  {
    return titlesTableClient.Query<TitleEntity>(entity => entity.PartitionKey == organizationId).ToList();
  }

  public OrganizationEntity? QueryOrganization(string organizationId)
  {
    return organizationsTableClient
        .Query<OrganizationEntity>(entity => entity.PartitionKey == "Organization" && entity.RowKey == organizationId)
        .SingleOrDefault();
  }

  public List<UserInOrganizationEntity> QueryOrganizationsByUser(string userId)
  {
    return usersInOrganizationTableClient.Query<UserInOrganizationEntity>(entity => entity.PartitionKey == userId).ToList();
  }
}
