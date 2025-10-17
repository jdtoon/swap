using System.Reflection;
using NetMX.DependencyInjection;

namespace NetMX.Ddd.Application.ObjectMapping;

/// <summary>
/// Simple object mapper using reflection to copy properties between objects.
/// For production use, configure custom mappings for complex scenarios.
/// </summary>
public class ObjectMapper : IObjectMapper, ISingletonDependency
{
    /// <summary>
    /// Maps source object to a new destination instance by copying matching properties.
    /// </summary>
    public TDestination Map<TDestination>(object source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var destination = Activator.CreateInstance<TDestination>();
        MapInternal(source, destination!);
        return destination;
    }

    /// <summary>
    /// Maps source object to existing destination instance by copying matching properties.
    /// </summary>
    public void Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        MapInternal(source, destination);
    }

    /// <summary>
    /// Maps source object to a new destination instance with type safety.
    /// </summary>
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return Map<TDestination>(source!);
    }

    private void MapInternal(object source, object destination)
    {
        var sourceType = source.GetType();
        var destinationType = destination.GetType();

        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var destProp in destinationProperties)
        {
            if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
            {
                // Only map if types are compatible
                if (IsAssignable(sourceProp.PropertyType, destProp.PropertyType))
                {
                    var value = sourceProp.GetValue(source);
                    destProp.SetValue(destination, value);
                }
            }
        }
    }

    private bool IsAssignable(Type sourceType, Type destinationType)
    {
        // Direct assignment
        if (destinationType.IsAssignableFrom(sourceType))
            return true;

        // Nullable handling
        var underlyingDestType = Nullable.GetUnderlyingType(destinationType);
        if (underlyingDestType != null && underlyingDestType == sourceType)
            return true;

        var underlyingSourceType = Nullable.GetUnderlyingType(sourceType);
        if (underlyingSourceType != null && underlyingSourceType == destinationType)
            return true;

        return false;
    }
}
