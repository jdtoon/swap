using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swap.Htmx.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidStringLiteralsForSwapEventsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SWAPHTMX001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Avoid string literals for Swap.Htmx event names",
        messageFormat: "Avoid string literal '{0}' for event names in {1}.{2}; use constants or EventKey",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Use strongly-typed EventKey or constants for Swap.Htmx event names (Chain/Emit)."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation) return;
        var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (symbol is null) return;

        var methodName = symbol.Name;
        var containing = symbol.ContainingType;
        if (containing is null) return;

        // Target APIs: Swap.Htmx.Events.SwapEventBusOptions.Chain and ISwapEventBus/SwapEventBus.Emit/EmitAsync
        var isOptionsChain = methodName == "Chain" &&
                             containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                               .Contains("Swap.Htmx.Events.SwapEventBusOptions");

        var isBusEmit = (methodName == "Emit" || methodName == "EmitAsync") &&
                        (containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                           .Contains("Swap.Htmx.Events.ISwapEventBus") ||
                         containing.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                           .Contains("Swap.Htmx.Events.SwapEventBus"));

        if (!isOptionsChain && !isBusEmit) return;

        // Find string literal arguments
        if (invocation.ArgumentList is null) return;
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            var expr = arg.Expression;
            if (expr is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var literal = lit.Token.ValueText;
                var diag = Diagnostic.Create(Rule, lit.GetLocation(), literal, containing.Name, methodName);
                context.ReportDiagnostic(diag);
            }
        }
    }
}
