using Cage.Simulation.Types;

namespace Cage.Simulation.Events;

public sealed class IntegerAttributeChangedEvent : GameEvent
{
    [EventProperty(CageType.Entity, "Entity")]
    public int EntityId { get; set; }

    [EventProperty(CageType.String)]
    public string Attribute { get; set; } = null!;

    [EventProperty(CageType.Int)]
    public int OldValue { get; set; }

    [EventProperty(CageType.Int)]
    public int NewValue { get; set; }
}
