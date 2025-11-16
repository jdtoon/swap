namespace Swap.Htmx;

/// <summary>
/// Constants for htmx request and response header names to avoid magic strings.
/// </summary>
public static class HxHeaders
{
    // Request headers
    public const string Request = "HX-Request";
    public const string Boosted = "HX-Boosted";
    public const string CurrentUrl = "HX-Current-URL";
    public const string HistoryRestore = "HX-History-Restore-Request";
    public const string Prompt = "HX-Prompt";
    public const string Target = "HX-Target";
    public const string Trigger = "HX-Trigger";
    public const string TriggerName = "HX-Trigger-Name";

    // Response headers
    public const string TriggerResp = "HX-Trigger";
    public const string TriggerAfterSwap = "HX-Trigger-After-Swap";
    public const string TriggerAfterSettle = "HX-Trigger-After-Settle";
    public const string PushUrl = "HX-Push-Url";
    public const string ReplaceUrl = "HX-Replace-Url";
    public const string Redirect = "HX-Redirect";
    public const string Refresh = "HX-Refresh";
    public const string Retarget = "HX-Retarget";
    public const string Reswap = "HX-Reswap";
    public const string Reselect = "HX-Reselect";
    public const string Location = "HX-Location";
}
