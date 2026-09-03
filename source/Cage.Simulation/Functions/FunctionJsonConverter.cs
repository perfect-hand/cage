using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation;

namespace Cage.Simulation.Functions;

public class FunctionJsonConverter : JsonConverter<Function>
{
    public override Function Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("$type", out var typeElement))
            throw new JsonException("Missing $type property for polymorphic Function deserialization.");

        var typeName = typeElement.GetString();
        if (string.IsNullOrEmpty(typeName))
            throw new JsonException("$type property is empty.");

        // Use reflection to create a default instance in the functions namespace
        var type = typeof(Function).Assembly.GetType($"{typeof(Function).Namespace}.{typeName}")
            ?? throw new JsonException($"Unknown function type: {typeName}");

        var function = Activator.CreateInstance(type) as Function
            ?? throw new JsonException($"Failed to instantiate function type: {typeName}");

        // Call the abstract ReadFromJson method to populate the instance
        function.ReadFromJson(root, options);
        return function;
    }

    public override void Write(Utf8JsonWriter writer, Function value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("$type", value.GetType().Name);
        value.WriteToJson(writer, options);
        writer.WriteEndObject();
    }
}
