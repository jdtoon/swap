using System.CommandLine;
using Swap.CLI.Commands;
using Xunit;

namespace Swap.CLI.Tests.Commands;

public class GenerateSeedCommandTests
{
    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        var cmd = GenerateSeedCommand.Create();
        Assert.Equal("seed", cmd.Name);
    }

    [Fact]
    public void Create_ShouldHaveNameArgument()
    {
        var cmd = GenerateSeedCommand.Create();
        var nameArg = cmd.Arguments.FirstOrDefault(a => a.Name == "name");
        Assert.NotNull(nameArg);
    }

    [Fact]
    public void Create_ShouldHaveOptions()
    {
        var cmd = GenerateSeedCommand.Create();
        Assert.NotNull(cmd.Options.FirstOrDefault(o => o.Name == "count"));
        Assert.NotNull(cmd.Options.FirstOrDefault(o => o.Name == "locale"));
        Assert.NotNull(cmd.Options.FirstOrDefault(o => o.Name == "if-empty"));
        Assert.NotNull(cmd.Options.FirstOrDefault(o => o.Name == "append"));
    }

    [Theory]
    [InlineData("Post")]
    [InlineData("User")]
    [InlineData("Order")]    
    public void Parse_ShouldAcceptEntityName(string entity)
    {
        var cmd = GenerateSeedCommand.Create();
        var result = cmd.Parse(new[] { entity });
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_ShouldAcceptAllKeyword()
    {
        var cmd = GenerateSeedCommand.Create();
        var result = cmd.Parse(new[] { "all" });
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_ShouldAcceptOptions()
    {
        var cmd = GenerateSeedCommand.Create();
        var result = cmd.Parse(new[] { "Post", "--count", "100", "--locale", "en_AU", "--if-empty" });
        Assert.Empty(result.Errors);
    }
}
