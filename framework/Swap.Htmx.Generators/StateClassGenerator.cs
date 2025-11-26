using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Swap.Htmx.Generators;

/// <summary>
/// Source generator that creates SwapState properties from view annotations.
/// Scans .cshtml files for swap-state-prop attributes and generates properties.
/// </summary>
/// <remarks>
/// Format: swap-state-prop="PropertyName:Type=DefaultValue"
/// 
/// Examples:
/// - swap-state-prop="Tab:string=all" → public string Tab { get; set; } = "all";
/// - swap-state-prop="Page:int=1" → public int Page { get; set; } = 1;
/// - swap-state-prop="Search:string?" → public string? Search { get; set; }
/// - swap-state-prop="IsActive:bool=true" → public bool IsActive { get; set; } = true;
/// </remarks>
[Generator]
public class StateClassGenerator : IIncrementalGenerator
{
    // Regex to match swap-state-prop="PropertyName:Type=DefaultValue"
    // Groups: 1=PropertyName, 2=Type, 3=DefaultValue (optional, includes =)
    private static readonly Regex StatePropertyRegex = new Regex(
        @"swap-state-prop\s*=\s*[""']([^:""']+):([^=""']+)(=[^""']+)?[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find classes with [SwapStateSource] attribute
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

    private static StateClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name.Contains("SwapStateSource"))
                {
                    // Extract the path argument
                    var pathArg = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    if (pathArg?.Expression is LiteralExpressionSyntax literal)
                    {
                        var path = literal.Token.ValueText;
                        return new StateClassInfo(
                            classDeclaration,
                            path,
                            GetNamespace(classDeclaration));
                    }
                }
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        StateClassInfo classInfo,
        IEnumerable<AdditionalText> cshtmlFiles)
    {
        // Find the matching .cshtml file
        var matchingFile = cshtmlFiles.FirstOrDefault(f =>
            NormalizePath(f.Path).EndsWith(NormalizePath(classInfo.ViewPath), StringComparison.OrdinalIgnoreCase));

        if (matchingFile == null)
            return;

        var sourceText = matchingFile.GetText();
        if (sourceText == null)
            return;

        var content = sourceText.ToString();
        var properties = ExtractStateProperties(content);

        if (!properties.Any())
            return;

        var source = GenerateSource(classInfo, properties);
        var fileName = $"{classInfo.ClassName}.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private static List<StateProperty> ExtractStateProperties(string content)
    {
        var properties = new List<StateProperty>();
        var matches = StatePropertyRegex.Matches(content);

        foreach (Match match in matches)
        {
            var propertyName = match.Groups[1].Value.Trim();
            var typeName = match.Groups[2].Value.Trim();
            var defaultValue = match.Groups[3].Success 
                ? match.Groups[3].Value.TrimStart('=').Trim() 
                : null;

            // Parse nullable types
            var isNullable = typeName.EndsWith("?");
            var baseType = isNullable ? typeName.TrimEnd('?') : typeName;

            // Map common type aliases
            var mappedType = MapTypeAlias(baseType);
            var fullType = isNullable ? $"{mappedType}?" : mappedType;

            properties.Add(new StateProperty(propertyName, fullType, defaultValue, isNullable));
        }

        return properties;
    }

    private static string MapTypeAlias(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "string" => "string",
            "int" => "int",
            "long" => "long",
            "short" => "short",
            "byte" => "byte",
            "float" => "float",
            "double" => "double",
            "decimal" => "decimal",
            "bool" => "bool",
            "boolean" => "bool",
            "datetime" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "guid" => "Guid",
            _ => typeName
        };
    }

    private static string GenerateSource(StateClassInfo classInfo, List<StateProperty> properties)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(classInfo.Namespace))
        {
            sb.AppendLine($"namespace {classInfo.Namespace}");
            sb.AppendLine("{");
        }

        var indent = string.IsNullOrEmpty(classInfo.Namespace) ? "" : "    ";

        sb.AppendLine($"{indent}public partial class {classInfo.ClassName}");
        sb.AppendLine($"{indent}{{");

        foreach (var prop in properties)
        {
            var defaultPart = GetDefaultValueAssignment(prop);
            sb.AppendLine($"{indent}    public {prop.TypeName} {prop.Name} {{ get; set; }}{defaultPart}");
        }

        sb.AppendLine($"{indent}}}");

        if (!string.IsNullOrEmpty(classInfo.Namespace))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string GetDefaultValueAssignment(StateProperty prop)
    {
        if (prop.DefaultValue == null)
            return ";";

        // Format default value based on type
        var formattedValue = FormatDefaultValue(prop.TypeName, prop.DefaultValue);
        return $" = {formattedValue};";
    }

    private static string FormatDefaultValue(string typeName, string defaultValue)
    {
        var baseType = typeName.TrimEnd('?').ToLowerInvariant();
        
        return baseType switch
        {
            "string" => $"\"{EscapeString(defaultValue)}\"",
            "bool" => defaultValue.ToLowerInvariant(),
            "char" => $"'{defaultValue}'",
            "int" or "long" or "short" or "byte" or "float" or "double" or "decimal" => defaultValue,
            _ when baseType.Contains("datetime") => $"default",
            _ when baseType.Contains("guid") => defaultValue.ToLowerInvariant() == "empty" 
                ? "Guid.Empty" 
                : $"Guid.Parse(\"{defaultValue}\")",
            _ => defaultValue
        };
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string? GetNamespace(ClassDeclarationSyntax classDeclaration)
    {
        var current = classDeclaration.Parent;
        while (current != null)
        {
            if (current is FileScopedNamespaceDeclarationSyntax fileScopedNs)
            {
                return fileScopedNs.Name.ToString();
            }
            if (current is NamespaceDeclarationSyntax ns)
            {
                return ns.Name.ToString();
            }
            current = current.Parent;
        }
        return null;
    }

    private sealed class StateClassInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; }
        public string ViewPath { get; }
        public string Namespace { get; }
        public string ClassName => ClassDeclaration.Identifier.Text;

        public StateClassInfo(ClassDeclarationSyntax classDeclaration, string viewPath, string ns)
        {
            ClassDeclaration = classDeclaration;
            ViewPath = viewPath;
            Namespace = ns;
        }
    }

    private sealed class StateProperty
    {
        public string Name { get; }
        public string TypeName { get; }
        public string DefaultValue { get; }
        public bool IsNullable { get; }

        public StateProperty(string name, string typeName, string defaultValue, bool isNullable)
        {
            Name = name;
            TypeName = typeName;
            DefaultValue = defaultValue;
            IsNullable = isNullable;
        }
    }
}
