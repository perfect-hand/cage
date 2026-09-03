using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Functions;

[JsonConverter(typeof(FunctionJsonConverter))]
public abstract class Function
{
    public abstract TypedValue Call(EvaluationContext context);

    internal abstract void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options);

    internal abstract void ReadFromJson(JsonElement root, JsonSerializerOptions options);
}
