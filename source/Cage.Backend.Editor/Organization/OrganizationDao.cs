using Azure.Data.Tables;

namespace Cage.Backend.Editor.Organization;

public class OrganizationDao : TableDao
{
  public const string OrganizationsTableName = "Organizations";
  public const string UsersInOrganizationTableName = "UsersInOrganizations";

  private readonly TableClient organizationsTableClient;
  private readonly TableClient usersInOrganizationTableClient;

  public OrganizationDao()
  {
    organizationsTableClient = CreateTableClient(OrganizationsTableName);
    usersInOrganizationTableClient = CreateTableClient(UsersInOrganizationTableName);
  }

  public async Task AddOrganization(OrganizationEntity organization)
  {
    await organizationsTableClient.AddEntityAsync(organization);
  }

  public async Task AddUserToOrganization(UserInOrganizationEntity userInOrganization)
  {
    await usersInOrganizationTableClient.AddEntityAsync(userInOrganization);
  }

  public async Task<List<UserInOrganizationEntity>> QueryByUser(string user)
  {
    return usersInOrganizationTableClient.Query<UserInOrganizationEntity>(ent => ent.PartitionKey == user).ToList();
  }

  public async Task<bool> IsUserInOrganization(string organizationId, string user)
  {
    return usersInOrganizationTableClient.Query<UserInOrganizationEntity>(ent => ent.PartitionKey == organizationId && ent.RowKey == user).Any();
  }
}
