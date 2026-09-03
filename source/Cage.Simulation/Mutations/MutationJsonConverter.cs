using System.Text.Json;
using System.Text.Json.Serialization;
using Cage.Simulation;

namespace Cage.Simulation.Mutations;

public class MutationJsonConverter : JsonConverter<Mutation>
{
    public override Mutation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("$type", out var typeElement))
            throw new JsonException("Missing $type property for polymorphic Mutation deserialization.");

        var typeName = typeElement.GetString();
        if (string.IsNullOrEmpty(typeName))
            throw new JsonException("$type property is empty.");

        // Use reflection to create a default instance in the mutations namespace
        var type = typeof(Mutation).Assembly.GetType($"{typeof(Mutation).Namespace}.{typeName}")
            ?? throw new JsonException($"Unknown mutation type: {typeName}");

        var mutation = Activator.CreateInstance(type) as Mutation
            ?? throw new JsonException($"Failed to instantiate mutation type: {typeName}");

        // Call the abstract ReadFromJson method to populate the instance
        mutation.ReadFromJson(root, options);
        return mutation;
    }

    public override void Write(Utf8JsonWriter writer, Mutation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("$type", value.GetType().Name);
        value.WriteToJson(writer, options);
        writer.WriteEndObject();
    }
}
