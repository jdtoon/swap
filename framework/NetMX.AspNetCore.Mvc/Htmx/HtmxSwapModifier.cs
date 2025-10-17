namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Provides modifiers for HTMX swap styles.
/// </summary>
public static class HtmxSwapModifier
{
    /// <summary>
    /// Builds a swap string with modifiers.
    /// </summary>
    /// <param name="swap">The base swap style.</param>
    /// <param name="modifiers">Optional modifiers to apply.</param>
    /// <returns>A formatted swap string with modifiers.</returns>
    public static string Build(string swap, params string[] modifiers)
    {
        if (modifiers.Length == 0)
            return swap;

        return $"{swap} {string.Join(" ", modifiers)}";
    }

    /// <summary>
    /// Scroll to the top of the swap.
    /// </summary>
    public static string ScrollTop() => "scroll:top";

    /// <summary>
    /// Scroll to the bottom of the swap.
    /// </summary>
    public static string ScrollBottom() => "scroll:bottom";

    /// <summary>
    /// Scroll to a specific element.
    /// </summary>
    public static string Scroll(string selector) => $"scroll:{selector}:top";

    /// <summary>
    /// Scroll to the bottom of a specific element.
    /// </summary>
    public static string ScrollBottom(string selector) => $"scroll:{selector}:bottom";

    /// <summary>
    /// Show the top of the swap in viewport.
    /// </summary>
    public static string ShowTop() => "show:top";

    /// <summary>
    /// Show the bottom of the swap in viewport.
    /// </summary>
    public static string ShowBottom() => "show:bottom";

    /// <summary>
    /// Show a specific element in viewport.
    /// </summary>
    public static string Show(string selector) => $"show:{selector}:top";

    /// <summary>
    /// Show the bottom of a specific element in viewport.
    /// </summary>
    public static string ShowBottom(string selector) => $"show:{selector}:bottom";

    /// <summary>
    /// Don't show anything in the viewport.
    /// </summary>
    public static string ShowNone() => "show:none";

    /// <summary>
    /// Focus scroll to the top.
    /// </summary>
    public static string FocusScrollTrue() => "focus-scroll:true";

    /// <summary>
    /// Don't focus scroll.
    /// </summary>
    public static string FocusScrollFalse() => "focus-scroll:false";

    /// <summary>
    /// Swap after the specified time.
    /// </summary>
    public static string Swap(string time) => $"swap:{time}";

    /// <summary>
    /// Settle after the specified time.
    /// </summary>
    public static string Settle(string time) => $"settle:{time}";

    /// <summary>
    /// Transition the swap.
    /// </summary>
    public static string TransitionTrue() => "transition:true";

    /// <summary>
    /// Don't transition the swap.
    /// </summary>
    public static string TransitionFalse() => "transition:false";

    /// <summary>
    /// Ignore the document title in the response.
    /// </summary>
    public static string IgnoreTitle() => "ignoreTitle:true";
}
