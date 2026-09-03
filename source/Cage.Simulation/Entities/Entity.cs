using Cage.Simulation.Types;

namespace Cage.Simulation.Entities;

public class Entity
{
    public int Id { get; private set; }
    public Dictionary<string, TypedValue> Attributes { get; private set; } = new();
    public List<string> Tags { get; private set; } = new();

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    public Entity() { }

    public Entity(int id)
    {
        Id = id;
    }
}