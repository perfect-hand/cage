using Cage.Backend.Editor.Organization;

namespace Cage.Backend.Editor.Title;

public abstract class VersionedTitleEntityService<TEntity> where TEntity : class, IVersionedTitleEntity
{
  protected readonly IVersionedTitleEntityDao<TEntity> entityDao;
  private readonly TitleDao titleDao = new();
  private readonly TitleVersionDao titleVersionDao = new();
  private readonly OrganizationDao organizationDao = new();

  protected VersionedTitleEntityService(IVersionedTitleEntityDao<TEntity> entityDao)
  {
    this.entityDao = entityDao;
  }

  protected async Task<List<TEntity>> ReadAllVersions(string titleId, string versionId)
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
      foreach (var entity in await entityDao.QueryByTitleVersion(version.RowKey))
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

  protected async Task PreserveForChildren(string titleId, string versionId, TEntity entity, bool isDeleted = false)
  {
    var children = (await titleVersionDao.QueryByTitle(titleId))
        .Where(version => version.ParentVersionId == versionId)
        .ToList();
    foreach (var child in children)
    {
      if (await entityDao.Get(child.RowKey, entity.RowKey) is not null)
      {
        continue;
      }

      await entityDao.Set(child.RowKey, CreateVersionEntity(entity, child.RowKey, isDeleted));
    }
  }

  protected async Task<TitleVersionEntity> EnsureAccess(string organizationId, string titleId, string versionId, string? user)
  {
    if (string.IsNullOrEmpty(user) || !await organizationDao.IsUserInOrganization(organizationId, user))
    {
      throw new UnauthorizedAccessException();
    }

    if (!await titleDao.TitleExists(organizationId, titleId))
    {
      throw new UnauthorizedAccessException();
    }

    var version = await titleVersionDao.Get(titleId, versionId);
    if (version is null || version.IsDeleted)
    {
      throw new UnauthorizedAccessException();
    }

    return version;
  }

  protected static void EnsureWritable(TitleVersionEntity version)
  {
    if (version.IsReadOnly)
    {
      throw new InvalidOperationException("This title version is read-only.");
    }
  }

  protected abstract TEntity CreateVersionEntity(TEntity entity, string versionId, bool isDeleted);
}
