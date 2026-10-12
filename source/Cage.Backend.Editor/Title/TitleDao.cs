using Azure.Data.Tables;
using Azure.Identity;

namespace Cage.Backend.Editor.Title;

public class TitleDao
{
  public const string TitlesTableName = "Titles";

  private readonly TableClient titlesTableClient;

  public TitleDao()
  {
    var storageUri = Environment.GetEnvironmentVariable("CAGE_TABLE_STORAGE_URI");
    var storageConnectionString = Environment.GetEnvironmentVariable("CAGE_TABLE_STORAGE_CONNECTION_STRING") ?? "UseDevelopmentStorage=true";

    var tableServiceClient = storageUri != null
        ? new TableServiceClient(new Uri(storageUri), new DefaultAzureCredential())
        : new TableServiceClient(storageConnectionString);

    tableServiceClient.CreateTableIfNotExists(TitlesTableName);

    titlesTableClient = storageUri != null
        ? new TableClient(new Uri(storageUri), TitlesTableName, new DefaultAzureCredential())
        : new TableClient(storageConnectionString, TitlesTableName);
  }

  public async Task AddTitle(TitleEntity title)
  {
    await titlesTableClient.AddEntityAsync(title);
  }

  public async Task<List<TitleEntity>> QueryByOrganization(string organizationId)
  {
    return titlesTableClient.Query<TitleEntity>(ent => ent.PartitionKey == organizationId).ToList();
  }
}
