using System.Globalization;
using System.Text;

namespace Swap.Htmx.Models;

/// <summary>
/// Builds the attributes applied to an out-of-band swap element: the <c>hx-swap-oob</c> token plus the
/// optional <c>data-swap-seq</c> (monotonic version) and <c>data-swap-hash</c> (content fingerprint)
/// that the client guards read to drop stale or unchanged updates.
/// </summary>
internal static class SwapOobAttributes
{
    public static string Build(SwapMode mode, long? seq = null, string? hash = null)
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

        return sb.ToString();
    }
}
