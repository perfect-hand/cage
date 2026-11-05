using Azure;
using Azure.Data.Tables;

namespace Cage.Backend.Editor.Tag;

public class TagDao : TableDao
{
  public const string TagsTableName = "Tags";

  private readonly TableClient tagsTableClient;

  public TagDao()
  {
    tagsTableClient = CreateTableClient(TagsTableName);
  }

  public async Task AddTag(TagEntity tag)
  {
    await tagsTableClient.AddEntityAsync(tag);
  }

  public Task<List<TagEntity>> QueryByTitleVersion(string titleVersionId)
  {
    return Task.FromResult(tagsTableClient.Query<TagEntity>(entity => entity.PartitionKey == titleVersionId).ToList());
  }

  public async Task<TagEntity?> GetTagTitleVersion(string titleVersionId, string tagId)
  {
    var response = await tagsTableClient.GetEntityIfExistsAsync<TagEntity>(titleVersionId, tagId);
    return response.HasValue ? response.Value : null;
  }

  public async Task SetTagTitleVersion(string titleVersionId, TagEntity tag)
  {
    tag.PartitionKey = titleVersionId;
    await tagsTableClient.UpsertEntityAsync(tag, TableUpdateMode.Replace);
  }

  public async Task<bool> DeleteTagTitleVersion(string titleVersionId, string tagId)
  {
    var tag = await tagsTableClient.GetEntityIfExistsAsync<TagEntity>(titleVersionId, tagId);
    if (!tag.HasValue)
    {
      return false;
    }

    await tagsTableClient.DeleteEntityAsync(titleVersionId, tagId);
    return true;
  }

  public async Task DeleteByTitleVersion(string titleVersionId)
  {
    var tags = await QueryByTitleVersion(titleVersionId);
    foreach (var tag in tags)
    {
      await tagsTableClient.DeleteEntityAsync(titleVersionId, tag.RowKey);
    }
  }
}
