using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Renders a file upload <c>&lt;form&gt;</c> wired for htmx multipart submission with a live progress bar,
/// without hand-wiring <c>hx-post</c>/<c>hx-encoding</c>/<c>hx-on::xhr:progress</c> yourself.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;swap-upload name="file" url="/upload" target="#result" swap="innerHTML"&gt;
///     &lt;button type="submit"&gt;Upload&lt;/button&gt;
/// &lt;/swap-upload&gt;
/// </code>
/// Renders:
/// <code>
/// &lt;form hx-post="/upload" hx-encoding="multipart/form-data" hx-target="#result" hx-swap="innerHTML"
///       hx-on::xhr:progress="..."&gt;
///     &lt;input type="file" name="file" /&gt;
///     &lt;progress value="0" max="100" style="display:none"&gt;&lt;/progress&gt;
///     &lt;button type="submit"&gt;Upload&lt;/button&gt;
/// &lt;/form&gt;
/// </code>
/// Requires both <c>name</c> and <c>url</c>; without either, no output is rendered.
/// </remarks>
[HtmlTargetElement("swap-upload")]
public class SwapUploadTagHelper : TagHelper
{
    private const string ProgressHandler =
        "var p=this.querySelector('progress'); if(p){p.style.display='';p.max=event.detail.total;p.value=event.detail.loaded;}";

    /// <summary>The name attribute of the file input. Required; without it, no output is rendered.</summary>
    [HtmlAttributeName("name")]
    public string? Name { get; set; }

    /// <summary>The URL the form posts the upload to (sets <c>hx-post</c>). Required; without it, no output is rendered.</summary>
    [HtmlAttributeName("url")]
    public string? Url { get; set; }

    /// <summary>The hx-target for the response. Defaults to <c>this</c>.</summary>
    [HtmlAttributeName("target")]
    public string Target { get; set; } = "this";

    /// <summary>The hx-swap style for the response. Defaults to <c>innerHTML</c>.</summary>
    [HtmlAttributeName("swap")]
    public string Swap { get; set; } = "innerHTML";

    /// <summary>Allow selecting multiple files. Default false.</summary>
    [HtmlAttributeName("multiple")]
    public bool Multiple { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Url))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "form";
        output.TagMode = TagMode.StartTagAndEndTag;

        output.Attributes.SetAttribute("hx-post", Url);
        output.Attributes.SetAttribute("hx-encoding", "multipart/form-data");
        output.Attributes.SetAttribute("hx-target", Target);
        output.Attributes.SetAttribute("hx-swap", Swap);
        output.Attributes.SetAttribute("hx-on::xhr:progress", ProgressHandler);

        output.Attributes.RemoveAll("name");
        output.Attributes.RemoveAll("url");
        output.Attributes.RemoveAll("target");
        output.Attributes.RemoveAll("swap");
        output.Attributes.RemoveAll("multiple");

        var encodedName = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(Name);
        var fileInput = Multiple
            ? $"<input type=\"file\" name=\"{encodedName}\" multiple />"
            : $"<input type=\"file\" name=\"{encodedName}\" />";

        var prefix = fileInput + "<progress value=\"0\" max=\"100\" style=\"display:none\"></progress>";
        output.PreContent.SetHtmlContent(prefix);
        // Inner content (e.g. a submit button) is preserved as-is.
    }
}
