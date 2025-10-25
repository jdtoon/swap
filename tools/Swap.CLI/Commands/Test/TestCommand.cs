using System.CommandLine;

namespace Swap.CLI.Commands.Test;

/// <summary>
/// Base command for testing operations.
/// Provides subcommands for feature, module, and E2E testing.
/// </summary>
public static class TestCommand
{
    public static Command Create()
    {
        var testCommand = new Command("test", "Run tests for features, modules, or E2E scenarios");

        // Add subcommands
        testCommand.Subcommands.Add(FeatureTestCommand.Create());
        testCommand.Subcommands.Add(ModuleTestCommand.Create());
        testCommand.Subcommands.Add(E2ETestCommand.Create());

        return testCommand;
    }
}

