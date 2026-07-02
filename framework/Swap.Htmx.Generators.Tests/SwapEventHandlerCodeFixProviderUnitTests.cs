using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Swap.Htmx.Generators.CodeFixes;
using Xunit;

namespace Swap.Htmx.Generators.Tests;

public class SwapEventHandlerCodeFixProviderUnitTests
{
    [Theory]
    [InlineData("order.created", "OrderCreatedHandler")]
    [InlineData("cart.item-added", "CartItemAddedHandler")]
    [InlineData("Product_Deleted", "ProductDeletedHandler")]
    [InlineData("...", "EventHandler")]
    public void BuildHandlerClassName_ProducesPascalCaseHandlerName(string eventName, string expected)
    {
        Assert.Equal(expected, SwapEventHandlerCodeFixProvider.BuildHandlerClassName(eventName));
    }

    [Fact]
    public void MakeUniqueTypeName_AppendsSuffix_WhenNameAlreadyExists()
    {
        var tree = CSharpSyntaxTree.ParseText("public class OrderCreatedHandler { }");
        var compilation = CSharpCompilation.Create(
            "T",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        // Existing type -> suffixed; free name -> unchanged. Guards against duplicate-type generation
        // on re-applying the fix or colliding with a pre-existing user type.
        Assert.Equal("OrderCreatedHandler2", SwapEventHandlerCodeFixProvider.MakeUniqueTypeName("OrderCreatedHandler", compilation));
        Assert.Equal("FreshHandler", SwapEventHandlerCodeFixProvider.MakeUniqueTypeName("FreshHandler", compilation));
    }

    [Fact]
    public async Task NoCodeFixRegistered_WhenTriggerPayloadTypeIsObject()
    {
        // No typed payload -> the fix can't know which ISwapEventHandler<T> to scaffold, so it must
        // decline gracefully (matching HandlerValidationAnalyzerTests.SWAP001_StillReported_WhenTrulyUnhandled).
        const string source = @"
public class Builder { public Builder WithTrigger(string e, object payload) => this; }

public static class Usage
{
    public static void Configure(Builder b)
    {
        b.WithTrigger(""nothing.handles.this"", new object());
    }
}";

        var actions = await GetRegisteredCodeActionsAsync(source);
        Assert.Empty(actions);
    }

    [Fact]
    public async Task NoCodeFixRegistered_WhenNoEventHandlerInterfaceExistsInCompilation()
    {
        const string source = @"
public class OrderCreated { }
public class Builder { public Builder WithTrigger(string e, object payload) => this; }

public static class Usage
{
    public static void Configure(Builder b)
    {
        b.WithTrigger(""order.created"", new OrderCreated());
    }
}";

        var actions = await GetRegisteredCodeActionsAsync(source);
        Assert.Empty(actions);
    }

    private static async Task<IReadOnlyList<CodeAction>> GetRegisteredCodeActionsAsync(string source)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var docId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddDocument(docId, "Test0.cs", SourceText.From(source));

        var document = solution.GetDocument(docId)!;
        var compilation = await document.Project.GetCompilationAsync();
        var withAnalyzers = compilation!.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new HandlerValidationAnalyzer()));
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        var swap001 = diagnostics.Single(d => d.Id == "SWAP001");

        var provider = new SwapEventHandlerCodeFixProvider();
        var registered = new List<CodeAction>();
        var context = new CodeFixContext(document, swap001, (action, _) => registered.Add(action), default);
        await provider.RegisterCodeFixesAsync(context);

        return registered;
    }
}
