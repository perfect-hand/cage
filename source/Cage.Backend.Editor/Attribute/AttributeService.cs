using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Attribute;

public class AttributeService
{
  public const int MaximumAttributesPerTitle = 100;
  public const int MaximumAttributeNameLength = 50;

  private readonly AttributeMapper mapper = new();
  private readonly AttributeDao attributeDao = new();
  private readonly TitleDao titleDao = new();
  private readonly TitleVersionDao titleVersionDao = new();
  private readonly OrganizationDao organizationDao = new();
  private readonly ILogger<AttributeService> logger;

  public AttributeService(ILogger<AttributeService> logger)
  {
    this.logger = logger;
  }

  public async Task<AttributeDto> Create(string organizationId, string titleId, string versionId, AttributeDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    if ((await ReadAllVersions(titleId, versionId)).Count >= MaximumAttributesPerTitle)
    {
      throw new ArgumentException($"Titles cannot have more than {MaximumAttributesPerTitle} attributes.", nameof(dto));
    }

    await FreezeDescendants(titleId, versionId);
    var attribute = mapper.ToEntity(dto, versionId);
    await attributeDao.AddAttribute(attribute);
    logger.LogInformation("Attribute {} created in title version {}.", attribute.RowKey, versionId);
    return mapper.ToDto(attribute);
  }

  public async Task<List<AttributeDto>> ReadAll(string organizationId, string titleId, string versionId, string? user)
  {
    await EnsureAccess(organizationId, titleId, versionId, user);
    return (await ReadAllVersions(titleId, versionId)).Select(mapper.ToDto).ToList();
  }

  public async Task<bool> Update(string organizationId, string titleId, string versionId, string attributeId, AttributeDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    var attributes = await ReadAllVersions(titleId, versionId);
    var existing = attributes.SingleOrDefault(attribute => attribute.RowKey == attributeId);
    if (existing is null)
    {
      return false;
    }

    await FreezeDescendants(titleId, versionId);
    existing.PartitionKey = versionId;
    existing.Name = dto.Name;
    existing.Type = dto.Type;
    await attributeDao.SetAttributeTitleVersion(versionId, existing);
    logger.LogInformation("Attribute {} updated in title version {}.", attributeId, versionId);
    return true;
  }

  public async Task<bool> Delete(string organizationId, string titleId, string versionId, string attributeId, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);

    var attributes = await ReadAllVersions(titleId, versionId);
    var existing = attributes.SingleOrDefault(attribute => attribute.RowKey == attributeId);
    if (existing is null)
    {
      return false;
    }

    await FreezeDescendants(titleId, versionId);
    if (version.ParentVersionId is null && version.ParentSnapshotId is null)
    {
      await attributeDao.DeleteAttributeTitleVersion(versionId, attributeId);
    }
    else
    {
      existing.PartitionKey = versionId;
      existing.IsDeleted = true;
      await attributeDao.SetAttributeTitleVersion(versionId, existing);
    }
    logger.LogInformation("Attribute {} deleted from title version {}.", attributeId, versionId);
    return true;
  }

  private async Task<List<AttributeEntity>> ReadAllVersions(string titleId, string versionId)
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

    if (versions.Count == 0)
    {
      throw new UnauthorizedAccessException();
    }

    var attributesById = new Dictionary<string, AttributeEntity>();
    foreach (var version in versions.AsEnumerable().Reverse())
    {
      if (version.ParentSnapshotId is not null)
      {
        foreach (var attribute in await attributeDao.QueryByTitleVersion(version.ParentSnapshotId))
        {
          attributesById[attribute.RowKey] = attribute;
        }
      }

      foreach (var attribute in await attributeDao.QueryByTitleVersion(version.RowKey))
      {
        if (attribute.IsDeleted)
        {
          attributesById.Remove(attribute.RowKey);
        }
        else
        {
          attributesById[attribute.RowKey] = attribute;
        }
      }
    }

    return attributesById.Values.ToList();
  }

  private async Task FreezeDescendants(string titleId, string versionId)
  {
    var children = (await titleVersionDao.QueryByTitle(titleId))
        .Where(version => version.ParentVersionId == versionId)
        .ToList();
    foreach (var child in children)
    {
      var attributes = await ReadAllVersions(titleId, child.RowKey);
      var snapshotId = Guid.NewGuid().ToString();
      await attributeDao.AddVersionSnapshot(snapshotId, attributes);
      child.ParentVersionId = null;
      child.ParentSnapshotId = snapshotId;
      await titleVersionDao.Update(child);
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

  private static void ValidateDto(AttributeDto dto)
  {
    if (dto.Name.Length > MaximumAttributeNameLength)
    {
      throw new ArgumentException($"Attribute names cannot exceed {MaximumAttributeNameLength} characters.", nameof(dto));
    }

    if (!Enum.IsDefined(dto.Type))
    {
      throw new ArgumentException("Attribute type is invalid.", nameof(dto));
    }
  }
}
