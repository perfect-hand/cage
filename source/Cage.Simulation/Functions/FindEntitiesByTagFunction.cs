using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Entities;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Functions;

public sealed class FindEntitiesByTagFunction : Function
{
    public Expression Tag { get; set; } = null!;

    public FindEntitiesByTagFunction() { }

    public FindEntitiesByTagFunction(Expression tag)
    {
        Tag = tag;
    }

    public override TypedValue Call(EvaluationContext context)
    {
        var tag = Tag.Evaluate(context).AsString();
        var matches = context.EntityManager.FindEntitiesByTag(tag);
        return new TypedValue(matches);
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("tag");
        JsonSerializer.Serialize(writer, Tag, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("tag", out var tagProp))
            Tag = JsonSerializer.Deserialize<Expression>(tagProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize tag expression");
    }
}
