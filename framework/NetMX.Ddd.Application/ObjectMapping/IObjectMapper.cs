namespace NetMX.Ddd.Application.ObjectMapping;

/// <summary>
/// Interface for mapping objects between types.
/// </summary>
public interface IObjectMapper
{
    /// <summary>
    /// Maps an object to a new instance of the destination type.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <returns>A new instance of the destination type.</returns>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Maps an object to an existing destination instance.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="destination">The destination object to map to.</param>
    void Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>
    /// Maps a source object to a destination type.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <returns>A new instance of the destination type.</returns>
    TDestination Map<TSource, TDestination>(TSource source);
}
