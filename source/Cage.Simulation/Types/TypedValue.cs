using System.Text.Json.Serialization;
using Cage.Simulation.Entities;

namespace Cage.Simulation.Types;

[JsonConverter(typeof(TypedValueJsonConverter))]
public class TypedValue
{
    public CageType Type { get; private set; }
    public object? Value { get; private set; }

    public TypedValue(int value)
    {
        Type = CageType.Int;
        Value = value;
    }

    public TypedValue(string value)
    {
        Type = CageType.String;
        Value = value;
    }

    public TypedValue(Entity value)
    {
        Type = CageType.Entity;
        Value = value;
    }

    public TypedValue(List<Entity> value)
    {
        Type = CageType.EntityList;
        Value = value;
    }

    public int AsInt() => Type == CageType.Int && Value != null ? (int)Value : throw new InvalidCastException();
    public string AsString() => Type == CageType.String && Value != null ? (string)Value : throw new InvalidCastException();
    public Entity AsEntity() => Type == CageType.Entity && Value != null ? (Entity)Value : throw new InvalidCastException();
    public List<Entity> AsEntityList() => Type == CageType.EntityList && Value != null ? (List<Entity>)Value : throw new InvalidCastException();
}