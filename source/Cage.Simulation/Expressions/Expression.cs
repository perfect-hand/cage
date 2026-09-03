using System.Text.Json.Serialization;
using Cage.Simulation.Types;

namespace Cage.Simulation.Expressions;

[JsonConverter(typeof(ExpressionJsonConverter))]
public abstract class Expression
{
    internal abstract TypedValue Evaluate(EvaluationContext context);
}
