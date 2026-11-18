using System.Text;
using System.Text.Json;
using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.CardSet;
using Cage.Backend.Editor.CardType;
using Cage.Backend.Editor.Tag;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.Card;

public class CardService : VersionedTitleEntityService<CardEntity>
{
  public const int MaximumCardsPerCardSet = 1000;
  public const int MaximumCardNameLength = 50;
  public const int MaximumAttributesPerCard = 100;
  public const int MaximumTagsPerCard = 100;
  private const int MaximumJsonPropertySize = 60 * 1024;

  private readonly CardMapper mapper = new();
  private readonly CardSetDao cardSetDao = new();
  private readonly CardTypeDao cardTypeDao = new();
  private readonly AttributeDao attributeDao = new();
  private readonly TagDao tagDao = new();
  private readonly ILogger<CardService> logger;

  public CardService(ILogger<CardService> logger) : base(new CardDao())
  {
    this.logger = logger;
  }

  public async Task<CardDto> Create(
      string organizationId,
      string titleId,
      string versionId,
      string cardSetId,
      CardDto dto,
      string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateName(dto);
    var cards = await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    if (cards.Count(card => card.CardSetId == cardSetId) >= MaximumCardsPerCardSet)
    {
      throw new ArgumentException($"A card set cannot have more than {MaximumCardsPerCardSet} cards.", nameof(dto));
    }

    await ValidateDto(titleId, versionId, cardSetId, dto);
    var card = mapper.ToEntity(dto, versionId, cardSetId);
    await PreserveForChildren(titleId, versionId, card, isDeleted: true);
    await entityDao.Add(card);
    logger.LogInformation("Card {} created in card set {} and title version {}.", card.RowKey, cardSetId, versionId);
    return mapper.ToDto(card);
  }

  public async Task<List<CardDto>> ReadAll(
      string organizationId,
      string titleId,
      string versionId,
      string cardSetId,
      string? user)
  {
    await EnsureAccess(organizationId, titleId, versionId, user);
    return (await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
        .Where(card => card.CardSetId == cardSetId)
        .Select(mapper.ToDto)
        .ToList();
  }

  public async Task<bool> Update(
      string organizationId,
      string titleId,
      string versionId,
      string cardSetId,
      string cardId,
      CardDto dto,
      string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);
    ValidateName(dto);

    var cards = await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    var existing = cards.SingleOrDefault(card => card.RowKey == cardId && card.CardSetId == cardSetId);
    if (existing is null)
    {
      return false;
    }

    await ValidateDto(titleId, versionId, cardSetId, dto);
    await PreserveForChildren(titleId, versionId, existing);
    existing.PartitionKey = versionId;
    existing.Name = dto.Name;
    existing.CardTypeId = dto.CardTypeId;
    existing.AttributesJson = JsonSerializer.Serialize(dto.Attributes);
    existing.TagsJson = JsonSerializer.Serialize(dto.Tags.Distinct(StringComparer.Ordinal));
    existing.IsDeleted = false;
    await entityDao.Update(versionId, existing);
    logger.LogInformation("Card {} updated in card set {} and title version {}.", cardId, cardSetId, versionId);
    return true;
  }

  public async Task<bool> Delete(
      string organizationId,
      string titleId,
      string versionId,
      string cardSetId,
      string cardId,
      string? user)
  {
    var version = await EnsureAccess(organizationId, titleId, versionId, user);
    EnsureWritable(version);

    var cards = await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    var existing = cards.SingleOrDefault(card => card.RowKey == cardId && card.CardSetId == cardSetId);
    if (existing is null)
    {
      return false;
    }

    await PreserveForChildren(titleId, versionId, existing);
    if (version.ParentVersionId is null)
    {
      await entityDao.Delete(versionId, cardId);
    }
    else
    {
      existing.PartitionKey = versionId;
      existing.IsDeleted = true;
      await entityDao.Update(versionId, existing);
    }
    logger.LogInformation("Card {} deleted from card set {} and title version {}.", cardId, cardSetId, versionId);
    return true;
  }

