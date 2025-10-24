namespace NetMX.AspNetCore.Mvc.Theming;

/// <summary>
/// Represents a theme with its associated styles and scripts.
/// </summary>
public interface ITheme
{
    /// <summary>
    /// Gets the CSS style URLs for this theme.
    /// </summary>
    /// <returns>An array of CSS URLs.</returns>
    string[] GetStyles();
    
    /// <summary>
    /// Gets the JavaScript URLs for this theme.
    /// </summary>
    /// <returns>An array of JavaScript URLs.</returns>
    string[] GetScripts();
}