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
        // Normalize line endings to the platform's Environment.NewLine. The Roslyn test harness formats
        // the newly generated code with Environment.NewLine (LF on Linux CI, CRLF on Windows) and
        // preserves the existing document's line endings, so both the TestCode (existing) and the
        // FixedCode (existing + generated) must use Environment.NewLine to compare byte-for-byte on any
        // platform, independent of the checkout's autocrlf / .gitattributes.
        static string Nl(string s) => s.Replace("\r\n", "\n").Replace("\n", System.Environment.NewLine);

        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = Nl(source),
            FixedCode = Nl(fixedSource),
            // HandlerValidationAnalyzer reports SWAP001 as a compilation-end ("non-local") diagnostic
            // so it can see the whole compilation (all triggers/handlers) before deciding an event is
            // unhandled. That is by design (see HandlerValidationAnalyzer.CompilationEndTags), and Roslyn
            // itself fully supports code fixes for compilation-end diagnostics in real editors. This test
            // harness flag only opts out of an extra, testing-only assertion that assumes syntax/semantic
            // (per-document) diagnostics.
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck,
            // Apply the fix exactly once. The scaffolded handler is intentionally unregistered (the dev
            // wires it up next), so SWAP001 can still be reported after the fix; iterating to a fixpoint
            // would keep scaffolding uniquely-named handlers (OrderCreatedHandler2, ...). One-shot
            // application is the intended behavior and is deterministic across Roslyn/SDK versions.
            NumberOfIncrementalIterations = 1,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}