  public async Task<Dictionary<string, int>> CountAttributeReferences(string titleId, string versionId)
  {
    var counts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var card in await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
    {
      var attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(card.AttributesJson) ?? new();
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
    foreach (var card in await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
    {
      var tags = JsonSerializer.Deserialize<List<string>>(card.TagsJson) ?? new();
      foreach (var name in tags.Distinct(StringComparer.Ordinal))
      {
        counts[name] = counts.GetValueOrDefault(name) + 1;
      }
    }

    return counts;
  }

  public async Task RenameAttributeReferences(string titleId, string versionId, string oldName, string newName)
  {
    await ValidateAttributeRenameReferences(titleId, versionId, oldName, newName);
    if (oldName == newName)
    {
      return;
    }

    var cards = (await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
        .Where(card => (JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(card.AttributesJson) ?? new())
            .ContainsKey(oldName))
        .ToList();
    foreach (var card in cards)
    {
      await PreserveForChildren(titleId, versionId, card);
      var attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(card.AttributesJson) ?? new();
      attributes[newName] = attributes[oldName];
      attributes.Remove(oldName);
      card.PartitionKey = versionId;
      card.AttributesJson = JsonSerializer.Serialize(attributes);
      await entityDao.Update(versionId, card);
    }
  }

  public async Task ValidateAttributeRenameReferences(string titleId, string versionId, string oldName, string newName)
  {
    if (oldName == newName)
    {
      return;
    }

    if ((await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId)).Any(card =>
            (JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(card.AttributesJson) ?? new())
            .ContainsKey(oldName) &&
            (JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(card.AttributesJson) ?? new())
            .ContainsKey(newName)))
    {
      throw new ArgumentException("A card already contains an attribute with the new name.", nameof(newName));
    }
  }

  public async Task RenameTagReferences(string titleId, string versionId, string oldName, string newName)
  {
    if (oldName == newName)
    {
      return;
    }

    foreach (var card in await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
    {
      var tags = JsonSerializer.Deserialize<List<string>>(card.TagsJson) ?? new();
      if (!tags.Contains(oldName, StringComparer.Ordinal))
      {
        continue;
      }

      await PreserveForChildren(titleId, versionId, card);
      card.PartitionKey = versionId;
      card.TagsJson = JsonSerializer.Serialize(
          tags.Select(tag => tag == oldName ? newName : tag).Distinct(StringComparer.Ordinal));
      await entityDao.Update(versionId, card);
    }
  }

  public async Task DeleteAttributeReferences(string titleId, string versionId, string name)
  {
    foreach (var card in await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
    {
      var attributes = JsonSerializer.Deserialize<Dictionary<string, TypedValue>>(card.AttributesJson) ?? new();
      if (!attributes.Remove(name))
      {
        continue;
      }

      await PreserveForChildren(titleId, versionId, card);
      card.PartitionKey = versionId;
      card.AttributesJson = JsonSerializer.Serialize(attributes);
      await entityDao.Update(versionId, card);
    }
  }

  public async Task DeleteTagReferences(string titleId, string versionId, string name)
  {
    foreach (var card in await entityDao.QueryByTitleVersionAndAncestors(titleId, versionId))
    {
      var tags = JsonSerializer.Deserialize<List<string>>(card.TagsJson) ?? new();
      if (tags.RemoveAll(tag => tag == name) > 0)
      {
        await PreserveForChildren(titleId, versionId, card);
        card.PartitionKey = versionId;
        card.TagsJson = JsonSerializer.Serialize(tags);
        await entityDao.Update(versionId, card);
      }
    }
  }

  protected override CardEntity CreateEntityVersion(CardEntity card, string versionId, bool isDeleted)
  {
    return new CardEntity
    {
      PartitionKey = versionId,
      RowKey = card.RowKey,
      Name = card.Name,
      CardSetId = card.CardSetId,
      CardTypeId = card.CardTypeId,
      AttributesJson = card.AttributesJson,
      TagsJson = card.TagsJson,
      IsDeleted = isDeleted
    };
  }

  private async Task ValidateDto(string titleId, string versionId, string cardSetId, CardDto dto)
  {
    if (dto.Attributes is null || dto.Tags is null || string.IsNullOrWhiteSpace(dto.CardTypeId))
    {
      throw new ArgumentException("Card type, attributes, and tags are required.", nameof(dto));
    }

    if (dto.Attributes.Count > MaximumAttributesPerCard || dto.Tags.Count > MaximumTagsPerCard)
    {
      throw new ArgumentException(
          $"A card cannot have more than {MaximumAttributesPerCard} attributes or {MaximumTagsPerCard} tags.",
          nameof(dto));
    }

    var attributesJson = JsonSerializer.Serialize(dto.Attributes);
    var tagsJson = JsonSerializer.Serialize(dto.Tags.Distinct(StringComparer.Ordinal));
    if (Encoding.UTF8.GetByteCount(attributesJson) > MaximumJsonPropertySize ||
        Encoding.UTF8.GetByteCount(tagsJson) > MaximumJsonPropertySize)
    {
      throw new ArgumentException("Card data is too large.", nameof(dto));
    }

    var cardSets = await cardSetDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    if (!cardSets.Any(cardSet => cardSet.RowKey == cardSetId))
    {
      throw new ArgumentException("Card set does not exist in this title version.", nameof(dto));
    }

    var cardTypes = await cardTypeDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    if (!cardTypes.Any(cardType => cardType.RowKey == dto.CardTypeId))
    {
      throw new ArgumentException("Card type does not exist in this title version.", nameof(dto));
    }

    var attributes = await attributeDao.QueryByTitleVersionAndAncestors(titleId, versionId);
    foreach (var attribute in dto.Attributes)
    {
      if (attribute.Value is null)
      {
        throw new ArgumentException($"Card attribute '{attribute.Key}' must have a value.", nameof(dto));
      }

      var definition = attributes.FirstOrDefault(item => item.Name == attribute.Key);
      if (definition is null || definition.Type != attribute.Value.Type)
      {
        throw new ArgumentException($"Card attribute '{attribute.Key}' is not defined for this title version.", nameof(dto));
      }
    }

    var tagNames = (await tagDao.QueryByTitleVersionAndAncestors(titleId, versionId))
        .Select(tag => tag.Name)
        .ToHashSet(StringComparer.Ordinal);
    if (dto.Tags.Any(tag => !tagNames.Contains(tag)))
    {
      throw new ArgumentException("Card contains a tag that is not defined for this title version.", nameof(dto));
    }
  }

  private static void ValidateName(CardDto dto)
  {
    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > MaximumCardNameLength)
    {
      throw new ArgumentException($"Card names must be non-empty and cannot exceed {MaximumCardNameLength} characters.", nameof(dto));
    }
  }
}
