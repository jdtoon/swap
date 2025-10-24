using NetMX.DependencyInjection;

namespace NetMX.Data.Filtering;

/// <summary>
/// Interface for managing data filters (e.g., soft delete, multi-tenancy) that can be enabled/disabled at runtime.
/// </summary>
public interface IDataFilter : IScopedDependency
{
    /// <summary>
    /// Enables the filter. Returns an IDisposable that can be used to restore the previous state.
    /// </summary>
    /// <typeparam name="TFilter">Type of the filter.</typeparam>
    IDisposable Enable<TFilter>() where TFilter : class;

    /// <summary>
    /// Disables the filter. Returns an IDisposable that can be used to restore the previous state.
    /// </summary>
    /// <typeparam name="TFilter">Type of the filter.</typeparam>
    IDisposable Disable<TFilter>() where TFilter : class;

    /// <summary>
    /// Checks if the filter is enabled.
    /// </summary>
    /// <typeparam name="TFilter">Type of the filter.</typeparam>
    bool IsEnabled<TFilter>() where TFilter : class;
}