using Microsoft.AspNetCore.Http;
using Xunit;

namespace Swap.Htmx.Tests;

public class HtmxNewExtensionsTests
{
    [Fact]
    public void EnsureVaryHxRequest_AddsHeader_WhenMissing()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.EnsureVaryHxRequest();
        Assert.Equal(HxHeaders.Request, ctx.Response.Headers["Vary"].ToString());
    }

    [Fact]
    public void EnsureVaryHxRequest_Appends_WhenOtherValuesExist()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Headers["Vary"] = "Accept-Encoding";
        ctx.Response.EnsureVaryHxRequest();
        var vary = ctx.Response.Headers["Vary"].ToString();
        Assert.Contains("Accept-Encoding", vary);
        Assert.Contains(HxHeaders.Request, vary);
        Assert.Contains(",", vary);
    }

    [Fact]
    public void EnsureVaryHxRequest_DoesNotDuplicate()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Headers["Vary"] = HxHeaders.Request;
        ctx.Response.EnsureVaryHxRequest();
        Assert.Equal(HxHeaders.Request, ctx.Response.Headers["Vary"].ToString());
    }

    [Fact]
    public void IsHtmxHistoryRestoreRequest_True_WhenHeaderTrue()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[HxHeaders.HistoryRestore] = "true";
        Assert.True(ctx.Request.IsHtmxHistoryRestoreRequest());
    }

    [Fact]
    public void IsHtmxHistoryRestoreRequest_False_WhenMissingOrFalse()
    {
        var ctx1 = new DefaultHttpContext();
        Assert.False(ctx1.Request.IsHtmxHistoryRestoreRequest());

        var ctx2 = new DefaultHttpContext();
        ctx2.Request.Headers[HxHeaders.HistoryRestore] = "false";
        Assert.False(ctx2.Request.IsHtmxHistoryRestoreRequest());
    }

    [Fact]
    public void HxStopPolling_Sets_Status_286()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.HxStopPolling();
        Assert.Equal(286, ctx.Response.StatusCode);
    }
}
