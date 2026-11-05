using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Tag;

public class TagService
{
  public const int MaximumTagsPerTitle = 100;
  public const int MaximumTagNameLength = 50;

  private readonly TagMapper mapper = new();
  private readonly TagDao tagDao = new();
  private readonly TitleDao titleDao = new();
  private readonly TitleVersionDao titleVersionDao = new();
  private readonly OrganizationDao organizationDao = new();
  private readonly ILogger<TagService> logger;

  public TagService(ILogger<TagService> logger)
  {
    this.logger = logger;
  }

  public async Task<TagDto> Create(string organizationId, string titleId, string versionId, TagDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    if ((await ReadAllVersions(titleId, versionId)).Count >= MaximumTagsPerTitle)
    {
      throw new ArgumentException($"Titles cannot have more than {MaximumTagsPerTitle} tags.", nameof(dto));
    }

    var tag = mapper.ToEntity(dto, versionId);
    await PreserveTagForChildren(titleId, versionId, tag, isDeleted: true);
    await tagDao.AddTag(tag);
    logger.LogInformation("Tag {} created in title version {}.", tag.RowKey, versionId);
    return mapper.ToDto(tag);
  }

  public async Task<List<TagDto>> ReadAll(string organizationId, string titleId, string versionId, string? user)
  {
    await EnsureAccess(organizationId, titleId, versionId, user);
    return (await ReadAllVersions(titleId, versionId)).Select(mapper.ToDto).ToList();
  }

  public async Task<bool> Update(string organizationId, string titleId, string versionId, string tagId, TagDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    var tags = await ReadAllVersions(titleId, versionId);
    var existing = tags.SingleOrDefault(tag => tag.RowKey == tagId);
    if (existing is null)
    {
      return false;
    }

    await PreserveTagForChildren(titleId, versionId, existing);
    existing.PartitionKey = versionId;
    existing.Name = dto.Name;
    existing.IsDeleted = false;
    await tagDao.SetTagTitleVersion(versionId, existing);
    logger.LogInformation("Tag {} updated in title version {}.", tagId, versionId);
    return true;
  }

  public async Task<bool> Delete(string organizationId, string titleId, string versionId, string tagId, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);

    var tags = await ReadAllVersions(titleId, versionId);
    var existing = tags.SingleOrDefault(tag => tag.RowKey == tagId);
    if (existing is null)
    {
      return false;
    }

    await PreserveTagForChildren(titleId, versionId, existing);
    if (version.ParentVersionId is null)
    {
      await tagDao.DeleteTagTitleVersion(versionId, tagId);
    }
    else
    {
      existing.PartitionKey = versionId;
      existing.IsDeleted = true;
      await tagDao.SetTagTitleVersion(versionId, existing);
    }
    logger.LogInformation("Tag {} deleted from title version {}.", tagId, versionId);
    return true;
  }

  private async Task<List<TagEntity>> ReadAllVersions(string titleId, string versionId)
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

    var tagsById = new Dictionary<string, TagEntity>();
    foreach (var version in versions.AsEnumerable().Reverse())
    {
      foreach (var tag in await tagDao.QueryByTitleVersion(version.RowKey))
      {
        if (tag.IsDeleted)
        {
          tagsById.Remove(tag.RowKey);
        }
        else
        {
          tagsById[tag.RowKey] = tag;
        }
      }
    }

    return tagsById.Values.ToList();
  }

  private async Task PreserveTagForChildren(string titleId, string versionId, TagEntity tag, bool isDeleted = false)
  {
    var children = (await titleVersionDao.QueryByTitle(titleId))
        .Where(version => version.ParentVersionId == versionId)
        .ToList();
    foreach (var child in children)
    {
      if (await tagDao.GetTagTitleVersion(child.RowKey, tag.RowKey) is not null)
      {
        continue;
      }

      await tagDao.SetTagTitleVersion(child.RowKey, new TagEntity
      {
        PartitionKey = child.RowKey,
        RowKey = tag.RowKey,
        Name = tag.Name,
        IsDeleted = isDeleted
      });
    }
  }

  private async Task<TitleVersionEntity> EnsureAccess(string organizationId, string titleId, string versionId, string? user)
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

  private static void EnsureWritable(TitleVersionEntity version)
  {
    if (version.IsReadOnly)
    {
      throw new InvalidOperationException("This title version is read-only.");
    }
  }

  private static void ValidateDto(TagDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > MaximumTagNameLength)
    {
      throw new ArgumentException($"Tag names must be non-empty and cannot exceed {MaximumTagNameLength} characters.", nameof(dto));
    }
  }
}
