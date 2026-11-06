using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Attribute;

public class AttributeService : VersionedTitleEntityService<AttributeEntity>
{
  public const int MaximumAttributesPerTitle = 100;
  public const int MaximumAttributeNameLength = 50;

  private readonly AttributeMapper mapper = new();
  private readonly ILogger<AttributeService> logger;

  public AttributeService(ILogger<AttributeService> logger) : base(new AttributeDao())
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

    var attribute = mapper.ToEntity(dto, versionId);
    await PreserveForChildren(titleId, versionId, attribute, isDeleted: true);
    await entityDao.Add(attribute);
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

    await PreserveForChildren(titleId, versionId, existing);
    existing.PartitionKey = versionId;
    existing.Name = dto.Name;
    existing.Type = dto.Type;
    existing.IsDeleted = false;
    await entityDao.Update(versionId, existing);
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

    await PreserveForChildren(titleId, versionId, existing);
    if (version.ParentVersionId is null)
    {
      await entityDao.Delete(versionId, attributeId);
    }
    else
    {
      existing.PartitionKey = versionId;
      existing.IsDeleted = true;
      await entityDao.Update(versionId, existing);
    }
    logger.LogInformation("Attribute {} deleted from title version {}.", attributeId, versionId);
    return true;
  }

  protected override AttributeEntity CreateEntityVersion(AttributeEntity attribute, string versionId, bool isDeleted)
  {
    return new AttributeEntity
    {
      PartitionKey = versionId,
      RowKey = attribute.RowKey,
      Name = attribute.Name,
      Type = attribute.Type,
      IsDeleted = isDeleted
    };
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
