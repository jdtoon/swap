using System.Text;

namespace Swap.Htmx.Models;

/// <summary>
/// Typed builder for HX-Reswap header values (swap style + modifiers).
/// </summary>
public sealed class HxReswapOptions
{
    public HxSwapStyle? Style { get; set; }
    public bool? Transition { get; set; }
    public TimeSpan? SwapDelay { get; set; }
    public TimeSpan? SettleDelay { get; set; }
    public bool? IgnoreTitle { get; set; }
    public HxScrollPosition? Scroll { get; set; }
    public HxScrollPosition? Show { get; set; }

    public string ToHeaderValue()
    {
        var parts = new List<string>(8);
        if (Style.HasValue) parts.Add(StyleToToken(Style.Value));
        if (Transition.HasValue) parts.Add($"transition:{Transition.Value.ToString().ToLower()}");
        if (SwapDelay.HasValue) parts.Add($"swap:{FormatMs(SwapDelay.Value)}");
        if (SettleDelay.HasValue) parts.Add($"settle:{FormatMs(SettleDelay.Value)}");
        if (IgnoreTitle.HasValue) parts.Add($"ignoreTitle:{IgnoreTitle.Value.ToString().ToLower()}");
        if (Scroll.HasValue) parts.Add($"scroll:{(Scroll.Value == HxScrollPosition.Top ? "top" : "bottom")}");
        if (Show.HasValue) parts.Add($"show:{(Show.Value == HxScrollPosition.Top ? "top" : "bottom")}");
        return string.Join(' ', parts);
    }

    private static string FormatMs(TimeSpan ts)
    {
        var ms = (int)Math.Round(ts.TotalMilliseconds);
        return ms + "ms";
    }

    private static string StyleToToken(HxSwapStyle s) => s switch
    {
        HxSwapStyle.InnerHTML => "innerHTML",
        HxSwapStyle.OuterHTML => "outerHTML",
        HxSwapStyle.BeforeBegin => "beforebegin",
        HxSwapStyle.AfterBegin => "afterbegin",
        HxSwapStyle.BeforeEnd => "beforeend",
        HxSwapStyle.AfterEnd => "afterend",
        HxSwapStyle.Delete => "delete",
        HxSwapStyle.None => "none",
        _ => "innerHTML"
    };
}

public enum HxSwapStyle
{
    InnerHTML,
    OuterHTML,
    BeforeBegin,
    AfterBegin,
    BeforeEnd,
    AfterEnd,
    Delete,
    None
}

public enum HxScrollPosition
{
    Top,
    Bottom
}
