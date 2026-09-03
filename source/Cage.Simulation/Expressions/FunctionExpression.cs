using Cage.Simulation.Functions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Expressions;

public sealed class FunctionExpression : Expression
{
    public Function Function { get; set; } = null!;

    public FunctionExpression() { }

    public FunctionExpression(Function function)
    {
        Function = function;
    }

    internal override TypedValue Evaluate(EvaluationContext context)
    {
        return Function.Call(context);
    }
}
