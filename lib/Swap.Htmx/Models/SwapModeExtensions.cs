namespace Swap.Htmx.Models;

/// <summary>
/// Maps <see cref="SwapMode"/> values to the token used in the <c>hx-swap-oob</c> attribute.
/// Shared by every result type so the mapping stays consistent.
/// </summary>
internal static class SwapModeExtensions
{
    public static string ToOobSwapToken(this SwapMode mode) => mode switch
    {
        SwapMode.OuterHTML => "true",
        SwapMode.InnerHTML => "innerHTML",
        SwapMode.MorphOuter => "morph:outerHTML",
        SwapMode.MorphInner => "morph:innerHTML",
        SwapMode.BeforeBegin => "beforebegin",
        SwapMode.AfterBegin => "afterbegin",
        SwapMode.BeforeEnd => "beforeend",
        SwapMode.AfterEnd => "afterend",
        SwapMode.Delete => "delete",
        SwapMode.None => "none",
        _ => "true"
    };
}
