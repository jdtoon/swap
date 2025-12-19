using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Swap.Htmx.Diagnostics;
using Swap.Htmx.Models;
using Swap.Htmx.Results;
using Xunit;
using Microsoft.AspNetCore.Http.Features;

namespace Swap.Htmx.Tests;

public class MinimalApiTests
{
    [Fact]
    public async Task SwapResult_Invokes_Diagnostics_ValidateResponse()
    {
        // Arrange
        var builder = new SwapResponseBuilder().WithTrigger("my-event");
        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var diagnosticsMock = new Mock<ISwapDiagnostics>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;

        servicesMock.Setup(s => s.GetService(typeof(ISwapDiagnostics))).Returns(diagnosticsMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        diagnosticsMock.Verify(d => d.ValidateResponse(It.IsAny<SwapResponseBuilder>()), Times.Once);
    }

    [Fact]
    public async Task SwapResult_Executes_And_Renders_View()
    {
        // Arrange
        var builder = new SwapResponseBuilder()
            .WithView("MyView", new { Name = "Test" });

        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var viewEngineMock = new Mock<ICompositeViewEngine>();
        var viewMock = new Mock<IView>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;

        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ICompositeViewEngine))).Returns(viewEngineMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        viewEngineMock.Setup(v => v.FindView(It.IsAny<ActionContext>(), "MyView", false))
            .Returns(ViewEngineResult.Found("MyView", viewMock.Object));

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        viewMock.Verify(v => v.RenderAsync(It.IsAny<ViewContext>()), Times.Once);
    }

    [Fact]
    public async Task SwapResult_When_MainView_NotFound_Throws_With_Actionable_Guidance()
    {
        // Arrange
        var builder = new SwapResponseBuilder().WithView("MissingView", new { Name = "Test" });
        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var viewEngineMock = new Mock<ICompositeViewEngine>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;
        httpContext.Response.Body = new MemoryStream();

        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ICompositeViewEngine))).Returns(viewEngineMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        viewEngineMock
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "MissingView", false))
            .Returns(ViewEngineResult.NotFound("MissingView", new[] { "~/Views/Shared/MissingView.cshtml" }));

        viewEngineMock
            .Setup(v => v.GetView(null, "MissingView", false))
            .Returns(ViewEngineResult.NotFound("MissingView", new[] { "MissingView" }));

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteAsync(httpContext));

        // Assert
        Assert.Contains("Could not find view 'MissingView'", ex.Message);
        Assert.Contains("Fix:", ex.Message);
        Assert.Contains("WithView", ex.Message);
        Assert.Contains("~/Views", ex.Message);
    }

    [Fact]
    public async Task SwapResult_When_OobView_NotFound_Throws_With_Actionable_Guidance()
    {
        // Arrange
        var builder = new SwapResponseBuilder().AlsoUpdate("target-id", "MissingOobView", null, SwapMode.OuterHTML);
        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var viewEngineMock = new Mock<ICompositeViewEngine>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;
        httpContext.Response.Body = new MemoryStream();

        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ICompositeViewEngine))).Returns(viewEngineMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        viewEngineMock
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "MissingOobView", false))
            .Returns(ViewEngineResult.NotFound("MissingOobView", new[] { "~/Views/Shared/MissingOobView.cshtml" }));

        viewEngineMock
            .Setup(v => v.GetView(null, It.IsAny<string>(), false))
            .Returns(ViewEngineResult.NotFound("MissingOobView", new[] { "~/Views/Shared/MissingOobView.cshtml" }));

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteAsync(httpContext));

        // Assert
        Assert.Contains("Could not find view 'MissingOobView'", ex.Message);
        Assert.Contains("OOB swap", ex.Message);
        Assert.Contains("Fix:", ex.Message);
        Assert.Contains("PartialViewSearchPaths", ex.Message);
    }

    [Fact]
    public async Task SwapResult_Executes_And_Writes_OobSwaps()
    {
        // Arrange
        var builder = new SwapResponseBuilder()
            .AlsoUpdate("target-id", "MyOobView", null, SwapMode.OuterHTML);

        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var viewEngineMock = new Mock<ICompositeViewEngine>();
        var viewMock = new Mock<IView>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;
        httpContext.Response.Body = new MemoryStream();

        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ICompositeViewEngine))).Returns(viewEngineMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        viewEngineMock.Setup(v => v.FindView(It.IsAny<ActionContext>(), "MyOobView", false))
            .Returns(ViewEngineResult.Found("MyOobView", viewMock.Object));

        viewMock.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback<ViewContext>(vc => vc.Writer.Write("<div>Content</div>"))
            .Returns(Task.CompletedTask);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("hx-swap-oob=\"true\"", body); // Default is OuterHTML which maps to hx-swap-oob="true"
        Assert.Contains("id=\"target-id\"", body);
        Assert.Contains("<div>Content</div>", body);
    }

    [Fact]
    public async Task SwapResult_Normalizes_OobSwap_TargetId_When_Passed_As_CssIdSelector()
    {
        // Arrange
        var builder = new SwapResponseBuilder()
            .AlsoUpdate("#target-id", "MyOobView", null, SwapMode.OuterHTML);

        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var viewEngineMock = new Mock<ICompositeViewEngine>();
        var viewMock = new Mock<IView>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;
        httpContext.Response.Body = new MemoryStream();

        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ICompositeViewEngine))).Returns(viewEngineMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        viewEngineMock.Setup(v => v.FindView(It.IsAny<ActionContext>(), "MyOobView", false))
            .Returns(ViewEngineResult.Found("MyOobView", viewMock.Object));

        viewMock.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback<ViewContext>(vc => vc.Writer.Write("<div>Content</div>"))
            .Returns(Task.CompletedTask);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("id=\"target-id\"", body);
        Assert.DoesNotContain("id=\"#target-id\"", body);
    }

    [Fact]
    public async Task SwapResult_Executes_And_Sets_Triggers()
    {
        // Arrange
        var builder = new SwapResponseBuilder()
            .WithTrigger("my-event");

        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;

        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.True(httpContext.Response.Headers.ContainsKey("HX-Trigger"));
        Assert.Equal("{\"my-event\": null}", httpContext.Response.Headers["HX-Trigger"]);
    }

    [Fact]
    public async Task SwapResult_Executes_And_Sets_Toasts()
    {
        // Arrange
        var builder = new SwapResponseBuilder()
            .WithSuccessToast("Operation successful");

        var result = new SwapResult(builder);

        var servicesMock = new Mock<IServiceProvider>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var modelMetadataProvider = new EmptyModelMetadataProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = servicesMock.Object;

        servicesMock.Setup(s => s.GetService(typeof(ILogger<SwapResult>))).Returns(Mock.Of<ILogger<SwapResult>>());
        servicesMock.Setup(s => s.GetService(typeof(ITempDataProvider))).Returns(tempDataProviderMock.Object);
        servicesMock.Setup(s => s.GetService(typeof(IModelMetadataProvider))).Returns(modelMetadataProvider);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.True(httpContext.Response.Headers.ContainsKey("HX-Trigger"));
        var triggerHeader = httpContext.Response.Headers["HX-Trigger"].ToString();
        Assert.Contains("showToast", triggerHeader);
        Assert.Contains("Operation successful", triggerHeader);
        Assert.Contains("success", triggerHeader);
    }
}
