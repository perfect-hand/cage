using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Functions;

public sealed class AddFunction : Function
{
    public Expression Operand1 { get; set; } = null!;
    public Expression Operand2 { get; set; } = null!;

    public AddFunction() { }

    public AddFunction(Expression operand1, Expression operand2)
    {
        Operand1 = operand1;
        Operand2 = operand2;
    }

    public override TypedValue Call(EvaluationContext context)
    {
        var val1 = Operand1.Evaluate(context).AsInt();
        var val2 = Operand2.Evaluate(context).AsInt();
        return new TypedValue(val1 + val2);
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("operand1");
        JsonSerializer.Serialize(writer, Operand1, options);

        writer.WritePropertyName("operand2");
        JsonSerializer.Serialize(writer, Operand2, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("operand1", out var operand1Prop))
            Operand1 = JsonSerializer.Deserialize<Expression>(operand1Prop.GetRawText(), options) ?? throw new JsonException("Failed to deserialize operand1 expression");

        if (root.TryGetProperty("operand2", out var operand2Prop))
            Operand2 = JsonSerializer.Deserialize<Expression>(operand2Prop.GetRawText(), options) ?? throw new JsonException("Failed to deserialize operand2 expression");
    }
}
