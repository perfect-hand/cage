using Cage.Simulation.Types;

namespace Cage.Simulation.Expressions;

public sealed class VariableExpression : Expression
{
    public string Name { get; set; } = null!;

    public VariableExpression() { }

    public VariableExpression(string name)
    {
        Name = name;
    }

    internal override TypedValue Evaluate(EvaluationContext context)
    {
        if (!context.Variables.TryGetValue(Name, out var value))
            throw new InvalidOperationException($"Variable '{Name}' is not defined.");

        return value;
    }
}
