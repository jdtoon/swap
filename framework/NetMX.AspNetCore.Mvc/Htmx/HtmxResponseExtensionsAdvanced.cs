using Microsoft.AspNetCore.Mvc;

namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Additional HTMX response extensions for advanced features.
/// </summary>
public static class HtmxResponseExtensionsAdvanced
{
    /// <summary>
    /// Sends an out-of-band swap response. Allows you to specify swaps on different target elements than the original target.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="content">The HTML content to swap out-of-band.</param>
    /// <param name="target">The target element selector.</param>
    /// <param name="swap">The swap style to use.</param>
    /// <returns>A ContentResult with the out-of-band swap markup.</returns>
    public static ContentResult HxOutOfBandSwap(
        this Controller controller,
        string content,
        string target,
        string swap = HtmxSwap.InnerHTML)
    {
        var oobContent = $"<div id=\"{target}\" hx-swap-oob=\"{swap}\">{content}</div>";
        return controller.Content(oobContent, "text/html");
    }

    /// <summary>
    /// Sends multiple out-of-band swaps in a single response.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="swaps">A collection of out-of-band swaps to perform.</param>
    /// <returns>A ContentResult with all out-of-band swap markup.</returns>
    public static ContentResult HxOutOfBandSwaps(
        this Controller controller,
        params (string target, string content, string swap)[] swaps)
    {
        var html = string.Join("", swaps.Select(s =>
            $"<div id=\"{s.target}\" hx-swap-oob=\"{s.swap}\">{s.content}</div>"));
        return controller.Content(html, "text/html");
    }

    /// <summary>
    /// Configures a polling interval for the client.
    /// Note: This sets the HX-Trigger header with a "load" event that includes a delay.
    /// The client should have hx-trigger="load" to start polling.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="intervalMs">The polling interval in milliseconds.</param>
    public static void HxPoll(this Controller controller, int intervalMs)
    {
        controller.HxTrigger("load", new { delay = intervalMs });
    }

    /// <summary>
    /// Stops polling by triggering a "stop-polling" event.
    /// The client should listen for this event and remove hx-trigger.
    /// </summary>
    public static void HxStopPoll(this Controller controller)
    {
        controller.HxTrigger("stop-polling");
    }

    /// <summary>
    /// Returns a 286 response to stop polling.
    /// HTMX interprets 286 as "stop polling".
    /// </summary>
    public static StatusCodeResult HxStopPollingResponse(this Controller controller)
    {
        return controller.StatusCode(286);
    }

    /// <summary>
    /// Tells HTMX to preserve the current scroll position.
    /// </summary>
    public static void HxPreserveScroll(this Controller controller)
    {
        controller.HxReswap(HtmxSwapModifier.Build(HtmxSwap.InnerHTML, HtmxSwapModifier.ShowNone()));
    }

    /// <summary>
    /// Tells HTMX to scroll to top after swap.
    /// </summary>
    public static void HxScrollTop(this Controller controller)
    {
        controller.HxReswap(HtmxSwapModifier.Build(HtmxSwap.InnerHTML, HtmxSwapModifier.ScrollTop()));
    }

    /// <summary>
    /// Tells HTMX to scroll to bottom after swap.
    /// </summary>
    public static void HxScrollBottom(this Controller controller)
    {
        controller.HxReswap(HtmxSwapModifier.Build(HtmxSwap.InnerHTML, HtmxSwapModifier.ScrollBottom()));
    }

    /// <summary>
    /// Tells HTMX to scroll to a specific element after swap.
    /// </summary>
    public static void HxScrollTo(this Controller controller, string selector)
    {
        controller.HxReswap(HtmxSwapModifier.Build(HtmxSwap.InnerHTML, HtmxSwapModifier.Scroll(selector)));
    }

    /// <summary>
    /// Builds a swap with custom modifiers.
    /// </summary>
    public static void HxReswapWithModifiers(this Controller controller, string swap, params string[] modifiers)
    {
        controller.HxReswap(HtmxSwapModifier.Build(swap, modifiers));
    }
}
