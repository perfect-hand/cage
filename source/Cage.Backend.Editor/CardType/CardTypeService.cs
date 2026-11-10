using System.Text;
using System.Text.Json;
using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Tag;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.CardType;

public class CardTypeService : VersionedTitleEntityService<CardTypeEntity>
{
  public const int MaximumCardTypesPerTitle = 100;
  public const int MaximumCardTypeNameLength = 50;
  public const int MaximumAttributesPerCardType = 100;
  public const int MaximumTagsPerCardType = 100;
  private const int MaximumJsonPropertySize = 60 * 1024;

  private readonly CardTypeMapper mapper = new();
  private readonly AttributeService attributeService;
  private readonly TagService tagService;
  private readonly ILogger<CardTypeService> logger;

  public CardTypeService(
      ILogger<CardTypeService> logger,
      AttributeService attributeService,
      TagService tagService) : base(new CardTypeDao())
  {
    this.logger = logger;
    this.attributeService = attributeService;
    this.tagService = tagService;
  }

  public async Task<CardTypeDto> Create(string organizationId, string titleId, string versionId, CardTypeDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateName(dto);

    var cardTypes = await ReadAllVersions(titleId, versionId);
    if (cardTypes.Count >= MaximumCardTypesPerTitle)
    {
      throw new ArgumentException($"Titles cannot have more than {MaximumCardTypesPerTitle} card types.", nameof(dto));
    }

    await ValidateDto(organizationId, titleId, versionId, dto, cardTypes, user);
    var cardType = mapper.ToEntity(dto, versionId);
    await PreserveForChildren(titleId, versionId, cardType, isDeleted: true);
    await entityDao.Add(cardType);
    logger.LogInformation("Card type {} created in title version {}.", cardType.RowKey, versionId);
    return mapper.ToDto(cardType);
  }

  public async Task<List<CardTypeDto>> ReadAll(string organizationId, string titleId, string versionId, string? user)
  {
    await EnsureAccess(organizationId, titleId, versionId, user);
    return (await ReadAllVersions(titleId, versionId)).Select(mapper.ToDto).ToList();
  }

  public async Task<bool> Update(string organizationId, string titleId, string versionId, string cardTypeId, CardTypeDto dto, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateName(dto);

    var cardTypes = await ReadAllVersions(titleId, versionId);
    var existing = cardTypes.SingleOrDefault(cardType => cardType.RowKey == cardTypeId);
    if (existing is null)
    {
      return false;
    }

    await ValidateDto(organizationId, titleId, versionId, dto, cardTypes, user, cardTypeId);
    await PreserveForChildren(titleId, versionId, existing);
    existing.PartitionKey = versionId;
    existing.Name = dto.Name;
    existing.ParentId = dto.ParentId;
    existing.AttributesJson = JsonSerializer.Serialize(dto.Attributes);
    existing.TagsJson = JsonSerializer.Serialize(dto.Tags.Distinct(StringComparer.Ordinal));
    existing.IsDeleted = false;
    await entityDao.Update(versionId, existing);
    logger.LogInformation("Card type {} updated in title version {}.", cardTypeId, versionId);
    return true;
  }

  public async Task<bool> Delete(string organizationId, string titleId, string versionId, string cardTypeId, string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);

    var cardTypes = await ReadAllVersions(titleId, versionId);
    var existing = cardTypes.SingleOrDefault(cardType => cardType.RowKey == cardTypeId);
    if (existing is null)
    {
      return false;
    }

    if (cardTypes.Any(cardType => cardType.ParentId == cardTypeId))
    {
      throw new InvalidOperationException("Card types with child card types cannot be deleted.");
    }

