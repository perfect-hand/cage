using Azure;
using Azure.Data.Tables;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Tag;

public class TagDao : VersionedTitleEntityDao<TagEntity>
{
  public const string TagsTableName = "Tags";

  private readonly TableClient tagsTableClient;

  public TagDao()
  {
    tagsTableClient = CreateTableClient(TagsTableName);
  }

  public override async Task Add(TagEntity tag)
  {
    await tagsTableClient.AddEntityAsync(tag);
  }

  public override Task<List<TagEntity>> QueryByTitleVersion(string titleVersionId)
  {
    return Task.FromResult(tagsTableClient.Query<TagEntity>(entity => entity.PartitionKey == titleVersionId).ToList());
  }

  public override async Task<TagEntity?> Get(string titleVersionId, string tagId)
  {
    var response = await tagsTableClient.GetEntityIfExistsAsync<TagEntity>(titleVersionId, tagId);
    return response.HasValue ? response.Value : null;
  }

  public override async Task Update(string titleVersionId, TagEntity tag)
  {
    tag.PartitionKey = titleVersionId;
    await tagsTableClient.UpsertEntityAsync(tag, TableUpdateMode.Replace);
  }

  public override async Task<bool> Delete(string titleVersionId, string tagId)
  {
    var tag = await tagsTableClient.GetEntityIfExistsAsync<TagEntity>(titleVersionId, tagId);
    if (!tag.HasValue)
    {
      return false;
    }

    await tagsTableClient.DeleteEntityAsync(titleVersionId, tagId);
    return true;
  }

  public override async Task DeleteByTitleVersion(string titleVersionId)
  {
    var tags = await QueryByTitleVersion(titleVersionId);
    foreach (var tag in tags)
    {
      await tagsTableClient.DeleteEntityAsync(titleVersionId, tag.RowKey);
    }
  }
}
