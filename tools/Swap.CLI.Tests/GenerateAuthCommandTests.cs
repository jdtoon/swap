using System.IO;
using Swap.CLI.Commands;
using Xunit;

namespace Swap.CLI.Tests;

public class GenerateAuthCommandTests
{
    [Fact]
    public void AuthCommand_Should_HaveCorrectNameAndAlias()
    {
        var command = GenerateAuthCommand.Create();
        
        Assert.Equal("auth", command.Name);
        Assert.Contains("a", command.Aliases);
    }
    
    [Fact]
    public void AuthCommand_Should_HaveDryRunOption()
    {
        var command = GenerateAuthCommand.Create();
        
        var dryRunOption = command.Options.FirstOrDefault(o => o.Name == "dry-run");
        Assert.NotNull(dryRunOption);
    }
    
    [Fact]
    public void AuthCommand_Should_HaveForceOption()
    {
        var command = GenerateAuthCommand.Create();
        
        var forceOption = command.Options.FirstOrDefault(o => o.Name == "force");
        Assert.NotNull(forceOption);
    }
    
    [Fact]
    public void AuthCommand_Should_HaveProjectOption()
    {
        var command = GenerateAuthCommand.Create();
        
        var projectOption = command.Options.FirstOrDefault(o => o.Name == "project");
        Assert.NotNull(projectOption);
        Assert.Contains("-p", projectOption.Aliases);
    }
}
