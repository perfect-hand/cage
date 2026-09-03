using Cage.Simulation.Types;
using Cage.Simulation.Entities;

namespace Cage.Simulation.Expressions;

public class EvaluationContext
{
    public Dictionary<string, TypedValue> Variables { get; } = new();
    public EntityManager EntityManager { get; }

    public EvaluationContext(EntityManager entityManager)
    {
        EntityManager = entityManager;
    }
}
