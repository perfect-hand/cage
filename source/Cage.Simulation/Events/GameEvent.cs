namespace Cage.Simulation.Events;

public abstract class GameEvent
{
    public string Name => GetType().Name;
}
