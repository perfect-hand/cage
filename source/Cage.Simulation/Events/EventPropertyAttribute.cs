using Cage.Simulation.Types;

namespace Cage.Simulation.Events;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EventPropertyAttribute : Attribute
{
    public CageType Type { get; }
    public string? PropertyName { get; }

    public EventPropertyAttribute(CageType type, string? propertyName = null)
    {
        Type = type;
        PropertyName = propertyName;
    }
}