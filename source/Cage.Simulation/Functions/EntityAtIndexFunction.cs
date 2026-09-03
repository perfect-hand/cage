using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Entities;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Functions;

public sealed class EntityAtIndexFunction : Function
{
    public Expression EntityList { get; set; } = null!;
    public Expression ListIndex { get; set; } = null!;

    public EntityAtIndexFunction() { }

    public EntityAtIndexFunction(Expression listExpression, Expression indexExpression)
    {
        EntityList = listExpression;
        ListIndex = indexExpression;
    }

    public override TypedValue Call(EvaluationContext context)
    {
        var list = EntityList.Evaluate(context).AsEntityList();
        var index = ListIndex.Evaluate(context).AsInt();

        if (index < 0 || index >= list.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range for the entity list.");

        return new TypedValue(list[index]);
    }

    internal override void WriteToJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WritePropertyName("list");
        JsonSerializer.Serialize(writer, EntityList, options);

        writer.WritePropertyName("index");
        JsonSerializer.Serialize(writer, ListIndex, options);
    }

    internal override void ReadFromJson(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("list", out var listProp))
            EntityList = JsonSerializer.Deserialize<Expression>(listProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize list expression");

        if (root.TryGetProperty("index", out var indexProp))
            ListIndex = JsonSerializer.Deserialize<Expression>(indexProp.GetRawText(), options) ?? throw new JsonException("Failed to deserialize index expression");
    }
}
