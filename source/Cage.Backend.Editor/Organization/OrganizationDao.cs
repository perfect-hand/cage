using System;
using Azure.Data.Tables;
using Azure.Identity;

namespace Cage.Backend.Editor.Organization;

public class OrganizationDao
{
    public const string OrganizationsTableName = "Organizations";
    public const string UsersInOrganizationTableName = "UsersInOrganizations";

    private readonly TableClient organizationsTableClient;
    private readonly TableClient usersInOrganizationTableClient;

    public OrganizationDao()
    {
        // Use TABLE_STORAGE_URI to connect to Azure Table Storage in production.
        // Use TABLE_STORAGE_CONNECTION_STRING for connecting to Azurite in Testcontainers where ports can differ and HTTPS is not supported.
        // Fall back to default connection string for local development.
        // See https://learn.microsoft.com/en-us/azure/storage/common/storage-connect-azurite?tabs=table-storage
        var storageUri = Environment.GetEnvironmentVariable("TABLE_STORAGE_URI");
        var storageConnectionString = Environment.GetEnvironmentVariable("TABLE_STORAGE_CONNECTION_STRING") ?? "UseDevelopmentStorage=true";

        // See https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
        var tableServiceClient = storageUri != null
            ? new TableServiceClient(new Uri(storageUri), new DefaultAzureCredential())
            : new TableServiceClient(storageConnectionString);

        tableServiceClient.CreateTableIfNotExists(OrganizationsTableName);
        tableServiceClient.CreateTableIfNotExists(UsersInOrganizationTableName);

        organizationsTableClient = storageUri != null
            ? new TableClient(new Uri(storageUri), OrganizationsTableName, new DefaultAzureCredential())
            : new TableClient(storageConnectionString, OrganizationsTableName);
        usersInOrganizationTableClient = storageUri != null
            ? new TableClient(new Uri(storageUri), UsersInOrganizationTableName, new DefaultAzureCredential())
            : new TableClient(storageConnectionString, UsersInOrganizationTableName);
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
}
