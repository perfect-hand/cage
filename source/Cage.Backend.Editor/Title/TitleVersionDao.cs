using Azure;
using Azure.Data.Tables;

namespace Cage.Backend.Editor.Title;

public class TitleVersionDao : TableDao
{
  public const string TitleVersionsTableName = "TitleVersions";

  private readonly TableClient titleVersionsTableClient;

  public TitleVersionDao()
  {
    titleVersionsTableClient = CreateTableClient(TitleVersionsTableName);
  }

  public async Task Add(TitleVersionEntity version)
  {
    await titleVersionsTableClient.AddEntityAsync(version);
  }

  public async Task<TitleVersionEntity?> Get(string titleId, string versionId)
  {
    var response = await titleVersionsTableClient.GetEntityIfExistsAsync<TitleVersionEntity>(titleId, versionId);
    return response.HasValue ? response.Value : null;
  }

  public async Task<List<TitleVersionEntity>> QueryByTitle(string titleId)
  {
    return titleVersionsTableClient.Query<TitleVersionEntity>(entity => entity.PartitionKey == titleId).ToList();
  }

  public async Task Update(TitleVersionEntity version)
  {
    await titleVersionsTableClient.UpdateEntityAsync(version, version.ETag, TableUpdateMode.Replace);
  }

  public async Task Delete(string titleId, string versionId)
  {
    await titleVersionsTableClient.DeleteEntityAsync(titleId, versionId);
  }

  public async Task DeleteByTitle(string titleId)
  {
    foreach (var version in await QueryByTitle(titleId))
    {
      await Delete(titleId, version.RowKey);
    }
  }
}
