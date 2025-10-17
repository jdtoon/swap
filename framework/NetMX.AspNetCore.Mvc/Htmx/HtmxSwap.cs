namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// HTMX swap styles that control how content is swapped into the DOM.
/// </summary>
public static class HtmxSwap
{
    /// <summary>
    /// Replace the inner html of the target element (default).
    /// </summary>
    public const string InnerHTML = "innerHTML";

    /// <summary>
    /// Replace the entire target element with the response.
    /// </summary>
    public const string OuterHTML = "outerHTML";

    /// <summary>
    /// Insert the response before the target element.
    /// </summary>
    public const string BeforeBegin = "beforebegin";

    /// <summary>
    /// Insert the response before the first child of the target element.
    /// </summary>
    public const string AfterBegin = "afterbegin";

    /// <summary>
    /// Insert the response after the last child of the target element.
    /// </summary>
    public const string BeforeEnd = "beforeend";

    /// <summary>
    /// Insert the response after the target element.
    /// </summary>
    public const string AfterEnd = "afterend";

    /// <summary>
    /// Deletes the target element regardless of the response.
    /// </summary>
    public const string Delete = "delete";

    /// <summary>
    /// Does not append content from response (out of band items will still be processed).
    /// </summary>
    public const string None = "none";
}
