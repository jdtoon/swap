namespace NetMX.Htmx;

/// <summary>
/// HTMX swap strategies for controlling how content is inserted into the DOM.
/// Maps to htmx's hx-swap attribute values.
/// </summary>
public enum HtmxSwap
{
    /// <summary>Replace the inner html of the target element (default)</summary>
    InnerHTML,
    
    /// <summary>Replace the entire target element with the response</summary>
    OuterHTML,
    
    /// <summary>Insert the response before the target element</summary>
    BeforeBegin,
    
    /// <summary>Insert the response before the first child of the target element</summary>
    AfterBegin,
    
    /// <summary>Insert the response after the last child of the target element</summary>
    BeforeEnd,
    
    /// <summary>Insert the response after the target element</summary>
    AfterEnd,
    
    /// <summary>Deletes the target element regardless of the response</summary>
    Delete,
    
    /// <summary>Does not append content from response (out of band items will still be processed)</summary>
    None
}

/// <summary>
/// Extension methods for HtmxSwap enum.
/// </summary>
public static class HtmxSwapExtensions
{
    /// <summary>
    /// Converts the HtmxSwap enum to the corresponding htmx swap value string.
    /// </summary>
    public static string ToHtmxValue(this HtmxSwap swap) => swap switch
    {
        HtmxSwap.InnerHTML => "innerHTML",
        HtmxSwap.OuterHTML => "outerHTML",
        HtmxSwap.BeforeBegin => "beforebegin",
        HtmxSwap.AfterBegin => "afterbegin",
        HtmxSwap.BeforeEnd => "beforeend",
        HtmxSwap.AfterEnd => "afterend",
        HtmxSwap.Delete => "delete",
        HtmxSwap.None => "none",
        _ => "innerHTML"
    };
}
