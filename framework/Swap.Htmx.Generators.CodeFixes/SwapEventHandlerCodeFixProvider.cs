using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

#nullable enable

namespace Swap.Htmx.Generators.CodeFixes;

/// <summary>
/// Offers a fix for SWAP001 ("Event has no registered handler") by scaffolding a distributed
/// <c>ISwapEventHandler&lt;T&gt;</c> implementation for the triggered event's payload type,
/// next to the code that raised the diagnostic.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwapEventHandlerCodeFixProvider)), Shared]
public sealed class SwapEventHandlerCodeFixProvider : CodeFixProvider
{
    private const string HandlerInterfaceName = "ISwapEventHandler";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(HandlerValidationAnalyzer.NoHandlerForEvent.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id != HandlerValidationAnalyzer.NoHandlerForEvent.Id)
                continue;

            var invocation = root.FindNode(diagnostic.Location.SourceSpan)
                .FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation is null)
                continue;

            var args = invocation.ArgumentList.Arguments;
            if (args.Count < 2)
                continue;

            var eventName = HandlerValidationAnalyzer.GetEventNameFromArgument(args[0].Expression, semanticModel);
            if (string.IsNullOrEmpty(eventName))
                continue;

            var payloadType = semanticModel.GetTypeInfo(args[1].Expression, context.CancellationToken).Type;
            if (payloadType is null || payloadType.SpecialType == SpecialType.System_Object || payloadType is IErrorTypeSymbol)
                continue;

            var handlerInterface = FindEventHandlerInterface(semanticModel.Compilation);
            if (handlerInterface is null)
                continue;

            var baseName = BuildHandlerClassName(eventName!);
            var className = MakeUniqueTypeName(baseName, semanticModel.Compilation);
            var title = $"Generate {handlerInterface.Name}<{payloadType.Name}> handler '{className}' for '{eventName}'";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => GenerateHandlerAsync(context.Document, invocation, className, payloadType, handlerInterface, ct),
                    equivalenceKey: nameof(SwapEventHandlerCodeFixProvider)),
                diagnostic);
        }
    }

    /// <summary>
    /// Finds an accessible <c>ISwapEventHandler&lt;T&gt;</c>-shaped interface (any namespace) in the
    /// compilation. The analyzer matches purely on name/arity, so the fix does the same to stay consistent.
    /// </summary>
    private static INamedTypeSymbol? FindEventHandlerInterface(Compilation compilation)
    {
        return compilation.GetSymbolsWithName(name => name == HandlerInterfaceName, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault(t => t.TypeKind == TypeKind.Interface && t.Arity == 1);
    }

    /// <summary>
    /// Ensures the generated handler class name doesn't collide with an existing type — a prior
    /// application of this fix, or a pre-existing user type — by appending a numeric suffix until unique.
    /// </summary>
    internal static string MakeUniqueTypeName(string baseName, Compilation compilation)
    {
        bool Exists(string n) => compilation.GetSymbolsWithName(name => name == n, SymbolFilter.Type).Any();

        if (!Exists(baseName))
            return baseName;

        for (var i = 2; i < 10000; i++)
        {
            var candidate = baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!Exists(candidate))
                return candidate;
        }

        return baseName + System.Guid.NewGuid().ToString("N");
    }

    private static async Task<Document> GenerateHandlerAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        string className,
        ITypeSymbol payloadType,
        INamedTypeSymbol handlerInterface,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var constructedInterface = handlerInterface.Construct(payloadType);

        var members = constructedInterface.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Select(method => BuildMethodImplementation(generator, method))
            .ToList();

        var classDeclaration = generator.ClassDeclaration(
            className,
            accessibility: Accessibility.Public,
            interfaceTypes: new[] { generator.TypeExpression(constructedInterface) },
            members: members);

        var containingType = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingType is null)
        {
            // No enclosing type (e.g. top-level statements) — append at the end of the file.
            editor.InsertAfter(editor.OriginalRoot.ChildNodes().Last(), classDeclaration);
        }
        else
        {
            editor.InsertAfter(containingType, classDeclaration);
        }

        var changedDocument = editor.GetChangedDocument();
        changedDocument = await Simplifier.ReduceAsync(changedDocument, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        changedDocument = await Formatter.FormatAsync(changedDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        return changedDocument;
    }

    /// <summary>
    /// Builds a concrete (non-abstract) implementation of an interface method with a
    /// <see cref="NotImplementedException"/> body, since <see cref="SyntaxGenerator.MethodDeclaration(IMethodSymbol, IEnumerable{SyntaxNode})"/>
    /// would otherwise reproduce the interface member's own "abstract" modifier.
    /// </summary>
    private static SyntaxNode BuildMethodImplementation(SyntaxGenerator generator, IMethodSymbol method)
    {
        return generator.MethodDeclaration(
            method.Name,
            parameters: method.Parameters.Select(p => generator.ParameterDeclaration(p)),
            returnType: generator.TypeExpression(method.ReturnType),
            accessibility: Accessibility.Public,
            modifiers: DeclarationModifiers.None,
            statements: BuildNotImplementedBody(generator));
    }

    private static IEnumerable<SyntaxNode> BuildNotImplementedBody(SyntaxGenerator generator)
    {
        yield return generator.ThrowStatement(
            generator.ObjectCreationExpression(
                generator.DottedName("System.NotImplementedException")));
    }

    /// <summary>
    /// Converts an event name like <c>"order.created"</c> into a PascalCase handler class name
    /// (<c>OrderCreatedHandler</c>).
    /// </summary>
    internal static string BuildHandlerClassName(string eventName)
    {
        var builder = new StringBuilder();
        var capitalizeNext = true;

        foreach (var ch in eventName)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(capitalizeNext ? char.ToUpperInvariant(ch) : ch);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        if (builder.Length == 0)
            builder.Append("Event");

        builder.Append("Handler");
        return builder.ToString();
    }
}
