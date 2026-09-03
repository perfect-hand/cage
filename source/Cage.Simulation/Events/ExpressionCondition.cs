using System;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Events;

public sealed class ExpressionCondition
{
    public Expression Left { get; set; } = null!;
    public ComparisonOperator Operator { get; set; }
    public Expression Right { get; set; } = null!;

    public bool Evaluate(EvaluationContext context)
    {
        var leftValue = Left.Evaluate(context);
        var rightValue = Right.Evaluate(context);

        return Compare(leftValue, rightValue);
    }

    private bool Compare(TypedValue left, TypedValue right)
    {
        if (left.Type != right.Type)
        {
            return Operator switch
            {
                ComparisonOperator.Equal => false,
                ComparisonOperator.NotEqual => true,
                _ => false,
            };
        }

        return left.Type switch
        {
            CageType.Int => CompareValues(left.AsInt(), right.AsInt()),
            CageType.String => CompareValues(left.AsString(), right.AsString()),
            _ => Operator switch
            {
                ComparisonOperator.Equal => Equals(left.Value, right.Value),
                ComparisonOperator.NotEqual => !Equals(left.Value, right.Value),
                _ => false,
            }
        };
    }

    private bool CompareValues(int left, int right)
    {
        return Operator switch
        {
            ComparisonOperator.Equal => left == right,
            ComparisonOperator.NotEqual => left != right,
            ComparisonOperator.LessThan => left < right,
            ComparisonOperator.LessThanOrEqual => left <= right,
            ComparisonOperator.GreaterThan => left > right,
            ComparisonOperator.GreaterThanOrEqual => left >= right,
            _ => false,
        };
    }

    private bool CompareValues(string left, string right)
    {
        var comparison = string.Compare(left, right, StringComparison.Ordinal);
        return Operator switch
        {
            ComparisonOperator.Equal => comparison == 0,
            ComparisonOperator.NotEqual => comparison != 0,
            ComparisonOperator.LessThan => comparison < 0,
            ComparisonOperator.LessThanOrEqual => comparison <= 0,
            ComparisonOperator.GreaterThan => comparison > 0,
            ComparisonOperator.GreaterThanOrEqual => comparison >= 0,
            _ => false,
        };
    }
}
