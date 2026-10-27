using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;

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

  public async Task<string> AddTitle(string organizationId, string name)
  {
    tableServiceClient.CreateTableIfNotExists(TitleDao.TitlesTableName);
    var titlesTableClient = new TableClient(connectionString, TitleDao.TitlesTableName);
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
    var titlesTableClient = new TableClient(connectionString, TitleDao.TitlesTableName);
    return titlesTableClient.Query<TitleEntity>(entity => entity.PartitionKey == organizationId).ToList();
  }
}
