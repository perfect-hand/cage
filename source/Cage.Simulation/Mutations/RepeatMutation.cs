using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;

namespace Cage.Simulation.Mutations;

public sealed class RepeatMutation : Mutation
{
    public Expression Times { get; set; } = null!;
    public List<Mutation> Mutations { get; set; } = new();

    public override void Apply(EvaluationContext context)
    {
        var times = Times.Evaluate(context).AsInt();
        for (int i = 0; i < times; i++)
        {
            foreach (var mutation in Mutations)
            {
                mutation.Apply(context);
            }
        }
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("times");
        JsonSerializer.Serialize(writer, Times, options);

        writer.WritePropertyName("mutations");
        JsonSerializer.Serialize(writer, Mutations, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("times", out var timesProp))
            Times = JsonSerializer.Deserialize<Expression>(timesProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize times expression");

        if (root.TryGetProperty("mutations", out var mutationsProp))
            Mutations = JsonSerializer.Deserialize<List<Mutation>>(mutationsProp.GetRawText(), options) ?? new List<Mutation>();
    }
}
