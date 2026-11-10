using Cage.Backend.Editor.CardType;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Tag;

public class TagService : VersionedTitleEntityService<TagEntity>
{
  public const int MaximumTagsPerTitle = 100;
  public const int MaximumTagNameLength = 50;

  private readonly TagMapper mapper = new();
  private readonly CardTypeService cardTypeService;
  private readonly ILogger<TagService> logger;

  public TagService(ILogger<TagService> logger, CardTypeService cardTypeService) : base(new TagDao())
  {
    this.logger = logger;
    this.cardTypeService = cardTypeService;
  }

  public async Task<TagDto> Create(string organizationId, string titleId, string versionId, TagDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    if ((await entityDao.FindAllVersions(titleId, versionId)).Count >= MaximumTagsPerTitle)
    {
      throw new ArgumentException($"Titles cannot have more than {MaximumTagsPerTitle} tags.", nameof(dto));
    }

    var tag = mapper.ToEntity(dto, versionId);
    await PreserveForChildren(titleId, versionId, tag, isDeleted: true);
    await entityDao.Add(tag);
    logger.LogInformation("Tag {} created in title version {}.", tag.RowKey, versionId);
    return mapper.ToDto(tag);
  }

  public async Task<List<TagDto>> ReadAll(string organizationId, string titleId, string versionId, string? user)
  {
    await EnsureAccess(organizationId, titleId, versionId, user);
    var referenceCounts = await cardTypeService.CountTagReferences(titleId, versionId);
    return (await entityDao.FindAllVersions(titleId, versionId))
        .Select(tag =>
        {
          var dto = mapper.ToDto(tag);
          dto.ReferenceCount = referenceCounts.GetValueOrDefault(tag.Name);
          return dto;
        })
        .ToList();
  }

  public async Task<bool> Update(string organizationId, string titleId, string versionId, string tagId, TagDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    var tags = await entityDao.FindAllVersions(titleId, versionId);
    var existing = tags.SingleOrDefault(tag => tag.RowKey == tagId);
    if (existing is null)
    {
      return false;
    }

    await cardTypeService.RenameTagReferences(titleId, versionId, existing.Name, dto.Name);
    await PreserveForChildren(titleId, versionId, existing);
    existing.PartitionKey = versionId;
    existing.Name = dto.Name;
    existing.IsDeleted = false;
    await entityDao.Update(versionId, existing);
    logger.LogInformation("Tag {} updated in title version {}.", tagId, versionId);
    return true;
  }

  public async Task<bool> Delete(string organizationId, string titleId, string versionId, string tagId, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);

    var tags = await entityDao.FindAllVersions(titleId, versionId);
    var existing = tags.SingleOrDefault(tag => tag.RowKey == tagId);
    if (existing is null)
    {
      return false;
    }

    await cardTypeService.DeleteTagReferences(titleId, versionId, existing.Name);
    await PreserveForChildren(titleId, versionId, existing);
    if (version.ParentVersionId is null)
    {
      await entityDao.Delete(versionId, tagId);
    }
    else
    {
      existing.PartitionKey = versionId;
      existing.IsDeleted = true;
      await entityDao.Update(versionId, existing);
    }
    logger.LogInformation("Tag {} deleted from title version {}.", tagId, versionId);
    return true;
  }

  protected override TagEntity CreateEntityVersion(TagEntity tag, string versionId, bool isDeleted)
  {
    return new TagEntity
    {
      PartitionKey = versionId,
      RowKey = tag.RowKey,
      Name = tag.Name,
      IsDeleted = isDeleted
    };
  }

  private static void ValidateDto(TagDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > MaximumTagNameLength)
    {
      throw new ArgumentException($"Tag names must be non-empty and cannot exceed {MaximumTagNameLength} characters.", nameof(dto));
    }
  }
}
