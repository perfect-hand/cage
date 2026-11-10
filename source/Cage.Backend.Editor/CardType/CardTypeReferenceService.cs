using System.Text.Json;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.CardType;

public class CardTypeReferenceService
{
  private readonly CardTypeDao cardTypeDao = new();
  private readonly TitleVersionDao titleVersionDao = new();

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
      await cardTypeDao.Update(versionId, cardType);
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
      await cardTypeDao.Update(versionId, cardType);
    }
  }

  private async Task<List<CardTypeEntity>> ReadAllVersions(string titleId, string versionId)
  {
    var versions = new List<TitleVersionEntity>();
    var visited = new HashSet<string>(StringComparer.Ordinal);
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

    var cardTypesById = new Dictionary<string, CardTypeEntity>(StringComparer.Ordinal);
    foreach (var version in versions.AsEnumerable().Reverse())
    {
      foreach (var cardType in await cardTypeDao.QueryByTitleVersion(version.RowKey))
      {
        if (cardType.IsDeleted)
        {
          cardTypesById.Remove(cardType.RowKey);
        }
        else
        {
          cardTypesById[cardType.RowKey] = cardType;
        }
      }
    }

    return cardTypesById.Values.ToList();
  }

  private async Task PreserveForChildren(string titleId, string versionId, CardTypeEntity cardType)
  {
    var children = (await titleVersionDao.QueryByTitle(titleId))
        .Where(version => version.ParentVersionId == versionId);
    foreach (var child in children)
    {
      if (await cardTypeDao.Get(child.RowKey, cardType.RowKey) is not null)
      {
        continue;
      }

      await cardTypeDao.Update(child.RowKey, new CardTypeEntity
      {
        PartitionKey = child.RowKey,
        RowKey = cardType.RowKey,
        Name = cardType.Name,
        ParentId = cardType.ParentId,
        AttributesJson = cardType.AttributesJson,
        TagsJson = cardType.TagsJson,
        IsDeleted = cardType.IsDeleted
      });
    }
  }
}
