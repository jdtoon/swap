using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Swap.Htmx.Generators;
using Xunit;

namespace Swap.Htmx.Generators.Tests;

public class HandlerValidationAnalyzerTests
{
    private static ImmutableArray<Diagnostic> Analyze(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new HandlerValidationAnalyzer()));

        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void SWAP001_NotReported_WhenEventHandledByTypedHandler()
    {
        // A triggered event whose payload is handled by an ISwapEventHandler<T> is NOT unhandled.
        const string source = @"
public class OrderCreated { }
public interface ISwapEventHandler<T> { }
public class OrderHandler : ISwapEventHandler<OrderCreated> { }

public class Builder { public Builder WithTrigger(string e, object payload) => this; }

public static class Usage
{
    public static void Configure(Builder b)
    {
        b.WithTrigger(""order.created"", new OrderCreated());
    }
}";
        var diagnostics = Analyze(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "SWAP001");
    }

    [Fact]
    public void SWAP001_StillReported_WhenTrulyUnhandled()
    {
        // No handler of any kind -> the warning should still fire.
        const string source = @"
public class Builder { public Builder WithTrigger(string e, object payload) => this; }

public static class Usage
{
    public static void Configure(Builder b)
    {
        b.WithTrigger(""nothing.handles.this"", new object());
    }
}";
        var diagnostics = Analyze(source);
        Assert.Contains(diagnostics, d => d.Id == "SWAP001");
    }

    [Fact]
    public void SupportedDiagnostics_HaveHelpLinks_AndNoDeadSwap003()
    {
        var analyzer = new HandlerValidationAnalyzer();
        var ids = analyzer.SupportedDiagnostics.Select(d => d.Id).ToHashSet();

        // SWAP003 was declared but never emitted; it must be removed so the rule set is trustworthy.
        Assert.DoesNotContain("SWAP003", ids);

        // Every supported rule must carry a help link for discoverability.
        Assert.All(analyzer.SupportedDiagnostics, d =>
            Assert.False(string.IsNullOrEmpty(d.HelpLinkUri), $"{d.Id} is missing a HelpLinkUri"));
    }
}
