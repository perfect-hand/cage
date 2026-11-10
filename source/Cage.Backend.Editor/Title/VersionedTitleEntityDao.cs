namespace Cage.Backend.Editor.Title;

public interface IVersionedTitleEntityDao<TEntity> where TEntity : IVersionedTitleEntity
{
  Task Add(TEntity entity);
  Task<List<TEntity>> QueryByTitleVersion(string titleVersionId);
  Task<TEntity?> Get(string titleVersionId, string entityId);
  Task Update(string titleVersionId, TEntity entity);
  Task<bool> Delete(string titleVersionId, string entityId);
  Task DeleteByTitleVersion(string titleVersionId);
  Task<List<TEntity>> FindAllVersions(string titleId, string versionId);
}

public abstract class VersionedTitleEntityDao<TEntity> : TableDao, IVersionedTitleEntityDao<TEntity>
    where TEntity : class, IVersionedTitleEntity
{
  private readonly TitleVersionDao titleVersionDao = new();

  public abstract Task Add(TEntity entity);
  public abstract Task<List<TEntity>> QueryByTitleVersion(string titleVersionId);
  public abstract Task<TEntity?> Get(string titleVersionId, string entityId);
  public abstract Task Update(string titleVersionId, TEntity entity);
  public abstract Task<bool> Delete(string titleVersionId, string entityId);
  public abstract Task DeleteByTitleVersion(string titleVersionId);

  public async Task<List<TEntity>> FindAllVersions(string titleId, string versionId)
  {
    var versions = new List<TitleVersionEntity>();
    var visited = new HashSet<string>();
    var currentId = versionId;
    while (true)
    {
      if (!visited.Add(currentId))
      {
        throw new InvalidOperationException("The title version hierarchy is invalid.");
      }

      var version = await titleVersionDao.Get(titleId, currentId);
      if (version is null)
      {
        throw new UnauthorizedAccessException();
      }

      versions.Add(version);
      if (version.ParentVersionId is null)
      {
        break;
      }

      currentId = version.ParentVersionId;
    }

    var entitiesById = new Dictionary<string, TEntity>();
    foreach (var version in versions.AsEnumerable().Reverse())
    {
      foreach (var entity in await QueryByTitleVersion(version.RowKey))
      {
        if (entity.IsDeleted)
        {
          entitiesById.Remove(entity.RowKey);
        }
        else
        {
          entitiesById[entity.RowKey] = entity;
        }
      }
    }

    return entitiesById.Values.ToList();
  }
}
