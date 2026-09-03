using Cage.Simulation.Types;

namespace Cage.Simulation.Entities;

/// <summary>
/// A blueprint for creating entities, containing default attributes and tags.
/// </summary>
public class EntityBlueprint
{
    public string Id { get; private set; }
    public Dictionary<string, TypedValue> Attributes { get; private set; } = new();
    public List<string> Tags { get; private set; } = new();

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    public EntityBlueprint() 
    {
        Id = string.Empty;
    }

    public EntityBlueprint(string id)
    {
        Id = id;
    }
}