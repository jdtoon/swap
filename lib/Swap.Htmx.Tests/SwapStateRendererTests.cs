using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.State;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for the shared request-scoped state render path used by all result types.
/// Guards against the "protected state rendered as plaintext" class of bug.
/// </summary>
public class SwapStateRendererTests
{
    private sealed class SecretState : SwapState
    {
        public override bool Protected => true;
        public string Secret { get; set; } = "hidden";
    }

    private sealed class PlainState : SwapState
    {
        public string Name { get; set; } = "n";
    }

    private static HttpContext ContextWith(bool dataProtection)
    {
        var services = new ServiceCollection();
        if (dataProtection)
        {
            services.AddDataProtection();
        }

        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }

    [Fact]
    public void RenderAsOobForRequest_EncryptsProtectedState_WhenProviderRegistered()
    {
        var html = SwapStateRenderer.RenderAsOobForRequest(
            new SecretState { Secret = "TopSecret" }, ContextWith(dataProtection: true));

        Assert.Contains("name=\"Secret\"", html);
        Assert.DoesNotContain("TopSecret", html); // must be encrypted, never plaintext
    }

    [Fact]
    public void RenderAsOobForRequest_Throws_WhenProtectedButNoProvider()
    {
        // Fail closed on the render side too: refuse to leak protected values as plaintext.
        Assert.Throws<InvalidOperationException>(() =>
            SwapStateRenderer.RenderAsOobForRequest(
                new SecretState { Secret = "TopSecret" }, ContextWith(dataProtection: false)));
    }

    [Fact]
    public void RenderAsOobForRequest_RendersPlainState_WhenNotProtected()
    {
        var html = SwapStateRenderer.RenderAsOobForRequest(
            new PlainState { Name = "Alice" }, ContextWith(dataProtection: false));

        Assert.Contains("Alice", html);
    }
}
