using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Swap.Htmx.Generators;

/// <summary>
/// Roslyn diagnostic analyzer that validates Swap.Htmx event handler configurations.
/// Produces warnings for:
/// - Events triggered but with no registered handlers
/// - Event chains referencing non-existent events
/// - Potential circular event chains
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerValidationAnalyzer : DiagnosticAnalyzer
{
    // SWAP001: Event triggered with no handler
    public static readonly DiagnosticDescriptor NoHandlerForEvent = new(
        id: "SWAP001",
        title: "Event has no registered handler",
        messageFormat: "Event '{0}' is triggered but no ISwapEventConfiguration handles it",
        category: "Swap.Htmx",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Events triggered via WithTrigger() should have a corresponding handler in an ISwapEventConfiguration.");

    // SWAP002: Event chain references non-existent event
    public static readonly DiagnosticDescriptor EventNotDefined = new(
        id: "SWAP002",
        title: "Event key not defined",
        messageFormat: "Event key '{0}' is referenced but not defined in any [SwapEventSource] class",
        category: "Swap.Htmx",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Events used in When() should be defined as const strings with [SwapEventSource].");

    // SWAP003: Potential circular event chain
    public static readonly DiagnosticDescriptor CircularEventChain = new(
        id: "SWAP003",
        title: "Potential circular event chain",
        messageFormat: "Event chain for '{0}' may create a circular dependency: {1}",
        category: "Swap.Htmx",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Event chains that trigger each other can cause infinite loops.");

    // SWAP004: Duplicate event handler
    public static readonly DiagnosticDescriptor DuplicateEventHandler = new(
        id: "SWAP004",
        title: "Duplicate event handler",
        messageFormat: "Event '{0}' has multiple handlers in the same configuration",
        category: "Swap.Htmx",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Having multiple handlers for the same event in one configuration may be intentional but could indicate a mistake.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            NoHandlerForEvent,
            EventNotDefined,
            CircularEventChain,
            DuplicateEventHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Analyze at compilation end to see all events and handlers
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var eventCollector = new EventCollector();
            
            // Collect all event definitions from [SwapEventSource] classes
            compilationContext.RegisterSyntaxNodeAction(
                ctx => CollectEventDefinitions(ctx, eventCollector),
                SyntaxKind.ClassDeclaration);

            // Collect all .WithTrigger() calls
            compilationContext.RegisterSyntaxNodeAction(
                ctx => CollectTriggerCalls(ctx, eventCollector),
                SyntaxKind.InvocationExpression);

            // Collect all events.When() handlers
            compilationContext.RegisterSyntaxNodeAction(
                ctx => CollectEventHandlers(ctx, eventCollector),
                SyntaxKind.InvocationExpression);

            // At the end of compilation, report diagnostics
            compilationContext.RegisterCompilationEndAction(ctx =>
            {
                ReportDiagnostics(ctx, eventCollector);
            });
        });
    }

    private static void CollectEventDefinitions(SyntaxNodeAnalysisContext context, EventCollector collector)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        
        // Check if class has [SwapEventSource] attribute
        var hasAttribute = classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("SwapEventSource"));

        if (!hasAttribute)
            return;

        // Collect all const string fields (these are event definitions)
        foreach (var member in classDeclaration.Members.OfType<FieldDeclarationSyntax>())
        {
            if (!member.Modifiers.Any(SyntaxKind.ConstKeyword))
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(member.Declaration.Type);
            if (typeInfo.Type?.SpecialType != SpecialType.System_String)
                continue;

            foreach (var variable in member.Declaration.Variables)
            {
                if (variable.Initializer?.Value is LiteralExpressionSyntax literal)
                {
                    var eventName = literal.Token.ValueText;
                    var location = variable.GetLocation();
                    collector.AddDefinedEvent(eventName, location);
                }
            }
        }
    }

    private static void CollectTriggerCalls(SyntaxNodeAnalysisContext context, EventCollector collector)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        // Check if this is a .WithTrigger() call
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "WithTrigger")
            return;

        // Get the event name from the argument
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
            return;

        var eventName = GetEventNameFromArgument(args[0].Expression, context.SemanticModel);
        if (!string.IsNullOrEmpty(eventName))
        {
            collector.AddTriggeredEvent(eventName!, invocation.GetLocation());
        }
    }

    private static void CollectEventHandlers(SyntaxNodeAnalysisContext context, EventCollector collector)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        // Check if this is an events.When() call
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "When")
            return;

        // Get the event name from the argument
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
            return;

        var eventName = GetEventNameFromArgument(args[0].Expression, context.SemanticModel);
        if (!string.IsNullOrEmpty(eventName))
        {
            collector.AddHandledEvent(eventName!, invocation.GetLocation());
        }
    }

    private static string? GetEventNameFromArgument(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // Handle string literal: WithTrigger("event.name")
        if (expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        // Handle member access: WithTrigger(ProductEvents.Product.Created)
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Try to get the constant value
            var constantValue = semanticModel.GetConstantValue(expression);
            if (constantValue.HasValue && constantValue.Value is string strValue)
            {
                return strValue;
            }

            // Try to evaluate the EventKey.Name property
            var symbol = semanticModel.GetSymbolInfo(expression).Symbol;
            if (symbol is IFieldSymbol field && field.HasConstantValue && field.ConstantValue is string fieldValue)
            {
                return fieldValue;
            }
        }

        // Handle EventKey: WithTrigger(new EventKey("event.name"))
        if (expression is ObjectCreationExpressionSyntax creation)
        {
            var argList = creation.ArgumentList;
            if (argList != null && argList.Arguments.Count > 0)
            {
                var firstArg = argList.Arguments[0];
                if (firstArg.Expression is LiteralExpressionSyntax litArg)
                {
                    return litArg.Token.ValueText;
                }
            }
        }

        return null;
    }

    private static void ReportDiagnostics(CompilationAnalysisContext context, EventCollector collector)
    {
        // SWAP001: Events triggered but not handled
        foreach (var triggered in collector.TriggeredEvents)
        {
            if (!collector.HandledEvents.ContainsKey(triggered.Key))
            {
                foreach (var location in triggered.Value)
                {
                    var diagnostic = Diagnostic.Create(
                        NoHandlerForEvent,
                        location,
                        triggered.Key);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // SWAP002: Events referenced in When() but not defined
        foreach (var handled in collector.HandledEvents)
        {
            if (!collector.DefinedEvents.ContainsKey(handled.Key) && 
                !collector.TriggeredEvents.ContainsKey(handled.Key))
            {
                // Only warn if the event name looks like it should be defined
                // (skip generic names that might come from external sources)
                if (handled.Key.Contains("."))
                {
                    foreach (var location in handled.Value)
                    {
                        var diagnostic = Diagnostic.Create(
                            EventNotDefined,
                            location,
                            handled.Key);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        // SWAP004: Duplicate handlers in same configuration
        foreach (var handled in collector.HandledEvents.Where(h => h.Value.Count > 1))
        {
            // Report on subsequent occurrences
            foreach (var location in handled.Value.Skip(1))
            {
                var diagnostic = Diagnostic.Create(
                    DuplicateEventHandler,
                    location,
                    handled.Key);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Note: Circular chain detection would require tracking event chain relationships
        // which is more complex and would need to analyze the RefreshPartial calls
        // that might trigger additional events. This is left as a future enhancement.
    }

    /// <summary>
    /// Collects events during compilation analysis.
    /// </summary>
    private class EventCollector
    {
        public Dictionary<string, List<Location>> DefinedEvents { get; } = new();
        public Dictionary<string, List<Location>> TriggeredEvents { get; } = new();
        public Dictionary<string, List<Location>> HandledEvents { get; } = new();

        public void AddDefinedEvent(string eventName, Location location)
        {
            if (!DefinedEvents.TryGetValue(eventName, out var list))
            {
                list = new List<Location>();
                DefinedEvents[eventName] = list;
            }
            list.Add(location);
        }

        public void AddTriggeredEvent(string eventName, Location location)
        {
            if (!TriggeredEvents.TryGetValue(eventName, out var list))
            {
                list = new List<Location>();
                TriggeredEvents[eventName] = list;
            }
            list.Add(location);
        }

        public void AddHandledEvent(string eventName, Location location)
        {
            if (!HandledEvents.TryGetValue(eventName, out var list))
            {
                list = new List<Location>();
                HandledEvents[eventName] = list;
            }
            list.Add(location);
        }
    }
}
