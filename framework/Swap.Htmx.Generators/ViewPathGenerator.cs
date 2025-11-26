using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Swap.Htmx.Generators;

/// <summary>
/// Source generator that creates strongly-typed view path constants
/// from .cshtml files in the specified folder.
/// </summary>
[Generator]
public class ViewPathGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find classes with [SwapViewSource] attribute
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static t => t is not null);

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
                if (name.Contains("SwapViewSource"))
                {
                    // Extract the path argument
                    var pathArg = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    if (pathArg?.Expression is LiteralExpressionSyntax literal)
                    {
                        var path = literal.Token.ValueText;
                        
                        // Check for IncludeSubdirectories named argument
                        var includeSubdirs = false;
                        foreach (var arg in attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                        {
                            if (arg.NameEquals?.Name.Identifier.Text == "IncludeSubdirectories" &&
                                arg.Expression is LiteralExpressionSyntax boolLiteral)
                            {
                                includeSubdirs = boolLiteral.Token.Value is true;
                            }
                        }

                        return new ClassInfo(
                            classDeclaration,
                            path,
                            includeSubdirs,
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
            .Select(f => Path.GetFileNameWithoutExtension(f.Path))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (matchingFiles.Count == 0)
        {
            // Still generate the class but empty, with a comment
            var emptySource = GenerateEmptySource(namespaceName, className, viewsPath);
            context.AddSource($"{className}.g.cs", SourceText.From(emptySource, Encoding.UTF8));
            return;
        }

        // Separate partials (starting with _) from regular views
        var partials = matchingFiles.Where(n => n.StartsWith("_")).ToList();
        var views = matchingFiles.Where(n => !n.StartsWith("_")).ToList();

        var source = GenerateSource(namespaceName, className, views, partials);
        context.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string NormalizePath(string path)
    {
        // Normalize path separators to forward slash for comparison
        return path.Replace('\\', '/').TrimEnd('/');
    }

    private static bool PathMatchesViewsFolder(string filePath, string viewsPath, bool includeSubdirs)
    {
        var normalizedFilePath = NormalizePath(filePath);
        
        // Check if the file is in the specified views folder
        // We need to match paths like:
        // - C:/project/Views/Inventory/Index.cshtml
        // - Views/Inventory/Index.cshtml
        // Against viewsPath like: Views/Inventory

        var pathParts = viewsPath.Split('/');
        var lastParts = string.Join("/", pathParts);

        // Check if the file path contains the views path
        var idx = normalizedFilePath.IndexOf(lastParts, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;

        // If not including subdirs, ensure the file is directly in the folder
        if (!includeSubdirs)
        {
            var afterPath = normalizedFilePath.Substring(idx + lastParts.Length);
            // Should be like "/Filename.cshtml" - only one slash
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
        sb.AppendLine($"// No .cshtml files found in path: {viewsPath}");
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

    private static string GenerateSource(string namespaceName, string className, List<string> views, List<string> partials)
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

        // Generate constants for regular views
        foreach (var view in views)
        {
            var constantName = ToValidIdentifier(view);
            sb.AppendLine($"{indent}    public const string {constantName} = \"{view}\";");
        }

        // Generate nested Partials class if there are any
        if (partials.Count > 0)
        {
            if (views.Count > 0) sb.AppendLine();
            
            sb.AppendLine($"{indent}    public static class Partials");
            sb.AppendLine($"{indent}    {{");
            
            foreach (var partial in partials)
            {
                // Remove the leading underscore for the constant name
                var constantName = ToValidIdentifier(partial.TrimStart('_'));
                sb.AppendLine($"{indent}        public const string {constantName} = \"{partial}\";");
            }
            
            sb.AppendLine($"{indent}    }}");
        }

        sb.AppendLine($"{indent}}}");

        if (namespaceName != "Global")
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string ToValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";

        var sb = new StringBuilder();
        
        // First character must be letter or underscore
        var first = name[0];
        if (char.IsLetter(first))
        {
            sb.Append(char.ToUpper(first));
        }
        else if (first == '_')
        {
            sb.Append('_');
        }
        else
        {
            sb.Append('_');
            if (char.IsDigit(first)) sb.Append(first);
        }

        // Remaining characters
        for (int i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else if (c == '-' || c == '.')
            {
                // Convert kebab-case or dots to PascalCase
                if (i + 1 < name.Length && char.IsLetter(name[i + 1]))
                {
                    sb.Append(char.ToUpper(name[i + 1]));
                    i++; // Skip next char as we've processed it
                }
            }
        }

        return sb.ToString();
    }

    private class ClassInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; }
        public string ViewsPath { get; }
        public bool IncludeSubdirectories { get; }
        public string Namespace { get; }

        public ClassInfo(ClassDeclarationSyntax classDeclaration, string viewsPath, bool includeSubdirectories, string ns)
        {
            ClassDeclaration = classDeclaration;
            ViewsPath = viewsPath;
            IncludeSubdirectories = includeSubdirectories;
            Namespace = ns;
        }
    }
}