    await PreserveForChildren(titleId, versionId, existing);
    if (version.ParentVersionId is null)
    {
      await entityDao.Delete(versionId, cardTypeId);
    }
    else
    {
      existing.PartitionKey = versionId;
      existing.IsDeleted = true;
      await entityDao.Update(versionId, existing);
    }
    logger.LogInformation("Card type {} deleted from title version {}.", cardTypeId, versionId);
    return true;
  }

  public async Task<Dictionary<string, int>> CountAttributeReferences(string titleId, string versionId)
  {
    var counts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var cardType in await ReadAllVersions(titleId, versionId))
    {
      var attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(cardType.AttributesJson) ?? new();
      foreach (var name in attributes.Keys)
      {
        counts[name] = counts.GetValueOrDefault(name) + 1;
      }
    }

    return counts;
  }

  public async Task<Dictionary<string, int>> CountTagReferences(string titleId, string versionId)
  {
    var counts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var cardType in await ReadAllVersions(titleId, versionId))
    {
      var tags = JsonSerializer.Deserialize<List<string>>(cardType.TagsJson) ?? new();
      foreach (var name in tags.Distinct(StringComparer.Ordinal))
      {
        counts[name] = counts.GetValueOrDefault(name) + 1;
      }
    }

    return counts;
  }

  public async Task RenameAttributeReferences(string titleId, string versionId, string oldName, string newName)
  {
    if (oldName == newName)
    {
      return;
    }

    var cardTypes = (await ReadAllVersions(titleId, versionId))
        .Where(cardType => (JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(cardType.AttributesJson) ?? new())
            .ContainsKey(oldName))
        .ToList();
    if (cardTypes.Any(cardType =>
            (JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(cardType.AttributesJson) ?? new())
            .ContainsKey(newName)))
    {
      throw new ArgumentException("A card type already contains an attribute with the new name.", nameof(newName));
    }

    foreach (var cardType in cardTypes)
    {
      await PreserveForChildren(titleId, versionId, cardType);
      var attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(cardType.AttributesJson) ?? new();
      attributes[newName] = attributes[oldName];
      attributes.Remove(oldName);
      cardType.PartitionKey = versionId;
      cardType.AttributesJson = JsonSerializer.Serialize(attributes);
      await entityDao.Update(versionId, cardType);
    }
  }

  public async Task RenameTagReferences(string titleId, string versionId, string oldName, string newName)
  {
    if (oldName == newName)
    {
      return;
    }

    foreach (var cardType in await ReadAllVersions(titleId, versionId))
    {
      var tags = JsonSerializer.Deserialize<List<string>>(cardType.TagsJson) ?? new();
      if (!tags.Contains(oldName, StringComparer.Ordinal))
      {
        continue;
      }

      await PreserveForChildren(titleId, versionId, cardType);
      cardType.PartitionKey = versionId;
      cardType.TagsJson = JsonSerializer.Serialize(
          tags.Select(tag => tag == oldName ? newName : tag).Distinct(StringComparer.Ordinal));
      await entityDao.Update(versionId, cardType);
    }
  }

  public async Task DeleteAttributeReferences(string titleId, string versionId, string name)
  {
    foreach (var cardType in await ReadAllVersions(titleId, versionId))
    {
      var attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(cardType.AttributesJson) ?? new();
      if (!attributes.Remove(name))
      {
        continue;
      }

      await PreserveForChildren(titleId, versionId, cardType);
      cardType.PartitionKey = versionId;
      cardType.AttributesJson = JsonSerializer.Serialize(attributes);
      await entityDao.Update(versionId, cardType);
    }
  }

  public async Task DeleteTagReferences(string titleId, string versionId, string name)
  {
    foreach (var cardType in await ReadAllVersions(titleId, versionId))
    {
      var tags = JsonSerializer.Deserialize<List<string>>(cardType.TagsJson) ?? new();
      if (tags.RemoveAll(tag => tag == name) > 0)
      {
        await PreserveForChildren(titleId, versionId, cardType);
        cardType.PartitionKey = versionId;
        cardType.TagsJson = JsonSerializer.Serialize(tags);
        await entityDao.Update(versionId, cardType);
      }
    }
  }

  protected override CardTypeEntity CreateEntityVersion(CardTypeEntity cardType, string versionId, bool isDeleted)
  {
    return new CardTypeEntity
    {
      PartitionKey = versionId,
      RowKey = cardType.RowKey,
      Name = cardType.Name,
      ParentId = cardType.ParentId,
      AttributesJson = cardType.AttributesJson,
      TagsJson = cardType.TagsJson,
      IsDeleted = isDeleted
    };
  }

  private async Task ValidateDto(
      string organizationId,
      string titleId,
      string versionId,
      CardTypeDto dto,
      List<CardTypeEntity> cardTypes,
      string? user,
      string? cardTypeId = null)
  {
    if (dto.Attributes is null || dto.Tags is null)
    {
      throw new ArgumentException("Card type attributes and tags are required.", nameof(dto));
    }

    if (dto.Attributes.Count > MaximumAttributesPerCardType || dto.Tags.Count > MaximumTagsPerCardType)
    {
      throw new ArgumentException(
          $"A card type cannot have more than {MaximumAttributesPerCardType} attributes or {MaximumTagsPerCardType} tags.",
          nameof(dto));
    }

    var attributesJson = JsonSerializer.Serialize(dto.Attributes);
    var tagsJson = JsonSerializer.Serialize(dto.Tags.Distinct(StringComparer.Ordinal));
    if (Encoding.UTF8.GetByteCount(attributesJson) > MaximumJsonPropertySize ||
        Encoding.UTF8.GetByteCount(tagsJson) > MaximumJsonPropertySize)
    {
      throw new ArgumentException("Card type data is too large.", nameof(dto));
    }

    var attributes = await attributeService.ReadAll(organizationId, titleId, versionId, user);
    var tags = await tagService.ReadAll(organizationId, titleId, versionId, user);
    foreach (var attribute in dto.Attributes)
    {
      if (attribute.Value is null)
      {
        throw new ArgumentException($"Card type attribute '{attribute.Key}' must have a value.", nameof(dto));
      }

      var definition = attributes.FirstOrDefault(item => item.Name == attribute.Key);
      if (definition is null || definition.Type != attribute.Value.Type)
      {
        throw new ArgumentException($"Card type attribute '{attribute.Key}' is not defined for this title version.", nameof(dto));
      }
    }

    var tagNames = tags.Select(tag => tag.Name).ToHashSet(StringComparer.Ordinal);
    if (dto.Tags.Any(tag => !tagNames.Contains(tag)))
    {
      throw new ArgumentException("Card type contains a tag that is not defined for this title version.", nameof(dto));
    }

    var cardTypeById = cardTypes.ToDictionary(cardType => cardType.RowKey);
    var visited = new HashSet<string>(StringComparer.Ordinal);
    var parentId = dto.ParentId;
    if (cardTypeId is not null)
    {
      visited.Add(cardTypeId);
    }
    while (parentId is not null)
    {
      if (!visited.Add(parentId))
      {
        throw new ArgumentException("Card type parent hierarchy contains a cycle.", nameof(dto));
      }

      if (!cardTypeById.TryGetValue(parentId, out var parent))
      {
        throw new ArgumentException("Card type parent does not exist in this title version.", nameof(dto));
      }
      parentId = parent.ParentId;
    }
  }

  private static void ValidateName(CardTypeDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > MaximumCardTypeNameLength)
    {
      throw new ArgumentException($"Card type names must be non-empty and cannot exceed {MaximumCardTypeNameLength} characters.", nameof(dto));
    }
  }
}
