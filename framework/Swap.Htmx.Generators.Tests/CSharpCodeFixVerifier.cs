using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Swap.Htmx.Generators.Tests;

/// <summary>
/// Thin wrapper over the Roslyn SDK code-fix testing framework, following the standard
/// analyzer-with-code-fix test template (using the non-obsolete <see cref="DefaultVerifier"/>).
/// </summary>
public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

    public static async Task VerifyCodeFixAsync(string source, string fixedSource)
        => await VerifyCodeFixAsync(source, System.Array.Empty<DiagnosticResult>(), fixedSource);

    public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

    public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            // HandlerValidationAnalyzer reports SWAP001 as a compilation-end ("non-local") diagnostic
            // so it can see the whole compilation (all triggers/handlers) before deciding an event is
            // unhandled. That is by design (see HandlerValidationAnalyzer.CompilationEndTags), and Roslyn
            // itself fully supports code fixes for compilation-end diagnostics in real editors. This test
            // harness flag only opts out of an extra, testing-only assertion that assumes syntax/semantic
            // (per-document) diagnostics.
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}
