using System;
using System.Collections.Generic;
using System.Linq;
using Cage.Simulation.Expressions;
using Cage.Simulation.Mutations;

namespace Cage.Simulation.Events;

public sealed class Trigger
{
    public string EventName { get; set; } = string.Empty;
    public List<ExpressionCondition> Conditions { get; set; } = new();
    public List<Mutation> Actions { get; set; } = new();

    public Trigger() { }

    public Trigger(Type eventType)
    {
        EventName = eventType.Name;
    }

    public bool MatchesEvent(GameEvent e)
    {
        return string.Equals(EventName, e.GetType().Name, StringComparison.Ordinal);
    }

    public bool AreAllConditionsFulfilled(EvaluationContext context)
    {
        return Conditions.All(condition => condition.Evaluate(context));
    }

    public void ExecuteActions(EvaluationContext context)
    {
        foreach (var action in Actions)
        {
            action.Apply(context);
        }
    }
}
