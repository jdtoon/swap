using Xunit;

namespace Swap.Htmx.Generators.Tests;

public class ElementIdGeneratorTests
{
    [Fact]
    public void Generates_Constants_For_Element_Ids()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Products"")]
public static partial class ProductIds { }
";

        var cshtmlContent = @"
<div id=""product-grid"">
    <table id=""products-table"">
        <tbody id=""table-body""></tbody>
    </table>
</div>
<div id=""pagination""></div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Products/Index.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string ProductGrid = \"product-grid\";", output);
        Assert.Contains("public const string ProductsTable = \"products-table\";", output);
        Assert.Contains("public const string TableBody = \"table-body\";", output);
        Assert.Contains("public const string Pagination = \"pagination\";", output);
    }

    [Fact]
    public void Converts_KebabCase_To_PascalCase()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Test"")]
public static partial class TestIds { }
";

        var cshtmlContent = @"
<div id=""my-awesome-component""></div>
<div id=""user-profile-card""></div>
<div id=""nav_menu_item""></div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string MyAwesomeComponent = \"my-awesome-component\";", output);
        Assert.Contains("public const string UserProfileCard = \"user-profile-card\";", output);
        Assert.Contains("public const string NavMenuItem = \"nav_menu_item\";", output);
    }

    [Fact]
    public void Handles_Single_Quotes()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Test"")]
public static partial class TestIds { }
";

        var cshtmlContent = @"
<div id='single-quote-id'></div>
<div id=""double-quote-id""></div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string SingleQuoteId = \"single-quote-id\";", output);
        Assert.Contains("public const string DoubleQuoteId = \"double-quote-id\";", output);
    }

    [Fact]
    public void Skips_Dynamic_Razor_Ids()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Test"")]
public static partial class TestIds { }
";

        var cshtmlContent = @"
<div id=""static-id""></div>
<div id=""@Model.DynamicId""></div>
<div id=""item-@item.Id""></div>
<div id=""@($""dynamic-{id}"")""></div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string StaticId = \"static-id\";", output);
        // Dynamic IDs should be skipped
        Assert.DoesNotContain("Model", output);
        Assert.DoesNotContain("item", output);
        Assert.DoesNotContain("dynamic", output);
    }

    [Fact]
    public void Deduplicates_Ids_Across_Files()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Test"")]
public static partial class TestIds { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", @"<div id=""shared-id""></div>"),
            new InMemoryAdditionalText("C:/Project/Views/Test/Edit.cshtml", @"<div id=""shared-id""></div>"),
            new InMemoryAdditionalText("C:/Project/Views/Test/_Partial.cshtml", @"<div id=""shared-id""></div>"),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        // Should only appear once
        var count = output.Split("SharedId").Length - 1;
        Assert.Equal(1, count);
    }

    [Fact]
    public void Filters_By_Prefix()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Test"", Prefix = ""product-"")]
public static partial class ProductIds { }
";

        var cshtmlContent = @"
<div id=""product-grid""></div>
<div id=""product-search""></div>
<div id=""user-profile""></div>
<div id=""cart-total""></div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("ProductGrid", output);
        Assert.Contains("ProductSearch", output);
        Assert.DoesNotContain("UserProfile", output);
        Assert.DoesNotContain("CartTotal", output);
    }

    [Fact]
    public void Excludes_Subdirectory_Files_By_Default()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Admin"")]
public static partial class AdminIds { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Admin/Index.cshtml", @"<div id=""admin-dashboard""></div>"),
            new InMemoryAdditionalText("C:/Project/Views/Admin/Users/Index.cshtml", @"<div id=""users-list""></div>"),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("AdminDashboard", output);
        Assert.DoesNotContain("UsersList", output);
    }

    [Fact]
    public void Includes_Subdirectory_Files_When_Enabled()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Admin"", IncludeSubdirectories = true)]
public static partial class AdminIds { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Admin/Index.cshtml", @"<div id=""admin-dashboard""></div>"),
            new InMemoryAdditionalText("C:/Project/Views/Admin/Users/Index.cshtml", @"<div id=""users-list""></div>"),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("AdminDashboard", output);
        Assert.Contains("UsersList", output);
    }

    [Fact]
    public void Handles_Empty_Folder_Gracefully()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Empty"")]
public static partial class EmptyIds { }
";

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, Array.Empty<InMemoryAdditionalText>());

        Assert.NotNull(output);
        Assert.Contains("public static partial class EmptyIds", output);
        Assert.Contains("No element IDs found", output);
    }

    [Fact]
    public void Handles_IDs_Starting_With_Numbers()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Test"")]
public static partial class TestIds { }
";

        var cshtmlContent = @"
<div id=""123-test""></div>
<div id=""item-456""></div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        // IDs starting with numbers should have underscore prefix
        Assert.Contains("_123Test", output);
        Assert.Contains("Item456", output);
    }

    [Fact]
    public void Preserves_Namespace()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace My.Deep.Namespace;

[SwapElementSource(""Views/Test"")]
public static partial class TestIds { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", @"<div id=""test""></div>"),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("namespace My.Deep.Namespace", output);
    }

    [Fact]
    public void Handles_Whitespace_Around_Equals()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapElementSource(""Views/Test"")]
public static partial class TestIds { }
";

        var cshtmlContent = @"
<div id=""no-space""></div>
<div id =""space-before""></div>
<div id= ""space-after""></div>
<div id = ""space-both""></div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ElementIdGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("NoSpace", output);
        Assert.Contains("SpaceBefore", output);
        Assert.Contains("SpaceAfter", output);
        Assert.Contains("SpaceBoth", output);
    }
}
