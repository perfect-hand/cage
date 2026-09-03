using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Mutations;

public sealed class SetIntegerAttributeMutation : Mutation
{
    public Expression Entity { get; set; } = null!;
    public string Attribute { get; set; } = null!;
    public Expression NewValue { get; set; } = null!;

    public override void Apply(EvaluationContext context)
    {
        var entity = Entity.Evaluate(context).AsEntity();
        var newValue = NewValue.Evaluate(context).AsInt();

        entity.Attributes[Attribute] = new TypedValue(newValue);
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("entity");
        JsonSerializer.Serialize(writer, Entity, options);

        writer.WriteString("attribute", Attribute);

        writer.WritePropertyName("newValue");
        JsonSerializer.Serialize(writer, NewValue, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("entity", out var entityProp))
            Entity = JsonSerializer.Deserialize<Expression>(entityProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize entity expression");

        if (root.TryGetProperty("attribute", out var attributeProp))
            Attribute = attributeProp.GetString() ?? throw new JsonException("Missing attribute value");

        if (root.TryGetProperty("newValue", out var newValueProp))
            NewValue = JsonSerializer.Deserialize<Expression>(newValueProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize newValue expression");
    }
}
