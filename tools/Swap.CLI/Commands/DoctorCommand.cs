using System.CommandLine;
using System.Diagnostics;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DoctorCommand
{
    public static Command Create()
    {
        var command = new Command("doctor", "Check development environment and dependencies");
        
        var fixOption = new Option<bool>(
            aliases: new[] { "--fix", "-f" },
            description: "Attempt to install missing tools automatically");
        
        command.AddOption(fixOption);
        
        command.SetHandler(async (bool fix) =>
        {
            await ExecuteAsync(fix);
        }, fixOption);
        
        return command;
    }
    
    private static async Task ExecuteAsync(bool fix = false)
    {
        AnsiConsole.MarkupLine("[bold cyan]Checking development environment...[/]");
        AnsiConsole.WriteLine();
        
        var checks = new List<(string Name, bool IsRequired, Func<Task<(bool Success, string Version, string? Message)>> Check, Func<Task<bool>>? Install)>
        {
            (".NET SDK", true, CheckDotNetAsync, null), // Cannot auto-install .NET SDK
            ("dotnet-ef", true, CheckDotNetEfAsync, InstallDotNetEfAsync),
            ("Node.js", false, CheckNodeAsync, null), // Cannot auto-install Node.js (needs installer)
            ("npm", false, CheckNpmAsync, null), // Comes with Node.js
            ("libman", false, CheckLibmanAsync, InstallLibmanAsync)
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
        var missingTools = new List<(string Name, bool IsRequired, Func<Task<bool>>? Install)>();
        
        foreach (var (name, isRequired, check, install) in checks)
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
                missingTools.Add((name, isRequired, install));
            }
            else
            {
                status = "[yellow]⚠ Missing[/]";
                warnCount++;
                notes = notes == "" ? "Optional" : $"Optional - {notes}";
                missingTools.Add((name, isRequired, install));
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
            
            if (fix)
            {
                await InstallMissingToolsAsync(missingTools.Where(t => t.IsRequired).ToList());
            }
            else
            {
                AnsiConsole.MarkupLine("[bold]Installation instructions:[/]");
                AnsiConsole.MarkupLine("  dotnet-ef: [cyan]dotnet tool install --global dotnet-ef[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Tip: Run[/] [cyan]swap doctor --fix[/] [dim]to install missing tools automatically[/]");
            }
        }
        else if (warnCount > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ {warnCount} optional tool(s) missing[/]");
            AnsiConsole.WriteLine();
            
            if (fix)
            {
                if (AnsiConsole.Confirm("Install optional tools?", false))
                {
                    await InstallMissingToolsAsync(missingTools.Where(t => !t.IsRequired).ToList());
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[bold]Optional installations:[/]");
                AnsiConsole.MarkupLine("  Node.js: [cyan]https://nodejs.org/[/]");
                AnsiConsole.MarkupLine("  libman: [cyan]dotnet tool install --global Microsoft.Web.LibraryManager.Cli[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Tip: Run[/] [cyan]swap doctor --fix[/] [dim]to install optional tools interactively[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]✓ All checks passed! Environment ready.[/]");
        }
    }
    
    private static async Task InstallMissingToolsAsync(List<(string Name, bool IsRequired, Func<Task<bool>>? Install)> tools)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]Installing missing tools...[/]");
        AnsiConsole.WriteLine();
        
        foreach (var (name, _, install) in tools)
        {
            if (install == null)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] {name} cannot be auto-installed. Please install manually.");
                continue;
            }
            
            AnsiConsole.MarkupLine($"[cyan]Installing {name}...[/]");
            var success = await install();
            
            if (success)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] {name} installed successfully!");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to install {name}");
            }
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Run[/] [cyan]swap doctor[/] [dim]again to verify installation[/]");
    }
    
    private static async Task<bool> InstallDotNetEfAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "tool install --global dotnet-ef",
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    private static async Task<bool> InstallLibmanAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "tool install --global Microsoft.Web.LibraryManager.Cli",
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
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
