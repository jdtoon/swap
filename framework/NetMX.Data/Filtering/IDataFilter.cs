using NetMX.DependencyInjection;

namespace NetMX.Data.Filtering;

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