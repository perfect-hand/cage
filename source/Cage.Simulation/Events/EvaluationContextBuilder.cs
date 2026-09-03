using System;
using System.Reflection;
using Cage.Simulation.Entities;
using Cage.Simulation.Expressions;
using Cage.Simulation.Types;

namespace Cage.Simulation.Events;

public class EvaluationContextBuilder
{
    public EvaluationContext BuildContextForEvent(GameEvent e, EntityManager entityManager)
    {
        var context = new EvaluationContext(entityManager);
        PopulateContextWithEventProperties(context, e, entityManager);
        return context;
    }

    private void PopulateContextWithEventProperties(EvaluationContext context, GameEvent e, EntityManager entityManager)
    {
        var eventType = e.GetType();
        var properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead)
                continue;

            var attribute = property.GetCustomAttribute<EventPropertyAttribute>();
            if (attribute is null)
                continue;

            var value = property.GetValue(e);
            if (value is null)
                continue;

            var typedValue = ConvertToTypedValue(value, attribute.Type, entityManager);

            if (typedValue is not null)
            {
                var propertyName = attribute.PropertyName ?? property.Name;
                var variableName = $"event{propertyName}";
                context.Variables[variableName] = typedValue;
            }
        }
    }

    private TypedValue? ConvertToTypedValue(object value, CageType explicitType, EntityManager entityManager)
    {
        return explicitType switch
        {
            CageType.Int when value is int intVal => new TypedValue(intVal),
            CageType.String when value is string strVal => new TypedValue(strVal),
            CageType.Entity when value is int entityIdVal => ResolveEntity(entityManager, entityIdVal),
            CageType.Entity when value is Entity entityVal => new TypedValue(entityVal),
            CageType.EntityList when value is List<Entity> listVal => new TypedValue(listVal),
            _ => null
        };
    }

    private TypedValue? ResolveEntity(EntityManager entityManager, int entityId)
    {
        var entity = entityManager.FindEntity(entityId);
        if (entity is not null)
        {
            return new TypedValue(entity);
        }
        return null;
    }
}