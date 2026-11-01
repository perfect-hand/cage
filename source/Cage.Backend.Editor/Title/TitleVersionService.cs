using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Organization;

namespace Cage.Backend.Editor.Title;

public class TitleVersionService
{
  public const int MaximumVersionsPerTitle = 100;
  public const int MaximumVersionNameLength = 50;

  private readonly TitleVersionMapper mapper = new();
  private readonly TitleVersionDao titleVersionDao = new();
  private readonly TitleDao titleDao = new();
  private readonly AttributeDao attributeDao = new();
  private readonly OrganizationDao organizationDao = new();
  private readonly ILogger<TitleVersionService> logger;

  public TitleVersionService(ILogger<TitleVersionService> logger)
  {
    this.logger = logger;
  }

  public async Task<List<TitleVersionDto>> ReadAll(string organizationId, string titleId, string? user)
  {
    await EnsureTitleAccess(organizationId, titleId, user);
    await EnsureInitialVersion(titleId);
    return (await titleVersionDao.QueryByTitle(titleId))
        .Where(version => !version.IsDeleted)
        .Select(mapper.ToDto)
        .ToList();
  }

  public async Task<TitleVersionDto> Create(string organizationId, string titleId, TitleVersionDto dto, string? user)
  {
    await EnsureTitleAccess(organizationId, titleId, user);
    ValidateName(dto.Name);
    await EnsureInitialVersion(titleId);

    var versions = await titleVersionDao.QueryByTitle(titleId);
    if (versions.Count(version => !version.IsDeleted) >= MaximumVersionsPerTitle)
    {
      throw new ArgumentException($"Titles cannot have more than {MaximumVersionsPerTitle} versions.", nameof(dto));
    }

    var parentVersionId = dto.ParentVersionId;
    var parent = parentVersionId is null ? null : await titleVersionDao.Get(titleId, parentVersionId);
    if (parentVersionId is not null && (parent is null || parent.IsDeleted))
    {
      throw new ArgumentException("Parent version does not exist.", nameof(dto));
    }

    var version = mapper.ToEntity(dto, titleId, parentVersionId);
    await titleVersionDao.Add(version);
    logger.LogInformation("Version {} created for title {}.", version.RowKey, titleId);
    return mapper.ToDto(version);
  }

  public async Task<bool> Rename(string organizationId, string titleId, string versionId, TitleVersionDto dto, string? user)
  {
    await EnsureTitleAccess(organizationId, titleId, user);
    ValidateName(dto.Name);
    await EnsureInitialVersion(titleId);
    var version = await GetActiveVersion(titleId, versionId);
    if (version is null)
    {
      return false;
    }

    version.Name = dto.Name;
    await titleVersionDao.Update(version);
    logger.LogInformation("Version {} renamed in title {}.", versionId, titleId);
    return true;
  }

  public async Task<bool> SetReadOnly(string organizationId, string titleId, string versionId, bool isReadOnly, string? user)
  {
    await EnsureTitleAccess(organizationId, titleId, user);
    await EnsureInitialVersion(titleId);
    var version = await GetActiveVersion(titleId, versionId);
    if (version is null)
    {
      return false;
    }

    version.IsReadOnly = isReadOnly;
    await titleVersionDao.Update(version);
    logger.LogInformation("Version {} read-only state set to {} in title {}.", versionId, isReadOnly, titleId);
    return true;
  }

  public async Task<bool> Delete(string organizationId, string titleId, string versionId, string? user)
  {
    await EnsureTitleAccess(organizationId, titleId, user);
    await EnsureInitialVersion(titleId);
    var version = await GetActiveVersion(titleId, versionId);
    if (version is null)
    {
      return false;
    }

    var versions = await titleVersionDao.QueryByTitle(titleId);
    if (versions.Any(candidate => candidate.ParentVersionId == versionId))
    {
      version.IsDeleted = true;
      await titleVersionDao.Update(version);
    }
    else
    {
      var parentVersionId = version.ParentVersionId;
      await attributeDao.DeleteByTitle(versionId);
      if (version.ParentSnapshotId is not null)
      {
        await attributeDao.DeleteByTitle(version.ParentSnapshotId);
      }

      if (versionId == titleId)
      {
        version.IsDeleted = true;
        await titleVersionDao.Update(version);
      }
      else
      {
        await titleVersionDao.Delete(titleId, versionId);
        await PruneDeletedAncestors(titleId, parentVersionId);
      }
    }

    logger.LogInformation("Version {} deleted from title {}.", versionId, titleId);
    return true;
  }

  private async Task EnsureInitialVersion(string titleId)
  {
    if (await titleVersionDao.Get(titleId, titleId) is null)
    {
      await titleVersionDao.Add(new TitleVersionEntity
      {
        PartitionKey = titleId,
        RowKey = titleId,
        Name = "Initial",
      });
    }

    private async Task PruneDeletedAncestors(string titleId, string? versionId)
    {
      while (versionId is not null)
      {
        var version = await titleVersionDao.Get(titleId, versionId);
        if (version is not { IsDeleted: true })
        {
          return;
        }

        var versions = await titleVersionDao.QueryByTitle(titleId);
        if (versions.Any(candidate => candidate.ParentVersionId == versionId))
        {
          return;
        }

        var parentVersionId = version.ParentVersionId;
        await attributeDao.DeleteByTitle(versionId);
        if (version.ParentSnapshotId is not null)
        {
          await attributeDao.DeleteByTitle(version.ParentSnapshotId);
        }

        if (versionId == titleId)
        {
          version.ParentVersionId = null;
          version.ParentSnapshotId = null;
          await titleVersionDao.Update(version);
          return;
        }

        await titleVersionDao.Delete(titleId, versionId);
        logger.LogInformation("Deleted version {} storage pruned from title {}.", versionId, titleId);
        versionId = parentVersionId;
      }
    }
  }

  private async Task<TitleVersionEntity?> GetActiveVersion(string titleId, string versionId)
  {
    var version = await titleVersionDao.Get(titleId, versionId);
    return version is { IsDeleted: false } ? version : null;
  }

  private static void ValidateName(string name)
  {
    if (name.Length > MaximumVersionNameLength)
    {
      throw new ArgumentException($"Version names cannot exceed {MaximumVersionNameLength} characters.", nameof(name));
    }
  }

  private async Task EnsureTitleAccess(string organizationId, string titleId, string? user)
  {
    if (string.IsNullOrEmpty(user) || !await organizationDao.IsUserInOrganization(organizationId, user))
    {
      throw new UnauthorizedAccessException();
    }

    if (!await titleDao.TitleExists(organizationId, titleId))
    {
      throw new UnauthorizedAccessException();
    }
  }
}
