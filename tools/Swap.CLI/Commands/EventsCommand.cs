using System.CommandLine;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class EventsCommand
{
    public static Command Create()
    {
        var cmd = new Command("events", "Inspect Swap event chains in your project");

        var list = new Command("list", "List configured event chains (source scan)");
        var projectOption = new Option<string?>(new[] {"--project", "-p"}, () => Directory.GetCurrentDirectory(), "Path to project directory");
        list.AddOption(projectOption);
        list.SetHandler(async (string? project) => await ListChains(project!), projectOption);

        var urlOption = new Option<string?>(new[] {"--url", "-u"}, description: "Base URL of a running app to query dev endpoint (http://localhost:5000)");
        var fromServer = new Command("from-server", "Fetch chains from a running app's dev endpoint (/_swap/dev/events.json)");
        fromServer.AddOption(urlOption);
        fromServer.SetHandler(async (string? url) => await FetchFromServer(url), urlOption);

        cmd.AddCommand(list);
        cmd.AddCommand(fromServer);
        return cmd;
    }

    private static async Task ListChains(string projectDir)
    {
        var path = Path.Combine(projectDir, "Events", "SwapEventChains.cs");
        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not find [bold]Events/SwapEventChains.cs[/] in {projectDir}");
            return;
        }
        var content = await File.ReadAllTextAsync(path);

        // Optional: load EventNames constants to resolve identifiers
        var namesMap = await LoadEventNamesAsync(projectDir);

        // Match full Chain(...) invocation and parse args (quoted or identifiers)
        var callPattern = new Regex(@"\.Chain\(([^)]*)\)", RegexOptions.Multiline);
        var edges = new List<(string trigger, string chained)>();
        foreach (Match m in callPattern.Matches(content))
        {
            var argsText = m.Groups[1].Value;
            var tokens = TokenizeArgs(argsText);
            if (tokens.Count == 0) continue;

            var trigger = ResolveToken(tokens[0], namesMap);
            foreach (var c in tokens.Skip(1))
            {
                var target = ResolveToken(c, namesMap);
                if (!string.IsNullOrWhiteSpace(trigger) && !string.IsNullOrWhiteSpace(target))
                    edges.Add((trigger, target));
            }
        }

        if (edges.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No chains found. Ensure you call Chain(...) in Events/SwapEventChains.cs[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Trigger");
        table.AddColumn("Chained");
        foreach (var g in edges.GroupBy(e => e.trigger).OrderBy(g => g.Key))
        {
            table.AddRow($"[cyan]{g.Key}[/]", string.Join("\n", g.Select(x => x.chained)));
        }
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]Found {edges.Count} edge(s) across {edges.Select(e => e.trigger).Distinct().Count()} trigger(s)[/]");
    }

    private static async Task<Dictionary<string, string>> LoadEventNamesAsync(string projectDir)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var file = Path.Combine(projectDir, "Events", "EventNames.cs");
        if (!File.Exists(file)) return map;
        var lines = await File.ReadAllLinesAsync(file);
        string? currentInner = null;
        var insideEventNames = false;
        var classRegex = new Regex(@"public\s+static\s+class\s+(\w+)");
    var constRegex = new Regex(@"public\s+const\s+string\s+(\w+)\s*=\s*""([^""]+)""\s*;");
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            var mClass = classRegex.Match(line);
            if (mClass.Success)
            {
                var name = mClass.Groups[1].Value;
                if (name == "EventNames") { insideEventNames = true; currentInner = null; continue; }
                if (insideEventNames)
                {
                    currentInner = name; // e.g., Domain, Ui
                }
                continue;
            }
            var mConst = constRegex.Match(line);
            if (insideEventNames && mConst.Success && currentInner is not null)
            {
                var constName = mConst.Groups[1].Value;
                var value = mConst.Groups[2].Value;
                var key = $"EventNames.{currentInner}.{constName}";
                map[key] = value;
            }
        }
        return map;
    }

    private static List<string> TokenizeArgs(string args)
    {
        var list = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < args.Length; i++)
        {
            var ch = args[i];
            if (ch == '"') { inQuotes = !inQuotes; sb.Append(ch); continue; }
            if (!inQuotes && ch == ',') { var t = sb.ToString().Trim(); if (t.Length > 0) list.Add(t); sb.Clear(); continue; }
            sb.Append(ch);
        }
        var last = sb.ToString().Trim(); if (last.Length > 0) list.Add(last);
        return list;
    }

    private static string ResolveToken(string token, Dictionary<string, string> namesMap)
    {
        token = token.Trim();
        if (token.StartsWith("\"") && token.EndsWith("\""))
        {
            return token.Trim('"');
        }
        if (token.StartsWith("EventNames."))
        {
            if (namesMap.TryGetValue(token, out var value)) return value;
        }
        return token; // fallback (identifier)
    }

    private static async Task FetchFromServer(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Provide --url http://localhost:5000 (or your app URL)");
            return;
        }
        var url = baseUrl.TrimEnd('/') + "/_swap/dev/events.json";
        try
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync(url);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(json) ?? new();

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Trigger");
            table.AddColumn("Chained");
            foreach (var kv in dict.OrderBy(k => k.Key))
            {
                table.AddRow($"[cyan]{kv.Key}[/]", string.Join("\n", kv.Value));
            }
            AnsiConsole.Write(table);
            var edgeCount = dict.Sum(k => k.Value.Length);
            AnsiConsole.MarkupLine($"[dim]Fetched {edgeCount} edge(s) across {dict.Count} trigger(s) from {url}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}
