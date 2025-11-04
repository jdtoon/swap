using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Swap.Htmx.Middleware;
using Xunit;

namespace Swap.Htmx.Tests;

public class HxVaryHeaderMiddlewareTests
{
    [Fact]
    public async Task Middleware_Appends_Vary_HxRequest()
    {
        var context = new DefaultHttpContext();

        var mw = new HxVaryHeaderMiddleware(next: _ => Task.CompletedTask);
        await mw.Invoke(context);

        Assert.True(context.Response.Headers.ContainsKey("Vary"));
        Assert.Contains(HxHeaders.Request, context.Response.Headers["Vary"].ToString());
    }
}
