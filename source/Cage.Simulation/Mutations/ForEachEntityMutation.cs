using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Mutations;

public sealed class ForEachEntityMutation : Mutation
{
    public string LoopVariable { get; set; } = null!;
    public Expression EntityList { get; set; } = null!;
    public List<Mutation> Mutations { get; set; } = new();

    public override void Apply(EvaluationContext context)
    {
        var list = EntityList.Evaluate(context).AsEntityList();

        // Save previous variable if exists
        var hadPrevious = context.Variables.TryGetValue(LoopVariable, out var previousValue);

        foreach (var entity in list)
        {
            context.Variables[LoopVariable] = new TypedValue(entity);
            foreach (var mutation in Mutations)
            {
                mutation.Apply(context);
            }
        }

        // Restore previous variable state
        if (hadPrevious)
            context.Variables[LoopVariable] = previousValue!;
        else
            context.Variables.Remove(LoopVariable);
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteString("loopVariable", LoopVariable);

        writer.WritePropertyName("list");
        JsonSerializer.Serialize(writer, EntityList, options);

        writer.WritePropertyName("mutations");
        JsonSerializer.Serialize(writer, Mutations, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("loopVariable", out var loopVarProp))
            LoopVariable = loopVarProp.GetString() ?? throw new JsonException("Missing loopVariable value");

        if (root.TryGetProperty("list", out var listProp))
            EntityList = JsonSerializer.Deserialize<Expression>(listProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize list expression");

        if (root.TryGetProperty("mutations", out var mutationsProp))
            Mutations = JsonSerializer.Deserialize<List<Mutation>>(mutationsProp.GetRawText(), options) ?? new List<Mutation>();
    }
}
