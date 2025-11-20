using Microsoft.Extensions.Logging;

namespace Swap.Htmx.Diagnostics;

/// <summary>
/// High-performance logging definitions for Swap.Htmx.
/// </summary>
internal static partial class SwapLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Swap Event Triggered: {EventName} with payload {PayloadType}")]
    public static partial void EventTriggered(this ILogger logger, string eventName, string payloadType);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "Processing Event Chain for: {EventName}. Found {ChainCount} handlers.")]
    public static partial void ProcessingEventChain(this ILogger logger, string eventName, int chainCount);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Event Chain Error for {EventName}: {ErrorMessage}")]
    public static partial void EventChainError(this ILogger logger, string eventName, string errorMessage, Exception? ex);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "SSE Broadcast: {EventName} to {TargetType} ({TargetValue})")]
    public static partial void SseBroadcast(this ILogger logger, string eventName, string targetType, string targetValue);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "SSE Connection Established: {ConnectionId} (User: {User})")]
    public static partial void SseConnectionEstablished(this ILogger logger, string connectionId, string user);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "SSE Connection Closed: {ConnectionId}")]
    public static partial void SseConnectionClosed(this ILogger logger, string connectionId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Warning,
        Message = "Unknown SSE Filter Key: {FilterKey}")]
    public static partial void SseUnknownFilter(this ILogger logger, string filterKey);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Debug,
        Message = "Rendering Partial: {ViewName} for Event: {EventName}")]
    public static partial void RenderingPartial(this ILogger logger, string viewName, string eventName);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Debug,
        Message = "SSE Render Start: {EventName}")]
    public static partial void SseRenderStart(this ILogger logger, string eventName);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Debug,
        Message = "SSE Render No Config: {EventName}")]
    public static partial void SseRenderNoConfig(this ILogger logger, string eventName);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Debug,
        Message = "SSE Render Success: {EventName} ({Length} chars)")]
    public static partial void SseRenderSuccess(this ILogger logger, string eventName, int length);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Error,
        Message = "SSE Render Error: {EventName}")]
    public static partial void SseRenderError(this ILogger logger, Exception ex, string eventName);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Debug,
        Message = "Toast Notification: {Type} - {Message}")]
    public static partial void Toast(this ILogger logger, string type, string message);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Debug,
        Message = "Trigger Event: {EventName} (Payload: {PayloadType})")]
    public static partial void Trigger(this ILogger logger, string eventName, string payloadType);
}
