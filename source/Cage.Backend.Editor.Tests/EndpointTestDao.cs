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
  private readonly TableClient titleVersionsTableClient;
  private readonly TableClient attributesTableClient;

  public EndpointTestDao(string connectionString)
  {
    var tableServiceClient = new TableServiceClient(connectionString);
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.OrganizationsTableName);
    tableServiceClient.CreateTableIfNotExists(OrganizationDao.UsersInOrganizationTableName);
    tableServiceClient.CreateTableIfNotExists(TitleDao.TitlesTableName);
    tableServiceClient.CreateTableIfNotExists(TitleVersionDao.TitleVersionsTableName);
    tableServiceClient.CreateTableIfNotExists(AttributeDao.AttributesTableName);
    attributesTableClient = tableServiceClient.GetTableClient(AttributeDao.AttributesTableName);
    organizationsTableClient = tableServiceClient.GetTableClient(OrganizationDao.OrganizationsTableName);
    usersInOrganizationTableClient = tableServiceClient.GetTableClient(OrganizationDao.UsersInOrganizationTableName);
    titlesTableClient = tableServiceClient.GetTableClient(TitleDao.TitlesTableName);
    titleVersionsTableClient = tableServiceClient.GetTableClient(TitleVersionDao.TitleVersionsTableName);
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
    await titleVersionsTableClient.AddEntityAsync(new TitleVersionEntity
    {
      PartitionKey = title.RowKey,
      RowKey = Guid.NewGuid().ToString(),
      Name = TitleVersionService.InitialVersionName
    });
    return title.RowKey;
  }

  public async Task<(string OrganizationId, string TitleId)> AddTitleToOwnOrganization()
  {
    var organizationId = Guid.NewGuid().ToString();
    await AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Organization");
    var titleId = await AddTitle(organizationId, "Title");
    return (organizationId, titleId);
  }

  public async Task<string> AddAttribute(string titleVersionId, string name, CageType type)
  {
    var attribute = new AttributeEntity
    {
      PartitionKey = titleVersionId,
      RowKey = Guid.NewGuid().ToString(),
      Name = name,
      Type = type
    };
    await attributesTableClient.AddEntityAsync(attribute);
    return attribute.RowKey;
  }

  public async Task<string> AddVersion(string titleId, string name, string? parentVersionId = null, bool isReadOnly = false)
  {
    var version = new TitleVersionEntity
    {
      PartitionKey = titleId,
      RowKey = Guid.NewGuid().ToString(),
      Name = name,
      ParentVersionId = parentVersionId,
      IsReadOnly = isReadOnly
    };
    await titleVersionsTableClient.AddEntityAsync(version);
    return version.RowKey;
  }

  public string GetInitialVersionId(string titleId)
  {
    return titleVersionsTableClient.Query<TitleVersionEntity>(entity =>
        entity.PartitionKey == titleId && entity.Name == TitleVersionService.InitialVersionName)
        .Single().RowKey;
  }

  public async Task SetVersionReadOnly(string titleId, string titleVersionId, bool isReadOnly)
  {
    var version = (await titleVersionsTableClient.GetEntityAsync<TitleVersionEntity>(titleId, titleVersionId)).Value;
    version.IsReadOnly = isReadOnly;
    await titleVersionsTableClient.UpdateEntityAsync(version, version.ETag, TableUpdateMode.Replace);
  }

  public List<AttributeEntity> QueryAttributesByTitleVersion(string titleVersionId)
  {
    return attributesTableClient.Query<AttributeEntity>(entity => entity.PartitionKey == titleVersionId).ToList();
  }

  public List<TitleEntity> QueryTitlesByOrganization(string organizationId)
  {
    return titlesTableClient.Query<TitleEntity>(entity => entity.PartitionKey == organizationId).ToList();
  }

  public List<TitleVersionEntity> QueryVersionsByTitle(string titleId)
  {
    return titleVersionsTableClient.Query<TitleVersionEntity>(entity => entity.PartitionKey == titleId).ToList();
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
