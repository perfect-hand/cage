using Azure;
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

  public async Task<bool> UpdateOrganizationName(string organizationId, string name)
  {
    var organizationResponse = await organizationsTableClient.GetEntityIfExistsAsync<OrganizationEntity>("Organization", organizationId);
    if (!organizationResponse.HasValue)
    {
      return false;
    }

    var organization = organizationResponse.Value!;
    organization.Name = name;
    await organizationsTableClient.UpdateEntityAsync(organization, ETag.All, TableUpdateMode.Merge);

    var usersInOrganization = usersInOrganizationTableClient.Query<UserInOrganizationEntity>(ent => ent.PartitionKey == organizationId).ToList();
    foreach (var userInOrganization in usersInOrganization)
    {
      userInOrganization.OrganizationName = name;
      await usersInOrganizationTableClient.UpdateEntityAsync(userInOrganization, ETag.All, TableUpdateMode.Merge);

      var userOrganizationResponse = await usersInOrganizationTableClient.GetEntityIfExistsAsync<UserInOrganizationEntity>(userInOrganization.RowKey, organizationId);
      if (userOrganizationResponse.HasValue)
      {
        var userOrganization = userOrganizationResponse.Value!;
        userOrganization.OrganizationName = name;
        await usersInOrganizationTableClient.UpdateEntityAsync(userOrganization, ETag.All, TableUpdateMode.Merge);
      }
    }

    return true;
  }

  public async Task<bool> DeleteOrganization(string organizationId)
  {
    var organizationResponse = await organizationsTableClient.GetEntityIfExistsAsync<OrganizationEntity>("Organization", organizationId);
    if (!organizationResponse.HasValue)
    {
      return false;
    }

    await organizationsTableClient.DeleteEntityAsync("Organization", organizationId);

    var usersInOrganization = usersInOrganizationTableClient.Query<UserInOrganizationEntity>(ent => ent.PartitionKey == organizationId).ToList();
    foreach (var userInOrganization in usersInOrganization)
    {
      await usersInOrganizationTableClient.DeleteEntityAsync(organizationId, userInOrganization.RowKey);
      await usersInOrganizationTableClient.DeleteEntityAsync(userInOrganization.RowKey, organizationId);
    }

    return true;
  }

  public async Task<bool> IsUserInOrganization(string organizationId, string user)
  {
    return usersInOrganizationTableClient.Query<UserInOrganizationEntity>(ent => ent.PartitionKey == organizationId && ent.RowKey == user).Any();
  }
}
