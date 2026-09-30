namespace Cage.Backend.Game.Common;

/// <summary>
/// Builds a uniform, human-readable <c>ToString()</c> representation for entities and DTOs,
/// e.g. "MatchDto{Id=123}". Modeled after Guava's <c>MoreObjects.toStringHelper</c> to avoid
/// hand-rolled, inconsistent ToString overrides.
/// </summary>
public sealed class ToStringBuilder
{
    private readonly string typeName;
    private readonly List<string> fields = [];

    private ToStringBuilder(string typeName)
    {
        this.typeName = typeName;
    }

    /// <summary>
    /// Starts building a ToString() representation for the given instance's runtime type.
    /// </summary>
    public static ToStringBuilder For(object instance)
    {
        return new ToStringBuilder(instance.GetType().Name);
    }

    /// <summary>
    /// Adds a named field to the representation. Values are rendered via their own ToString(),
    /// or as "null" when the value is null.
    /// </summary>
    public ToStringBuilder Add(string name, object? value)
    {
        fields.Add($"{name}={value ?? "null"}");
        return this;
    }

    public override string ToString()
    {
        return $"{typeName}{{{string.Join(", ", fields)}}}";
    }
}
