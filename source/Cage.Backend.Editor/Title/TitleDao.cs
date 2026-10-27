using Azure;
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

  public async Task<bool> UpdateTitleName(string organizationId, string titleId, string name)
  {
    var titleResponse = await titlesTableClient.GetEntityIfExistsAsync<TitleEntity>(organizationId, titleId);
    if (!titleResponse.HasValue)
    {
      return false;
    }

    var title = titleResponse.Value!;
    title.Name = name;
    await titlesTableClient.UpdateEntityAsync(title, ETag.All, TableUpdateMode.Merge);
    return true;
  }

  public async Task<bool> DeleteTitle(string organizationId, string titleId)
  {
    var titleResponse = await titlesTableClient.GetEntityIfExistsAsync<TitleEntity>(organizationId, titleId);
    if (!titleResponse.HasValue)
    {
      return false;
    }

    await titlesTableClient.DeleteEntityAsync(organizationId, titleId);
    return true;
  }

  public async Task DeleteByOrganization(string organizationId)
  {
    var titles = await QueryByOrganization(organizationId);
    foreach (var title in titles)
    {
      await titlesTableClient.DeleteEntityAsync(organizationId, title.RowKey);
    }
  }
}
