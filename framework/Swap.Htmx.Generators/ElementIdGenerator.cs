using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Swap.Htmx.Generators;

/// <summary>
/// Source generator that creates strongly-typed element ID constants
/// by scanning .cshtml files for id="..." attributes.
/// </summary>
[Generator]
public class ElementIdGenerator : IIncrementalGenerator
{
    // Regex to match id="value" or id='value' in HTML/Razor
    // Handles: id="my-id", id='my-id', id="@Model.Id" (skipped - dynamic)
    private static readonly Regex IdAttributeRegex = new Regex(
        @"id\s*=\s*[""']([^""'@][^""']*)[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find classes with [SwapElementSource] attribute
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static t => t is not null)
            .Select(static (t, _) => t!);

        // Step 2: Collect all additional files (.cshtml)
        var cshtmlFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));

        // Step 3: Combine class declarations with cshtml files
        var combined = classDeclarations.Combine(cshtmlFiles.Collect());

        // Step 4: Generate source
        context.RegisterSourceOutput(combined, static (spc, source) => Execute(spc, source.Left!, source.Right));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0;
    }

    private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name.Contains("SwapElementSource"))
                {
                    // Extract the path argument
                    var pathArg = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    if (pathArg?.Expression is LiteralExpressionSyntax literal)
                    {
                        var path = literal.Token.ValueText;
                        
                        // Check for optional named arguments
                        var includeSubdirs = false;
                        string? prefix = null;
                        
                        foreach (var arg in attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                        {
                            if (arg.NameEquals?.Name.Identifier.Text == "IncludeSubdirectories" &&
                                arg.Expression is LiteralExpressionSyntax boolLiteral)
                            {
                                includeSubdirs = boolLiteral.Token.Value is true;
                            }
                            else if (arg.NameEquals?.Name.Identifier.Text == "Prefix" &&
                                     arg.Expression is LiteralExpressionSyntax prefixLiteral)
                            {
                                prefix = prefixLiteral.Token.ValueText;
                            }
                        }

                        return new ClassInfo(
                            classDeclaration,
                            path,
                            includeSubdirs,
                            prefix,
                            GetNamespace(classDeclaration));
                    }
                }
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        ClassInfo classInfo,
        System.Collections.Immutable.ImmutableArray<AdditionalText> cshtmlFiles)
    {
        var className = classInfo.ClassDeclaration.Identifier.Text;
        var namespaceName = classInfo.Namespace;
        var viewsPath = NormalizePath(classInfo.ViewsPath);

        // Filter cshtml files to those matching the path
        var matchingFiles = cshtmlFiles
            .Where(f => PathMatchesViewsFolder(f.Path, viewsPath, classInfo.IncludeSubdirectories))
            .ToList();

        // Extract all IDs from matching files
        var allIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var file in matchingFiles)
        {
            var text = file.GetText();
            if (text is null)
                continue;

            var content = text.ToString();
            if (string.IsNullOrEmpty(content))
                continue;

            var ids = ExtractIds(content);
            foreach (var id in ids)
            {
                // Apply prefix filter if specified
                if (string.IsNullOrEmpty(classInfo.Prefix) || 
                    id.StartsWith(classInfo.Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    allIds.Add(id);
                }
            }
        }

        if (allIds.Count == 0)
        {
            var emptySource = GenerateEmptySource(namespaceName, className, viewsPath);
            context.AddSource($"{className}.g.cs", SourceText.From(emptySource, Encoding.UTF8));
            return;
        }

        var sortedIds = allIds.OrderBy(id => id).ToList();
        var source = GenerateSource(namespaceName, className, sortedIds);
        context.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static IEnumerable<string> ExtractIds(string content)
    {
        var matches = IdAttributeRegex.Matches(content);
        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                var id = match.Groups[1].Value.Trim();
                
                // Skip empty IDs or IDs that look like Razor expressions
                if (!string.IsNullOrEmpty(id) && 
                    !id.Contains("@") && 
                    !id.Contains("{") &&
                    !id.Contains("}"))
                {
                    yield return id;
                }
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    private static bool PathMatchesViewsFolder(string filePath, string viewsPath, bool includeSubdirs)
    {
        var normalizedFilePath = NormalizePath(filePath);
        
        var idx = normalizedFilePath.IndexOf(viewsPath, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;

        if (!includeSubdirs)
        {
            var afterPath = normalizedFilePath.Substring(idx + viewsPath.Length);
            var slashCount = afterPath.Count(c => c == '/');
            return slashCount == 1;
        }

        return true;
    }

    private static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        var potentialNamespaceParent = syntax.Parent;
        while (potentialNamespaceParent != null &&
               !(potentialNamespaceParent is NamespaceDeclarationSyntax) &&
               !(potentialNamespaceParent is FileScopedNamespaceDeclarationSyntax))
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            return namespaceParent.Name.ToString();
        }

        return "Global";
    }

    private static string GenerateEmptySource(string namespaceName, string className, string viewsPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine($"// No element IDs found in path: {viewsPath}");
        sb.AppendLine($"// Ensure .cshtml files are included as AdditionalFiles in your .csproj");
        sb.AppendLine();

        if (namespaceName != "Global")
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static partial class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine($"public static partial class {className}");
            sb.AppendLine("{");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string GenerateSource(string namespaceName, string className, List<string> ids)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine();

        var indent = namespaceName != "Global" ? "    " : "";

        if (namespaceName != "Global")
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"{indent}public static partial class {className}");
        sb.AppendLine($"{indent}{{");

        foreach (var id in ids)
        {
            var constantName = ToValidIdentifier(id);
            sb.AppendLine($"{indent}    public const string {constantName} = \"{id}\";");
        }

        sb.AppendLine($"{indent}}}");

        if (namespaceName != "Global")
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string ToValidIdentifier(string id)
    {
        if (string.IsNullOrEmpty(id)) return "_";

        var sb = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in id)
        {
            if (c == '-' || c == '_' || c == '.')
            {
                capitalizeNext = true;
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                if (sb.Length == 0 && char.IsDigit(c))
                {
                    sb.Append('_');
                }
                
                sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }
        }

        return sb.Length > 0 ? sb.ToString() : "_";
    }

    private class ClassInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; }
        public string ViewsPath { get; }
        public bool IncludeSubdirectories { get; }
        public string? Prefix { get; }
        public string Namespace { get; }

        public ClassInfo(ClassDeclarationSyntax classDeclaration, string viewsPath, 
                        bool includeSubdirectories, string? prefix, string ns)
        {
            ClassDeclaration = classDeclaration;
            ViewsPath = viewsPath;
            IncludeSubdirectories = includeSubdirectories;
            Prefix = prefix;
            Namespace = ns;
        }
    }
}
