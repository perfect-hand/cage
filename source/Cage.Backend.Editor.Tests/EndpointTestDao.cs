using Azure.Data.Tables;
using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.Tests;

public class EndpointTestDao
{
  private readonly TableClient organizationsTableClient;
  private readonly TableClient usersInOrganizationTableClient;
  private readonly TableClient titlesTableClient;
  private readonly TableClient attributesTableClient;

  public EndpointTestDao(string connectionString)
  {
    var tableServiceClient = new TableServiceClient(connectionString);
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.OrganizationsTableName);
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.UsersInOrganizationTableName);
    tableServiceClient.CreateTableIfNotExists(TitleDao.TitlesTableName);
    tableServiceClient.CreateTableIfNotExists(AttributeDao.AttributesTableName);
    attributesTableClient = tableServiceClient.GetTableClient(AttributeDao.AttributesTableName);
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

  public async Task<(string OrganizationId, string TitleId)> AddTitleToOwnOrganization()
  {
    var organizationId = Guid.NewGuid().ToString();
    await AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var titleId = await AddTitle(organizationId, "Title");
    return (organizationId, titleId);
  }

  public async Task<string> AddAttribute(string titleId, string name, CageType type)
  {
    var attribute = new AttributeEntity
    {
      PartitionKey = titleId,
      RowKey = Guid.NewGuid().ToString(),
      Name = name,
      Type = type
    };
    await attributesTableClient.AddEntityAsync(attribute);
    return attribute.RowKey;
  }

  public List<AttributeEntity> QueryAttributesByTitle(string titleId)
  {
    return attributesTableClient.Query<AttributeEntity>(entity => entity.PartitionKey == titleId).ToList();
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
