using System.CommandLine;
using System.Diagnostics;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DoctorCommand
{
    public static Command Create()
    {
        var command = new Command("doctor", "Check development environment and dependencies");
        
        command.SetHandler(async () =>
        {
            await ExecuteAsync();
        });
        
        return command;
    }
    
    private static async Task ExecuteAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]Checking development environment...[/]");
        AnsiConsole.WriteLine();
        
        var checks = new List<(string Name, bool IsRequired, Func<Task<(bool Success, string Version, string? Message)>> Check)>
        {
            (".NET SDK", true, CheckDotNetAsync),
            ("dotnet-ef", true, CheckDotNetEfAsync),
            ("Node.js", false, CheckNodeAsync),
            ("npm", false, CheckNpmAsync),
            ("libman", false, CheckLibmanAsync)
        };
        
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Tool[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]Version[/]");
        table.AddColumn("[bold]Notes[/]");
        
        int passCount = 0;
        int failCount = 0;
        int warnCount = 0;
        
        foreach (var (name, isRequired, check) in checks)
        {
            var (success, version, message) = await check();
            
            string status;
            string notes = message ?? "";
            
            if (success)
            {
                status = "[green]✓ Installed[/]";
                passCount++;
            }
            else if (isRequired)
            {
                status = "[red]✗ Missing[/]";
                failCount++;
            }
            else
            {
                status = "[yellow]⚠ Missing[/]";
                warnCount++;
                notes = notes == "" ? "Optional" : $"Optional - {notes}";
            }
            
            table.AddRow(
                name,
                status,
                version,
                notes
            );
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        
        // Summary
        if (failCount > 0)
        {
            AnsiConsole.MarkupLine($"[red]✗ {failCount} required tool(s) missing[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Installation instructions:[/]");
            AnsiConsole.MarkupLine("  dotnet-ef: dotnet tool install --global dotnet-ef");
        }
        else if (warnCount > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ {warnCount} optional tool(s) missing[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Optional installations:[/]");
            AnsiConsole.MarkupLine("  Node.js: https://nodejs.org/");
            AnsiConsole.MarkupLine("  libman: dotnet tool install --global Microsoft.Web.LibraryManager.Cli");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]✓ All checks passed! Environment ready.[/]");
        }
    }
    
    private static async Task<(bool Success, string Version, string? Message)> CheckDotNetAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                var version = output.Trim();
                return (true, version, null);
            }
            
            return (false, "N/A", "dotnet command failed");
        }
        catch
        {
            return (false, "N/A", "dotnet not found in PATH");
        }
    }
    
    private static async Task<(bool Success, string Version, string? Message)> CheckDotNetEfAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "ef --version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0 && output.Contains("Entity Framework Core"))
            {
                // Extract version from output like:
                // "Entity Framework Core .NET Command-line Tools
                //  9.0.10"
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2)
                {
                    var version = lines[1].Trim();
                    if (!string.IsNullOrEmpty(version))
                    {
                        return (true, version, null);
                    }
                }
                return (true, "installed", null);
            }
            
            return (false, "N/A", "Run: dotnet tool install --global dotnet-ef");
        }
        catch
        {
            return (false, "N/A", "Run: dotnet tool install --global dotnet-ef");
        }
    }
    
    private static async Task<(bool Success, string Version, string? Message)> CheckNodeAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                var version = output.Trim().TrimStart('v');
                return (true, version, "Needed for Tailwind CSS");
            }
            
            return (false, "N/A", "Needed for Tailwind CSS");
        }
        catch
        {
            return (false, "N/A", "Needed for Tailwind CSS");
        }
    }
    
    private static async Task<(bool Success, string Version, string? Message)> CheckNpmAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                var version = output.Trim();
                return (true, version, "Needed for npm packages");
            }
            
            return (false, "N/A", "Comes with Node.js");
        }
        catch
        {
            return (false, "N/A", "Comes with Node.js");
        }
    }
    
    private static async Task<(bool Success, string Version, string? Message)> CheckLibmanAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "libman",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                var version = output.Trim();
                return (true, version, "Needed for client libraries");
            }
            
            return (false, "N/A", "Needed for client libraries");
        }
        catch
        {
            return (false, "N/A", "Needed for client libraries");
        }
    }
}
