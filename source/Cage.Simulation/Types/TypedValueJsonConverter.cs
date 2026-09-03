using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation.Entities;

namespace Cage.Simulation.Types;

public class TypedValueJsonConverter : JsonConverter<TypedValue>
{
    public override TypedValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var typeString = root.GetProperty("type").GetString();
        var type = Enum.Parse<CageType>(typeString!);

        var valueElement = root.GetProperty("value");

        object? value = type switch
        {
            CageType.Int => valueElement.GetInt32(),
            CageType.String => valueElement.GetString() ?? string.Empty,
            CageType.Entity => JsonSerializer.Deserialize<Entity>(valueElement.GetRawText(), options),
            CageType.EntityList => JsonSerializer.Deserialize<List<Entity>>(valueElement.GetRawText(), options),
            _ => throw new JsonException($"Unknown CageType: {type}")
        };

        return type switch
        {
            CageType.Int => new TypedValue((int)value!),
            CageType.String => new TypedValue((string)value!),
            CageType.Entity => new TypedValue((Entity)value!),
            CageType.EntityList => new TypedValue((List<Entity>)value!),
            _ => throw new JsonException($"Unknown CageType: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, TypedValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type.ToString());

        writer.WritePropertyName("value");
        switch (value.Type)
        {
            case CageType.Int:
                writer.WriteNumberValue((int)value.Value!);
                break;
            case CageType.String:
                writer.WriteStringValue((string)value.Value!);
                break;
            case CageType.Entity:
                JsonSerializer.Serialize(writer, value.Value, options);
                break;
            case CageType.EntityList:
                JsonSerializer.Serialize(writer, value.Value, options);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
        writer.WriteEndObject();
    }
}