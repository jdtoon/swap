using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Swap.Htmx.Diagnostics;

/// <summary>
/// Centralized telemetry for Swap.Htmx.
/// </summary>
public static class SwapTelemetry
{
    internal static readonly AssemblyName AssemblyName = typeof(SwapTelemetry).Assembly.GetName();
    internal static readonly string ActivitySourceName = AssemblyName.Name ?? "Swap.Htmx";
    internal static readonly Version Version = AssemblyName.Version ?? new Version(1, 0, 0);

    /// <summary>
    /// The ActivitySource for Swap.Htmx traces.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());

    /// <summary>
    /// The Meter for Swap.Htmx metrics.
    /// </summary>
    public static readonly Meter Meter = new(ActivitySourceName, Version.ToString());

    // Metrics
    internal static readonly Counter<long> EventsTriggered = Meter.CreateCounter<long>(
        "swap.events.triggered", 
        description: "Total number of Swap events triggered");

    internal static readonly Counter<long> SseBroadcasts = Meter.CreateCounter<long>(
        "swap.sse.broadcasts", 
        description: "Total number of SSE broadcasts sent");

    internal static readonly Counter<long> SseConnections = Meter.CreateCounter<long>(
        "swap.sse.connections", 
        description: "Total number of active SSE connections");
        
    internal static readonly Histogram<double> EventProcessingDuration = Meter.CreateHistogram<double>(
        "swap.events.processing_duration",
        unit: "ms",
        description: "Duration of event chain processing");
}
