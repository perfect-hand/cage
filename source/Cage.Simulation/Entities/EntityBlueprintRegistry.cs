namespace Cage.Simulation.Entities;

/// <summary>
/// Registry for looking up EntityBlueprints by string id.
/// </summary>
public class EntityBlueprintRegistry
{
    private readonly Dictionary<string, EntityBlueprint> blueprints = new();

    public IReadOnlyDictionary<string, EntityBlueprint> Blueprints => blueprints;

    public void Register(EntityBlueprint blueprint)
    {
        blueprints[blueprint.Id] = blueprint;
    }

    public EntityBlueprint? Get(string id)
    {
        return blueprints.TryGetValue(id, out var blueprint) ? blueprint : null;
    }

    public bool Contains(string id)
    {
        return blueprints.ContainsKey(id);
    }
}