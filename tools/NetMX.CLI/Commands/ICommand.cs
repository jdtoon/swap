using System.CommandLine;

namespace NetMX.CLI.Commands;

/// <summary>
/// Interface for all NetMX CLI commands
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the command definition
    /// </summary>
    Command GetCommand();
    
    /// <summary>
    /// Executes the command
    /// </summary>
    Task<int> ExecuteAsync();
}
