using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;

namespace Cage.Simulation.Mutations;

public sealed class ShuffleEntityListMutation : Mutation
{
    public Expression Entity { get; set; } = null!;
    public string Attribute { get; set; } = null!;

    public override void Apply(EvaluationContext context)
    {
        var entity = Entity.Evaluate(context).AsEntity();
        var list = entity.Attributes[Attribute].AsEntityList();

        // Fisher-Yates shuffle
        var random = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("entity");
        JsonSerializer.Serialize(writer, Entity, options);

        writer.WriteString("attribute", Attribute);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("entity", out var entityProp))
            Entity = JsonSerializer.Deserialize<Expression>(entityProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize entity expression");

        if (root.TryGetProperty("attribute", out var attributeProp))
            Attribute = attributeProp.GetString() ?? throw new JsonException("Missing attribute value");
    }
}