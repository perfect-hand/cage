using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;

namespace Cage.Simulation.Mutations;

[JsonConverter(typeof(MutationJsonConverter))]
public abstract class Mutation
{
    public abstract void Apply(EvaluationContext context);

    internal abstract void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options);

    internal abstract void ReadFromJson(JsonElement root, JsonSerializerOptions options);
}
