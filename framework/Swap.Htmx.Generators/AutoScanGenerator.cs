using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Swap.Htmx.Generators;

/// <summary>
/// Source generator that automatically creates SwapViews and SwapElements classes
/// by scanning all .cshtml files in the project at compile time.
/// No attributes required - just add the package and use the generated classes.
///
/// SwapViews are grouped by controller folder for intuitive usage:
///   SwapViews.TenantsManagement.Index
///   SwapViews.TenantsManagement._TenantList
///   SwapViews.Shared._Layout
///
/// SwapElements contains all static element IDs found in views.
/// </summary>
/// <remarks>
/// The pipeline is built so that editing an unrelated .cs file does not re-run the
/// .cshtml scan: each AdditionalText is projected down to a small equatable
/// <see cref="ScannedFile"/> record (plain strings/bools, no syntax nodes or
/// Compilation), and only the assembly name (a string) is pulled from the
/// compilation provider. Unchanged files/assembly name therefore
/// produce equal values and the source output step is skipped by the driver.
/// </remarks>
[Generator]
public class AutoScanGenerator : IIncrementalGenerator
{
    // Regex to match id="value" or id='value' in HTML/Razor
    // Skips dynamic IDs like id="@Model.Id"
    private static readonly Regex IdAttributeRegex = new Regex(
        @"id\s*=\s*[""']([^""'@][^""']*)[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Project each .cshtml AdditionalText down to a small equatable record.
        // Unchanged files produce an equal ScannedFile, so the Collect() below
        // and everything downstream is skipped by the driver when nothing relevant changed.
        var scannedFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, ct) => ScanFile(file, ct))
            .WithTrackingName(TrackingNames.ScannedFiles)
            .Collect()
            .WithTrackingName(TrackingNames.CollectedFiles);

        // Only the assembly name (a plain string) is read off the Compilation, so
        // ordinary .cs edits (which produce a new Compilation instance but usually
        // the same assembly name) do not invalidate the file-scan pipeline.
        var assemblyName = context.CompilationProvider
            .Select(static (c, _) => c.AssemblyName)
            .WithTrackingName(TrackingNames.AssemblyName);

        var filesAndAssemblyName = scannedFiles.Combine(assemblyName)
            .WithTrackingName(TrackingNames.Combined);

        // Generate source
        context.RegisterSourceOutput(filesAndAssemblyName, static (spc, source) =>
        {
            var files = source.Left;
            var rootNamespace = source.Right ?? "SwapGenerated";

            if (files.IsEmpty)
                return;

            // Generate SwapViews
            GenerateSwapViews(spc, files, rootNamespace);

            // Generate SwapElements
            GenerateSwapElements(spc, files, rootNamespace);
        });
    }

    /// <summary>
    /// Tracking names for the incremental pipeline steps, exposed so tests can assert
    /// on <see cref="IncrementalGeneratorRunStep"/> cache behavior.
    /// </summary>
    internal static class TrackingNames
    {
        public const string ScannedFiles = "AutoScan_ScannedFiles";
        public const string CollectedFiles = "AutoScan_CollectedFiles";
        public const string AssemblyName = "AutoScan_AssemblyName";
        public const string Combined = "AutoScan_Combined";
    }

    private static ScannedFile ScanFile(AdditionalText file, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(file.Path);
        var viewInfo = ParseViewPath(normalizedPath);

        var text = file.GetText(cancellationToken);
        var content = text?.ToString() ?? string.Empty;
        var ids = string.IsNullOrEmpty(content)
            ? ImmutableArray<string>.Empty
            : ExtractIds(content).ToImmutableArray();

        return new ScannedFile(
            hasViewInfo: viewInfo is not null,
            controllerFolder: viewInfo?.ControllerFolder ?? string.Empty,
            fileName: viewInfo?.FileName ?? string.Empty,
            idSourceName: Path.GetFileNameWithoutExtension(file.Path),
            ids: new IdList(ids));
    }

    /// <summary>
    /// Small, value-equatable projection of a single .cshtml <see cref="AdditionalText"/>.
    /// Contains only the data needed to generate source (no syntax nodes, no Compilation),
    /// so unchanged files compare equal and don't retrigger downstream generation.
    /// </summary>
    private readonly struct ScannedFile : IEquatable<ScannedFile>
    {
        public ScannedFile(bool hasViewInfo, string controllerFolder, string fileName, string idSourceName, IdList ids)
        {
            HasViewInfo = hasViewInfo;
            ControllerFolder = controllerFolder;
            FileName = fileName;
            IdSourceName = idSourceName;
            Ids = ids;
        }

        public bool HasViewInfo { get; }
        public string ControllerFolder { get; }
        public string FileName { get; }
        public string IdSourceName { get; }
        public IdList Ids { get; }

        public bool Equals(ScannedFile other) =>
            HasViewInfo == other.HasViewInfo &&
            ControllerFolder == other.ControllerFolder &&
            FileName == other.FileName &&
            IdSourceName == other.IdSourceName &&
            Ids.Equals(other.Ids);

        public override bool Equals(object? obj) => obj is ScannedFile other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + HasViewInfo.GetHashCode();
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ControllerFolder);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(FileName);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(IdSourceName);
                hash = hash * 31 + Ids.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// Wraps an <see cref="ImmutableArray{T}"/> of ids with structural (value) equality,
    /// since <see cref="ImmutableArray{T}"/> itself compares by the identity of its
    /// backing array rather than by element content.
    /// </summary>
    private readonly struct IdList : IEquatable<IdList>
    {
        public IdList(ImmutableArray<string> ids) => Ids = ids;

        public ImmutableArray<string> Ids { get; }

        public bool Equals(IdList other) => Ids.SequenceEqual(other.Ids, StringComparer.Ordinal);

        public override bool Equals(object? obj) => obj is IdList other && Equals(other);

        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var id in Ids)
            {
                hash = unchecked(hash * 31 + StringComparer.Ordinal.GetHashCode(id));
            }

            return hash;
        }
    }

    private static void GenerateSwapViews(
        SourceProductionContext context,
        ImmutableArray<ScannedFile> files,
        string rootNamespace)
    {
        // Group views by controller folder
        // Key: controller folder name (e.g., "TenantsManagement", "Shared", "Home")
        // Value: Dictionary of view filename -> ViewInfo
        var viewsByController = new Dictionary<string, Dictionary<string, ViewInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!file.HasViewInfo)
                continue;

            var viewInfo = new ViewInfo(file.ControllerFolder, file.FileName);

            if (!viewsByController.TryGetValue(viewInfo.ControllerFolder, out var viewsDict))
            {
                viewsDict = new Dictionary<string, ViewInfo>(StringComparer.OrdinalIgnoreCase);
                viewsByController[viewInfo.ControllerFolder] = viewsDict;
            }

            // Use filename as key - duplicates within same folder are errors anyway
            if (!viewsDict.ContainsKey(viewInfo.FileName))
            {
                viewsDict[viewInfo.FileName] = viewInfo;
            }
        }

        if (viewsByController.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Swap.Htmx.Generators - do not edit");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Auto-generated view constants grouped by controller folder.");
        sb.AppendLine("    /// Usage: SwapViews.TenantsManagement._TenantList");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static class SwapViews");
        sb.AppendLine("    {");

        foreach (var kvp in viewsByController.OrderBy(k => k.Key))
        {
            var controllerFolder = kvp.Key;
            var views = kvp.Value.Values.OrderBy(v => v.FileName).ToList();
            var className = ToValidClassName(controllerFolder);

            sb.AppendLine($"        /// <summary>Views for {controllerFolder}</summary>");
            sb.AppendLine($"        public static class {className}");
            sb.AppendLine("        {");

            foreach (var view in views)
            {
                // Keep the filename as-is (including underscore for partials)
                // This prevents clashes between _TenantList and TenantList
                var constantName = ToValidIdentifier(view.FileName);
                
                // All views use short names - Razor resolves them relative to controller
                sb.AppendLine($"            public const string {constantName} = \"{view.FileName}\";");
            }

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("SwapViews.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateSwapElements(
        SourceProductionContext context,
        ImmutableArray<ScannedFile> files,
        string rootNamespace)
    {
        var allIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // id -> source file

        foreach (var file in files)
        {
            foreach (var id in file.Ids.Ids)
            {
                if (!allIds.ContainsKey(id))
                {
                    allIds[id] = file.IdSourceName;
                }
            }
        }

        if (allIds.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Swap.Htmx.Generators - do not edit");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Auto-generated element ID constants from .cshtml files.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static class SwapElements");
        sb.AppendLine("    {");

        foreach (var kvp in allIds.OrderBy(k => k.Key))
        {
            var id = kvp.Key;
            var constantName = ToValidIdentifier(id);
            sb.AppendLine($"        /// <summary>From {kvp.Value}</summary>");
            sb.AppendLine($"        public const string {constantName} = \"{id}\";");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("SwapElements.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
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
                if (string.IsNullOrEmpty(id))
                    continue;
                    
                // Skip Razor expressions
                if (id.Contains("@") || id.Contains("{") || id.Contains("}"))
                    continue;
                    
                // Skip framework-generated IDs
                if (id.StartsWith("__"))
                    continue;
                    
                // Skip numeric-only IDs (noise like "3", "4", "123")
                if (id.All(char.IsDigit))
                    continue;
                    
                // Skip single character IDs (noise)
                if (id.Length == 1)
                    continue;

                yield return id;
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static ViewInfo? ParseViewPath(string fullPath)
    {
        // Parse view paths to extract controller folder and filename
        // 
        // Standard MVC:
        //   Views/Home/Index.cshtml -> ControllerFolder="Home", FileName="Index"
        //   Views/Shared/_Layout.cshtml -> ControllerFolder="Shared", FileName="_Layout"
        //
        // Modular Monolith:
        //   Modules/SuperAdmin/Views/TenantsManagement/Index.cshtml -> ControllerFolder="TenantsManagement"
        //   Modules/SuperAdmin/Views/Shared/_Layout.cshtml -> ControllerFolder="Shared"
        //
        // Areas:
        //   Areas/Admin/Views/Home/Index.cshtml -> ControllerFolder="Admin_Home" (nested)
        //
        // Razor Pages:
        //   Pages/Account/Login.cshtml -> ControllerFolder="Account", FileName="Login"
        //   Pages/Index.cshtml -> ControllerFolder="Pages", FileName="Index"
        //
        // Components:
        //   Components/WeatherWidget/Default.cshtml -> ControllerFolder="WeatherWidget"

        var parts = fullPath.Split('/');
        var fileNameWithExt = parts[parts.Length - 1];
        var fileName = Path.GetFileNameWithoutExtension(fileNameWithExt);

        // Skip special Razor files
        if (fileName == "_ViewStart" || fileName == "_ViewImports")
            return null;

        string controllerFolder = "Root";

        // Find key folder markers
        int viewsIndex = -1;
        int pagesIndex = -1;
        int areasIndex = -1;
        int componentsIndex = -1;
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Equals("Views", StringComparison.OrdinalIgnoreCase))
                viewsIndex = i;
            else if (part.Equals("Pages", StringComparison.OrdinalIgnoreCase))
                pagesIndex = i;
            else if (part.Equals("Areas", StringComparison.OrdinalIgnoreCase))
                areasIndex = i;
            else if (part.Equals("Components", StringComparison.OrdinalIgnoreCase))
                componentsIndex = i;
        }

        // Determine controller folder based on structure
        if (viewsIndex >= 0)
        {
            // Standard Views folder structure
            // Views/{ControllerFolder}/{View}.cshtml
            // or Views/{ControllerFolder}/SubFolder/{View}.cshtml (still use ControllerFolder)
            
            if (viewsIndex + 1 < parts.Length - 1)
            {
                // Has a subfolder after Views - that's our controller folder
                controllerFolder = parts[viewsIndex + 1];
                
                // Check for Areas pattern: Areas/{Area}/Views/{Controller}/
                if (areasIndex >= 0 && areasIndex < viewsIndex && areasIndex + 1 < viewsIndex)
                {
                    var areaName = parts[areasIndex + 1];
                    controllerFolder = $"{areaName}_{controllerFolder}";
                }
            }
            else
            {
                // File directly in Views folder (unlikely but handle it)
                controllerFolder = "Root";
            }
        }
        else if (pagesIndex >= 0)
        {
            // Razor Pages structure
            // Pages/{Folder}/{Page}.cshtml -> Folder
            // Pages/{Page}.cshtml -> Pages
            
            if (pagesIndex + 1 < parts.Length - 1)
            {
                controllerFolder = parts[pagesIndex + 1];
            }
            else
            {
                controllerFolder = "Pages";
            }
        }
        else if (componentsIndex >= 0)
        {
            // View Components
            // Components/{ComponentName}/{View}.cshtml
            
            if (componentsIndex + 1 < parts.Length - 1)
            {
                controllerFolder = parts[componentsIndex + 1];
            }
            else
            {
                controllerFolder = "Components";
            }
        }
        else
        {
            // Fallback: use parent folder
            if (parts.Length >= 2)
            {
                controllerFolder = parts[parts.Length - 2];
            }
        }

        return new ViewInfo(controllerFolder, fileName);
    }

    private static string ToValidClassName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Root";

        var sb = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in name)
        {
            if (c == '-' || c == '_' || c == '.')
            {
                capitalizeNext = true;
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }
        }

        var result = sb.ToString();
        
        // Ensure it starts with a letter
        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return string.IsNullOrEmpty(result) ? "Root" : result;
    }

    private static string ToValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";

        // Handle underscore prefix for partials - keep it!
        var startsWithUnderscore = name.StartsWith("_");
        
        var sb = new StringBuilder();
        var capitalizeNext = !startsWithUnderscore; // Don't capitalize first char if starts with _

        foreach (var c in name)
        {
            if (c == '-' || c == '.')
            {
                capitalizeNext = true;
                continue;
            }

            if (c == '_')
            {
                // Keep underscores as-is (important for partial naming)
                sb.Append('_');
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

    private class ViewInfo
    {
        public string ControllerFolder { get; }
        public string FileName { get; }

        public ViewInfo(string controllerFolder, string fileName)
        {
            ControllerFolder = controllerFolder;
            FileName = fileName;
        }
    }
}
