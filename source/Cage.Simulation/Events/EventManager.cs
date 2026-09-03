using System.Collections.Generic;
using System.Linq;
using Cage.Simulation.Entities;

namespace Cage.Simulation.Events;

public class EventManager
{
    public IReadOnlyList<Trigger> HandleEvent(GameEvent e, IEnumerable<Trigger> triggers, EntityManager entityManager)
    {
        var executed = new List<Trigger>();
        var contextBuilder = new EvaluationContextBuilder();
        var context = contextBuilder.BuildContextForEvent(e, entityManager);

        foreach (var trigger in triggers)
        {
            if (!trigger.MatchesEvent(e))
                continue;

            if (!trigger.AreAllConditionsFulfilled(context))
                continue;

            trigger.ExecuteActions(context);
            executed.Add(trigger);
        }

        return executed;
    }
}
