using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Swap.Htmx.Models;

namespace Swap.Htmx.Middleware;

/// <summary>
/// Round-trips "flash" toasts (see <c>SwapResponseBuilder.WithFlash</c>) through TempData so a
/// success/error message survives an HTTP redirect and is shown on the next response.
/// </summary>
internal static class SwapFlashHelper
{
    internal const string TempDataKey = "Swap.Flash";

    private sealed record FlashDto(string Message, int Type);

    /// <summary>Stashes flash toasts into TempData to be emitted on the next response.</summary>
    public static void Store(ITempDataDictionary tempData, IReadOnlyList<ToastNotification> flashes)
    {
        if (tempData is null || flashes is null || flashes.Count == 0)
        {
            return;
        }

        var dtos = flashes.Select(f => new FlashDto(f.Message, (int)f.Type)).ToList();
        tempData[TempDataKey] = JsonSerializer.Serialize(dtos);
    }

    /// <summary>
    /// Reads and REMOVES any pending flash toasts from TempData, so they are consumed exactly once.
    /// </summary>
    public static IReadOnlyList<ToastNotification> TakePending(ITempDataDictionary tempData)
    {
        if (tempData is null ||
            !tempData.TryGetValue(TempDataKey, out var raw) ||
            raw is not string json ||
            string.IsNullOrEmpty(json))
        {
            return Array.Empty<ToastNotification>();
        }

        tempData.Remove(TempDataKey);

        try
        {
            var dtos = JsonSerializer.Deserialize<List<FlashDto>>(json);
            if (dtos is null)
            {
                return Array.Empty<ToastNotification>();
            }

            return dtos.Select(d => new ToastNotification(d.Message, (ToastType)d.Type)).ToList();
        }
        catch
        {
            return Array.Empty<ToastNotification>();
        }
    }

    /// <summary>Emits the given flash toasts as <c>HX-Trigger</c> <c>showToast</c> events on the response.</summary>
    public static void Emit(HttpResponse response, IReadOnlyList<ToastNotification> flashes)
    {
        if (response is null || flashes is null)
        {
            return;
        }

        foreach (var flash in flashes)
        {
            response.ShowToast(flash.Message, flash.Type);
        }
    }
}
