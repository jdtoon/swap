using System.CommandLine;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class GenerateHtmxShellCommand
{
    public static Command Create()
    {
        var command = new Command("htmx-shell", "Add HTMX shell middleware (allowlist redirect for non-HTMX GET) and optionally enable global hx-boost");
        command.AddAlias("hx");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");
        command.AddOption(projectOption);

        var addBoostOption = new Option<bool>(
            aliases: new[] { "--add-boost" },
            description: "Inject hx-boost=\"true\" into _Layout.cshtml <body> tag"
        );
        command.AddOption(addBoostOption);

        command.SetHandler(async (context) =>
        {
            var workingDir = context.ParseResult.GetValueForOption(projectOption);
            var addBoost = context.ParseResult.GetValueForOption(addBoostOption);
            var dir = !string.IsNullOrEmpty(workingDir) ? Path.GetFullPath(workingDir) : Directory.GetCurrentDirectory();

            var projectFiles = Directory.GetFiles(dir, "*.csproj");
            if (projectFiles.Length == 0)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {dir}");
                context.ExitCode = 1;
                return;
            }
            var projectFile = projectFiles[0];
            var projectName = Path.GetFileNameWithoutExtension(projectFile);

            await AddMiddlewareAsync(dir, projectName);
            await WireProgramAsync(dir, projectName);

            if (addBoost)
            {
                await InjectHxBoostAsync(dir);
            }

            AnsiConsole.MarkupLine("[green]✓[/] HTMX shell middleware added and wired");
            if (addBoost) AnsiConsole.MarkupLine("[green]✓[/] Global hx-boost enabled in _Layout.cshtml");
            context.ExitCode = 0;
        });

        return command;
    }

    private static async Task AddMiddlewareAsync(string workingDir, string projectName)
    {
        var middlewareDir = Path.Combine(workingDir, "Middleware");
        Directory.CreateDirectory(middlewareDir);
        var filePath = Path.Combine(middlewareDir, "HtmxShellMiddleware.cs");

        if (!File.Exists(filePath))
        {
            var code = $@"namespace {projectName}.Middleware;

public class HtmxShellMiddleware
{{
    private readonly RequestDelegate _next;

    // Adjust allowlist for controllers that render HTMX partial routes
    private static readonly HashSet<string> Allowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {{
        // e.g., ""/Posts"", ""/Categories""
    }};

    public HtmxShellMiddleware(RequestDelegate next)
    {{
        _next = next;
    }}

    public async Task InvokeAsync(HttpContext context)
    {{
        var isGet = HttpMethods.IsGet(context.Request.Method);
    var isHtmx = context.Request.Headers.ContainsKey(""HX-Request"");
        var path = context.Request.Path.Value ?? string.Empty;

        if (isGet && !isHtmx)
        {{
            // If a request targets an allowlisted resource subroute (e.g., /Posts/List),
            // redirect to the base route (e.g., /Posts) which should render the shell.
            foreach (var baseRoute in Allowlist)
            {{
                if (path.StartsWith(baseRoute + ""/"", StringComparison.OrdinalIgnoreCase))
                {{
                    context.Response.Redirect(baseRoute);
                    return;
                }}
            }}
        }}

        await _next(context);
    }}
}}
";
            await File.WriteAllTextAsync(filePath, code);
        }
    }

    private static async Task WireProgramAsync(string workingDir, string projectName)
    {
        var programPath = Path.Combine(workingDir, "Program.cs");
        if (!File.Exists(programPath))
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Program.cs not found; skipping middleware wiring");
            return;
        }

        var content = await File.ReadAllTextAsync(programPath);

        // Ensure using
        var usingLine = $"using {projectName}.Middleware;";
        if (!content.Contains(usingLine))
        {
            var firstUsingEnd = content.IndexOf(";\n");
            if (firstUsingEnd > 0)
            {
                content = content.Insert(firstUsingEnd + 2, usingLine + "\n");
            }
            else
            {
                content = usingLine + "\n" + content;
            }
        }

        // Ensure app.UseMiddleware<HtmxShellMiddleware>(); appears after builder.Build()
        var buildIdx = content.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        if (buildIdx >= 0)
        {
            var insertPos = content.IndexOf('\n', buildIdx);
            if (insertPos > 0)
            {
                if (!content.Contains("UseMiddleware<HtmxShellMiddleware>"))
                {
                    content = content.Insert(insertPos + 1, "app.UseMiddleware<HtmxShellMiddleware>();\n");
                }
            }
        }

        await File.WriteAllTextAsync(programPath, content);
    }

    private static async Task InjectHxBoostAsync(string workingDir)
    {
        try
        {
            var layoutPath = Path.Combine(workingDir, "Views", "Shared", "_Layout.cshtml");
            if (!File.Exists(layoutPath)) return;

            var content = await File.ReadAllTextAsync(layoutPath);
            var bodyIdx = content.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
            if (bodyIdx >= 0)
            {
                var bodyEnd = content.IndexOf('>', bodyIdx);
                if (bodyEnd > bodyIdx)
                {
                    var bodyTag = content.Substring(bodyIdx, bodyEnd - bodyIdx + 1);
                    if (!bodyTag.Contains("hx-boost"))
                    {
                        var boosted = bodyTag.Insert(bodyTag.Length - 1, " hx-boost=\"true\"");
                        content = content.Remove(bodyIdx, bodyTag.Length).Insert(bodyIdx, boosted);
                        await File.WriteAllTextAsync(layoutPath, content);
                    }
                }
            }
        }
        catch { }
    }
}
