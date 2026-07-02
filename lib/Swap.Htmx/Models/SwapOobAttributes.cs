using System;
using System.Globalization;
using System.Text;

namespace Swap.Htmx.Models;

/// <summary>
/// Builds (and patches) the attributes applied to an out-of-band swap element: the <c>hx-swap-oob</c>
/// token plus the optional <c>data-swap-seq</c> (monotonic version) and <c>data-swap-hash</c> (content
/// fingerprint) the client guards read to drop stale, duplicate, or unchanged updates.
/// </summary>
internal static class SwapOobAttributes
{
    public static string Build(SwapMode mode, long? seq = null, string? hash = null, bool ifExists = false)
    {
        var sb = new StringBuilder();
        sb.Append("hx-swap-oob=\"").Append(mode.ToOobSwapToken()).Append('"');

        if (seq.HasValue)
        {
            sb.Append(" data-swap-seq=\"").Append(seq.Value.ToString(CultureInfo.InvariantCulture)).Append('"');
        }

        if (!string.IsNullOrEmpty(hash))
        {
            sb.Append(" data-swap-hash=\"").Append(hash).Append('"');
        }

        if (ifExists)
        {
            sb.Append(" data-swap-if-exists=\"true\"");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Injects <c>data-swap-seq</c> / <c>data-swap-hash</c> into a fragment that already declares its own
    /// <c>hx-swap-oob</c> (so the client guards still apply to self-declared OOB partials), skipping any
    /// stamp that is already present. No-op when both values are absent or no <c>hx-swap-oob</c> is found.
    /// </summary>
    public static string InjectStampsIfMissing(string html, long? seq, string? hash, bool ifExists = false)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        html = Inject(html, "data-swap-seq", seq.HasValue ? seq.Value.ToString(CultureInfo.InvariantCulture) : null);
        html = Inject(html, "data-swap-hash", string.IsNullOrEmpty(hash) ? null : hash);
        html = Inject(html, "data-swap-if-exists", ifExists ? "true" : null);
        return html;
    }

    private static string Inject(string html, string attr, string? value)
    {
        if (value is null || html.Contains(attr))
        {
            return html;
        }

        const string marker = "hx-swap-oob";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
        {
            return html;
        }

        var stamp = $" {attr}=\"{value}\"";
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

    /// <summary>
    /// A stable, cheap content fingerprint (FNV-1a, 64-bit, hex) of a rendered fragment. Stable across
    /// processes (unlike <see cref="string.GetHashCode()"/>), so the client can compare an incoming
    /// <c>data-swap-hash</c> against the one already on the target element and skip an unchanged swap.
    /// </summary>
    public static string ComputeContentHash(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "0";
        }

        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        var hash = offset;
        foreach (var c in content)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }
}
