using System;
using System.Globalization;
using System.Text;

namespace Swap.Htmx.Models;

/// <summary>
/// Builds (and patches) the attributes applied to an out-of-band swap element: the <c>hx-swap-oob</c>
/// token plus the optional <c>data-swap-seq</c> monotonic version the client guard reads to drop stale
/// or duplicate updates.
/// </summary>
internal static class SwapOobAttributes
{
    public static string Build(SwapMode mode, long? seq = null)
    {
        var sb = new StringBuilder();
        sb.Append("hx-swap-oob=\"").Append(mode.ToOobSwapToken()).Append('"');

        if (seq.HasValue)
        {
            sb.Append(" data-swap-seq=\"").Append(seq.Value.ToString(CultureInfo.InvariantCulture)).Append('"');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Injects <c>data-swap-seq</c> into a fragment that already declares its own <c>hx-swap-oob</c> (so
    /// the client out-of-order guard still applies to self-declared OOB partials), unless a stamp is
    /// already present. No-op when <paramref name="seq"/> is null or no <c>hx-swap-oob</c> is found.
    /// </summary>
    public static string InjectSeqIfMissing(string html, long? seq)
    {
        if (!seq.HasValue || string.IsNullOrEmpty(html) || html.Contains("data-swap-seq"))
        {
            return html;
        }

        const string marker = "hx-swap-oob";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
        {
            return html;
        }

        var stamp = $" data-swap-seq=\"{seq.Value.ToString(CultureInfo.InvariantCulture)}\"";
        var after = idx + marker.Length;

        // hx-swap-oob="value" -> insert after the closing quote of the attribute value.
        if (after < html.Length && html[after] == '=')
        {
            var q1 = html.IndexOf('"', after);
            var q2 = q1 >= 0 ? html.IndexOf('"', q1 + 1) : -1;
            return q2 > 0 ? html.Insert(q2 + 1, stamp) : html;
        }

        // Bare hx-swap-oob (implicit "true") -> insert right after the attribute name.
        return html.Insert(after, stamp);
    }
}
