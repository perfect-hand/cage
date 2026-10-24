using Azure.Data.Tables;
using Azure.Identity;

namespace Cage.Backend.Editor;

public abstract class TableDao
{
  // Use CAGE_TABLE_STORAGE_URI to connect to Azure Table Storage in production.
  // Use CAGE_TABLE_STORAGE_CONNECTION_STRING for connecting to Azurite in Testcontainers where ports can differ and HTTPS is not supported.
  // Fall back to default connection string for local development.
  // See https://learn.microsoft.com/en-us/azure/storage/common/storage-connect-azurite?tabs=table-storage
  private readonly string? storageUri = Environment.GetEnvironmentVariable("CAGE_TABLE_STORAGE_URI");
  private readonly string storageConnectionString = Environment.GetEnvironmentVariable("CAGE_TABLE_STORAGE_CONNECTION_STRING") ?? "UseDevelopmentStorage=true";
  private readonly TableServiceClient tableServiceClient;

  // See https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
  protected TableDao()
  {
    tableServiceClient = storageUri != null
        ? new TableServiceClient(new Uri(storageUri), new DefaultAzureCredential())
        : new TableServiceClient(storageConnectionString);
  }

  protected TableClient CreateTableClient(string tableName)
  {
    tableServiceClient.CreateTableIfNotExists(tableName);

    return storageUri != null
        ? new TableClient(new Uri(storageUri), tableName, new DefaultAzureCredential())
        : new TableClient(storageConnectionString, tableName);
  }
}
