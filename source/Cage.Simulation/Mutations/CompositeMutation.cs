using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;

namespace Cage.Simulation.Mutations;

public sealed class CompositeMutation : Mutation
{
    public List<Mutation> Mutations { get; set; } = new();

    public override void Apply(EvaluationContext context)
    {
        foreach (var mutation in Mutations)
        {
            mutation.Apply(context);
        }
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("mutations");
        JsonSerializer.Serialize(writer, Mutations, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("mutations", out var mutationsProp))
        {
            Mutations = JsonSerializer.Deserialize<List<Mutation>>(mutationsProp.GetRawText(), options)
                ?? new List<Mutation>();
        }
    }
}
