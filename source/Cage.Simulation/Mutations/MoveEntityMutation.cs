using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;

namespace Cage.Simulation.Mutations;

public sealed class MoveEntityMutation : Mutation
{
    public Expression EntityToMove { get; set; } = null!;
    public Expression SourceEntity { get; set; } = null!;
    public string SourceAttribute { get; set; } = null!;
    public Expression TargetEntity { get; set; } = null!;
    public string TargetAttribute { get; set; } = null!;

    public override void Apply(EvaluationContext context)
    {
        var entityToMove = EntityToMove.Evaluate(context).AsEntity();
        var sourceEntity = SourceEntity.Evaluate(context).AsEntity();
        var targetEntity = TargetEntity.Evaluate(context).AsEntity();

        var sourceList = sourceEntity.Attributes[SourceAttribute].AsEntityList();
        var targetList = targetEntity.Attributes[TargetAttribute].AsEntityList();

        // Remove from source list
        sourceList.Remove(entityToMove);

        // Add to target list
        targetList.Add(entityToMove);
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("entityToMove");
        JsonSerializer.Serialize(writer, EntityToMove, options);

        writer.WritePropertyName("sourceEntity");
        JsonSerializer.Serialize(writer, SourceEntity, options);

        writer.WriteString("sourceAttribute", SourceAttribute);

        writer.WritePropertyName("targetEntity");
        JsonSerializer.Serialize(writer, TargetEntity, options);

        writer.WriteString("targetAttribute", TargetAttribute);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("entityToMove", out var entityToMoveProp))
            EntityToMove = JsonSerializer.Deserialize<Expression>(entityToMoveProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize entityToMove expression");

        if (root.TryGetProperty("sourceEntity", out var sourceEntityProp))
            SourceEntity = JsonSerializer.Deserialize<Expression>(sourceEntityProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize sourceEntity expression");

        if (root.TryGetProperty("sourceAttribute", out var sourceAttributeProp))
            SourceAttribute = sourceAttributeProp.GetString() ?? throw new JsonException("Missing sourceAttribute value");

        if (root.TryGetProperty("targetEntity", out var targetEntityProp))
            TargetEntity = JsonSerializer.Deserialize<Expression>(targetEntityProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize targetEntity expression");

        if (root.TryGetProperty("targetAttribute", out var targetAttributeProp))
            TargetAttribute = targetAttributeProp.GetString() ?? throw new JsonException("Missing targetAttribute value");
    }
}