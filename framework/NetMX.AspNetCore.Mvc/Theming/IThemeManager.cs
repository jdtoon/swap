using NetMX.DependencyInjection;

namespace NetMX.AspNetCore.Mvc.Theming;

/// <summary>
/// Manages the current theme for the application.
/// </summary>
public interface IThemeManager : IScopedDependency
{
    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    ITheme CurrentTheme { get; }
}