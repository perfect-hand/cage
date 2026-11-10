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

      await entityDao.Update(child.RowKey, CreateEntityVersion(entity, child.RowKey, isDeleted));
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

  protected abstract TEntity CreateEntityVersion(TEntity entity, string versionId, bool isDeleted);
}
