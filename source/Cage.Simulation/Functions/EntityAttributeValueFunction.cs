using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Functions;

public sealed class EntityAttributeValueFunction : Function
{
    public Expression Entity { get; set; } = null!;
    public Expression AttributeName { get; set; } = null!;

    public EntityAttributeValueFunction() { }

    public EntityAttributeValueFunction(Expression entityExpression, Expression attributeNameExpression)
    {
        Entity = entityExpression;
        AttributeName = attributeNameExpression;
    }

    public override TypedValue Call(EvaluationContext context)
    {
        var entity = Entity.Evaluate(context).AsEntity();
        var attributeName = AttributeName.Evaluate(context).AsString();

        if (!entity.Attributes.TryGetValue(attributeName, out var value))
            throw new KeyNotFoundException($"Attribute '{attributeName}' was not found on the entity.");

        return value;
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("entity");
        JsonSerializer.Serialize(writer, Entity, options);

        writer.WritePropertyName("attributeName");
        JsonSerializer.Serialize(writer, AttributeName, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("entity", out var entityProp))
            Entity = JsonSerializer.Deserialize<Expression>(entityProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize entity expression");

        if (root.TryGetProperty("attributeName", out var attributeNameProp))
            AttributeName = JsonSerializer.Deserialize<Expression>(attributeNameProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize attributeName expression");
    }
}
