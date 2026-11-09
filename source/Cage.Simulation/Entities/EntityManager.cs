using System.Linq;

namespace Cage.Simulation.Entities;

/// <summary>
/// Manages entities within a match, handling creation, lookup, and blueprint resolution.
/// </summary>
public class EntityManager
{
  private readonly List<Entity> entities = new();
  private readonly EntityBlueprintRegistry blueprintRegistry;
  private int nextEntityId = 1;

  public IReadOnlyList<Entity> Entities => entities.AsReadOnly();
  public EntityBlueprintRegistry BlueprintRegistry => blueprintRegistry;

  public EntityManager() : this(new EntityBlueprintRegistry())
  {
  }

  public EntityManager(EntityBlueprintRegistry blueprintRegistry)
  {
    this.blueprintRegistry = blueprintRegistry;
  }

  /// <summary>
  /// Finds all entities that have the provided tag.
  /// </summary>
  public List<Entity> FindEntitiesByTag(string tag)
  {
    return entities.Where(e => e.Tags.Contains(tag)).ToList();
  }

  public Entity CreateEntity()
  {
    var entity = new Entity(nextEntityId++);
    entities.Add(entity);
    return entity;
  }

  /// <summary>
  /// Creates one or more entities from string blueprint ids.
  /// </summary>
  public IReadOnlyList<Entity> CreateEntitiesFromBlueprintIds(IEnumerable<string> blueprintIds)
  {
    var createdEntities = new List<Entity>();

    foreach (var blueprintId in blueprintIds)
    {
      var entity = CreateEntityFromBlueprint(blueprintId);
      if (entity != null)
      {
        createdEntities.Add(entity);
      }
    }

    return createdEntities.AsReadOnly();
  }

  /// <summary>
  /// Creates a single entity from a string blueprint id.
  /// </summary>
  public Entity? CreateEntityFromBlueprint(string blueprintId)
  {
    var blueprint = blueprintRegistry.Get(blueprintId);
    if (blueprint == null)
    {
      return null;
    }

    var entity = new Entity(nextEntityId);
    ApplyBlueprint(entity, blueprint, new HashSet<string>());
    nextEntityId++;
    entities.Add(entity);
    return entity;
  }

  private void ApplyBlueprint(Entity entity, EntityBlueprint blueprint, HashSet<string> visited)
  {
    if (!visited.Add(blueprint.Id))
    {
      throw new InvalidOperationException("The entity blueprint hierarchy contains a cycle.");
    }

    if (blueprint.ParentId is not null)
    {
      var parent = blueprintRegistry.Get(blueprint.ParentId)
          ?? throw new InvalidOperationException($"Parent entity blueprint '{blueprint.ParentId}' does not exist.");
      ApplyBlueprint(entity, parent, visited);
    }

    foreach (var attribute in blueprint.Attributes)
    {
      entity.Attributes[attribute.Key] = attribute.Value;
    }

    foreach (var tag in blueprint.Tags)
    {
      if (!entity.Tags.Contains(tag))
      {
        entity.Tags.Add(tag);
      }
    }
  }

  public Entity? FindEntity(int id)
  {
    return entities.FirstOrDefault(entity => entity.Id == id);
  }

  public Entity? GetLastCreatedEntity()
  {
    return entities.LastOrDefault();
  }
}
