using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for OOB target ID validation in SwapResponseBuilder.
/// </summary>
public class SwapResponseBuilderTargetIdValidationTests
{
    [Theory]
    [InlineData("sidebar")]
    [InlineData("main-content")]
    [InlineData("item_list")]
    [InlineData("A")]
    [InlineData("z")]
    [InlineData("cart-count-123")]
    [InlineData("myElement")]
    public void AlsoUpdate_AcceptsValidTargetIds(string targetId)
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main")
            .AlsoUpdate(targetId, "_Partial");

        Assert.Single(builder.OobSwaps);
        Assert.Equal(targetId, builder.OobSwaps[0].TargetId);
    }

    [Theory]
    [InlineData("#sidebar", "sidebar")]
    [InlineData("#main-content", "main-content")]
    [InlineData("# item", "item")]
    [InlineData("  #sidebar  ", "sidebar")]
    public void AlsoUpdate_NormalizesHashPrefix(string input, string expected)
    {
        var builder = new SwapResponseBuilder()
            .WithView("_Main")
            .AlsoUpdate(input, "_Partial");

        Assert.Single(builder.OobSwaps);
        Assert.Equal(expected, builder.OobSwaps[0].TargetId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AlsoUpdate_RejectsEmptyTargetId(string targetId)
    {
        var builder = new SwapResponseBuilder().WithView("_Main");
        Assert.Throws<ArgumentException>(() => builder.AlsoUpdate(targetId, "_Partial"));
    }

    [Theory]
    [InlineData("123-item")]
    [InlineData("1abc")]
    [InlineData("-leading-dash")]
    [InlineData("_leading-underscore")]
    public void AlsoUpdate_RejectsIdsNotStartingWithLetter(string targetId)
    {
        var builder = new SwapResponseBuilder().WithView("_Main");
        Assert.Throws<ArgumentException>(() => builder.AlsoUpdate(targetId, "_Partial"));
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("has.dot")]
    [InlineData("has<angle>")]
    [InlineData("has\"quote")]
    [InlineData("javascript:alert(1)")]
    [InlineData("on\nclick")]
    public void AlsoUpdate_RejectsIdsWithInvalidCharacters(string targetId)
    {
        var builder = new SwapResponseBuilder().WithView("_Main");
        Assert.Throws<ArgumentException>(() => builder.AlsoUpdate(targetId, "_Partial"));
    }

    [Fact]
    public void AlsoUpdateIfExists_ValidatesTargetId()
    {
        var builder = new SwapResponseBuilder().WithView("_Main");
        Assert.Throws<ArgumentException>(() => builder.AlsoUpdateIfExists("123bad", "_Partial"));
    }

    [Fact]
    public void AlsoUpdateIf_ValidatesTargetId_WhenConditionTrue()
    {
        var builder = new SwapResponseBuilder().WithView("_Main");
        Assert.Throws<ArgumentException>(() => builder.AlsoUpdateIf(true, "123bad", "_Partial"));
    }

    [Fact]
    public void AlsoUpdateIf_SkipsValidation_WhenConditionFalse()
    {
        // When condition is false, the swap is never added, so no validation occurs
        var builder = new SwapResponseBuilder()
            .WithView("_Main")
            .AlsoUpdateIf(false, "123bad", "_Partial");

        Assert.Empty(builder.OobSwaps);
    }

    [Fact]
    public void AlsoUpdateMany_ValidatesTargetIds()
    {
        var items = new[] { "item1", "item2" };
        var builder = new SwapResponseBuilder().WithView("_Main");

        Assert.Throws<ArgumentException>(() =>
            builder.AlsoUpdateMany(items, item => $"123-{item}", "_ItemPartial"));
    }

    [Fact]
    public void AlsoUpdateMany_AcceptsValidTargetIds()
    {
        var items = new[] { "first", "second" };
        var builder = new SwapResponseBuilder()
            .WithView("_Main")
            .AlsoUpdateMany(items, item => $"item-{item}", "_ItemPartial");

        Assert.Equal(2, builder.OobSwaps.Count);
        Assert.Equal("item-first", builder.OobSwaps[0].TargetId);
        Assert.Equal("item-second", builder.OobSwaps[1].TargetId);
    }
}
