using Microsoft.AspNetCore.Razor.TagHelpers;
using Swap.Htmx.TagHelpers;
using System.Text.Encodings.Web;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for the SwapHiddenTagHelper.
/// </summary>
public class SwapHiddenTagHelperTests
{
    private static SwapHiddenTagHelper CreateTagHelper()
    {
        return new SwapHiddenTagHelper(HtmlEncoder.Default);
    }

    private static (TagHelperContext, TagHelperOutput) CreateTagHelperContext()
    {
        var context = new TagHelperContext(
            tagName: "swap-hidden",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "swap-hidden",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        return (context, output);
    }

    #region Basic Rendering Tests

    [Fact]
    public void Process_StringValue_RendersHiddenInput()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "search";
        helper.Value = "test query";
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("input", output.TagName);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal("hidden", output.Attributes["type"].Value);
        Assert.Equal("search", output.Attributes["name"].Value);
        Assert.Equal("test query", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_IntValue_RendersCorrectly()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "page";
        helper.Value = 42;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("42", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_EmptyName_SuppressesOutput()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "";
        helper.Value = "test";
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.True(output.IsContentModified);
    }

    #endregion

    #region Boolean Formatting Tests

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Process_BooleanValue_FormatsCorrectly(bool input, string expected)
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "isActive";
        helper.Value = input;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal(expected, output.Attributes["value"].Value);
    }

    #endregion

    #region Date Formatting Tests

    [Fact]
    public void Process_DateTime_FormatsWithDefaultFormat()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "startDate";
        helper.Value = new DateTime(2025, 1, 15);
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("2025-01-15", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_DateOnly_FormatsCorrectly()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "birthDate";
        helper.Value = new DateOnly(1990, 6, 15);
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("1990-06-15", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_DateTime_CustomFormat_UsesCustomFormat()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "timestamp";
        helper.Value = new DateTime(2025, 1, 15, 14, 30, 0);
        helper.DateFormat = "yyyy-MM-ddTHH:mm:ss";
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("2025-01-15T14:30:00", output.Attributes["value"].Value);
    }

    #endregion

    #region Decimal/Numeric Formatting Tests

    [Fact]
    public void Process_DecimalValue_UsesInvariantCulture()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "amount";
        helper.Value = 1234.56m;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("1234.56", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_DoubleValue_UsesInvariantCulture()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "rate";
        helper.Value = 0.15;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("0.15", output.Attributes["value"].Value);
    }

    #endregion

    #region Null and Empty Handling Tests

    [Fact]
    public void Process_NullValue_SuppressesOutputByDefault()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "optional";
        helper.Value = null;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.True(output.IsContentModified);
    }

    [Fact]
    public void Process_NullValue_IncludeEmpty_RendersEmptyValue()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "optional";
        helper.Value = null;
        helper.IncludeEmpty = true;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("input", output.TagName);
        Assert.Equal("", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_EmptyString_SuppressesOutputByDefault()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "filter";
        helper.Value = "";
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.True(output.IsContentModified);
    }

    #endregion

    #region Collection Handling Tests

    [Fact]
    public void Process_StringList_RendersCommaSeparated()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "tags";
        helper.Value = new List<string> { "urgent", "billing", "review" };
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("urgent,billing,review", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_IntArray_RendersCommaSeparated()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "ids";
        helper.Value = new[] { 1, 2, 3 };
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("1,2,3", output.Attributes["value"].Value);
    }

    [Fact]
    public void Process_Collection_MultipleMode_RendersMultipleInputs()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "selectedIds";
        helper.Value = new List<int> { 10, 20, 30 };
        helper.RenderMultiple = true;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Null(output.TagName); // No wrapper tag
        var content = output.Content.GetContent();
        Assert.Contains("name=\"selectedIds\" value=\"10\"", content);
        Assert.Contains("name=\"selectedIds\" value=\"20\"", content);
        Assert.Contains("name=\"selectedIds\" value=\"30\"", content);
    }

    #endregion

    #region Enum Handling Tests

    private enum Status { Pending, Active, Completed }

    [Fact]
    public void Process_EnumValue_RendersEnumName()
    {
        // Arrange
        var helper = CreateTagHelper();
        helper.Name = "status";
        helper.Value = Status.Active;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("Active", output.Attributes["value"].Value);
    }

    #endregion

    #region Guid Handling Tests

    [Fact]
    public void Process_GuidValue_RendersCorrectly()
    {
        // Arrange
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var helper = CreateTagHelper();
        helper.Name = "entityId";
        helper.Value = guid;
        var (context, output) = CreateTagHelperContext();

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", output.Attributes["value"].Value);
    }

    #endregion
}
