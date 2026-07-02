using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Swap.Htmx;
using Swap.Htmx.Middleware;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapFlashTests
{
    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static ITempDataDictionary NewTempData()
        => new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());

    [Fact]
    public void WithFlash_RecordsFlashToasts()
    {
        var builder = new SwapResponseBuilder()
            .WithFlash("saved", ToastType.Success)
            .WithFlash("note", ToastType.Info);

        Assert.Equal(2, builder.FlashToasts.Count);
        Assert.Equal("saved", builder.FlashToasts[0].Message);
        Assert.Equal(ToastType.Success, builder.FlashToasts[0].Type);
    }

    [Fact]
    public void Store_Then_TakePending_RoundTrips_AndConsumesOnce()
    {
        var td = NewTempData();
        SwapFlashHelper.Store(td, new List<ToastNotification>
        {
            new("saved", ToastType.Success),
            new("careful", ToastType.Warning),
        });

        Assert.True(td.ContainsKey(SwapFlashHelper.TempDataKey));

        var taken = SwapFlashHelper.TakePending(td);
        Assert.Equal(2, taken.Count);
        Assert.Equal("saved", taken[0].Message);
        Assert.Equal(ToastType.Success, taken[0].Type);
        Assert.Equal("careful", taken[1].Message);
        Assert.Equal(ToastType.Warning, taken[1].Type);

        // Consumed exactly once — a second take yields nothing.
        Assert.False(td.ContainsKey(SwapFlashHelper.TempDataKey));
        Assert.Empty(SwapFlashHelper.TakePending(td));
    }

    [Fact]
    public void Emit_SetsHxTriggerShowToast()
    {
        var ctx = new DefaultHttpContext();

        SwapFlashHelper.Emit(ctx.Response, new List<ToastNotification> { new("hello", ToastType.Success) });

        var trigger = ctx.Response.Headers["HX-Trigger"].ToString();
        Assert.Contains("showToast", trigger);
        Assert.Contains("hello", trigger);
    }
}
