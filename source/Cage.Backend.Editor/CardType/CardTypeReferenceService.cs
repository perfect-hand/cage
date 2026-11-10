using System.Text.Json;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.CardType;

public class CardTypeReferenceService : VersionedTitleEntityService<CardTypeEntity>
{
  public CardTypeReferenceService() : base(new CardTypeDao())
  {
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
}
