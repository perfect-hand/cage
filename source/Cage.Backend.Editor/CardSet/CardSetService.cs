using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.CardSet;

public class CardSetService : VersionedTitleEntityService<CardSetEntity>
{
  public const int MaximumCardSetsPerTitle = 100;
  public const int MaximumCardSetNameLength = 50;

  private readonly CardSetMapper mapper = new();
  private readonly ILogger<CardSetService> logger;

  public CardSetService(ILogger<CardSetService> logger) : base(new CardSetDao())
  {
    this.logger = logger;
  }

  public async Task<CardSetDto> Create(string organizationId, string titleId, string versionId, CardSetDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    if ((await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId)).Count >= MaximumCardSetsPerTitle)
    {
      throw new ArgumentException($"Titles cannot have more than {MaximumCardSetsPerTitle} card sets.", nameof(dto));
    }

    var cardSet = mapper.ToEntity(dto, versionId);
    await PreserveForChildren(titleId, versionId, cardSet, isDeleted: true);
    await entityDao.Add(cardSet);
    logger.LogInformation("Card set {} created in title version {}.", cardSet.RowKey, versionId);
    return mapper.ToDto(cardSet);
  }

  public async Task<List<CardSetDto>> ReadAll(string organizationId, string titleId, string versionId, string? user)
  {
    await EnsureAccess(organizationId, titleId, versionId, user);
    return (await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
        .Select(mapper.ToDto)
        .ToList();
  }

  public async Task<bool> Update(string organizationId, string titleId, string versionId, string cardSetId, CardSetDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateDto(dto);

    var cardSets = await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    var existing = cardSets.SingleOrDefault(cardSet => cardSet.RowKey == cardSetId);
    if (existing is null)
    {
      return false;
    }

    await PreserveForChildren(titleId, versionId, existing);
    existing.PartitionKey = versionId;
    existing.Name = dto.Name;
    existing.IsDeleted = false;
    await entityDao.Update(versionId, existing);
    logger.LogInformation("Card set {} updated in title version {}.", cardSetId, versionId);
    return true;
  }

  public async Task<bool> Delete(string organizationId, string titleId, string versionId, string cardSetId, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);

    var cardSets = await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    var existing = cardSets.SingleOrDefault(cardSet => cardSet.RowKey == cardSetId);
    if (existing is null)
    {
      return false;
    }

    await PreserveForChildren(titleId, versionId, existing);
    if (version.ParentVersionId is null)
    {
      await entityDao.Delete(versionId, cardSetId);
    }
    else
    {
      existing.PartitionKey = versionId;
      existing.IsDeleted = true;
      await entityDao.Update(versionId, existing);
    }
    logger.LogInformation("Card set {} deleted from title version {}.", cardSetId, versionId);
    return true;
  }

  protected override CardSetEntity CreateEntityVersion(CardSetEntity cardSet, string versionId, bool isDeleted)
  {
    return new CardSetEntity
    {
      PartitionKey = versionId,
      RowKey = cardSet.RowKey,
      Name = cardSet.Name,
      IsDeleted = isDeleted
    };
  }

  private static void ValidateDto(CardSetDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > MaximumCardSetNameLength)
    {
      throw new ArgumentException($"Card set names must be non-empty and cannot exceed {MaximumCardSetNameLength} characters.", nameof(dto));
    }
  }
}
