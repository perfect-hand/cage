using Azure.Data.Tables;

namespace Cage.Backend.Editor.Title;

public class TitleDao : TableDao
{
  public const string TitlesTableName = "Titles";

  private readonly TableClient titlesTableClient;

  public TitleDao()
  {
    titlesTableClient = CreateTableClient(TitlesTableName);
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
