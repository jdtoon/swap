using Xunit;

namespace Swap.Htmx.Generators.Tests;

public class ViewPathGeneratorTests
{
    [Fact]
    public void Generates_Constants_For_Views_In_Folder()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views/Inventory"")]
public static partial class InventoryViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Inventory/Index.cshtml", "<h1>Index</h1>"),
            new InMemoryAdditionalText("C:/Project/Views/Inventory/Create.cshtml", "<h1>Create</h1>"),
            new InMemoryAdditionalText("C:/Project/Views/Inventory/Edit.cshtml", "<h1>Edit</h1>"),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string Index = \"Index\";", output);
        Assert.Contains("public const string Create = \"Create\";", output);
        Assert.Contains("public const string Edit = \"Edit\";", output);
    }

    [Fact]
    public void Generates_Partials_Class_For_Underscore_Prefixed_Views()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views/Products"")]
public static partial class ProductViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Products/Index.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Products/_Grid.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Products/_Pagination.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Products/_EditModal.cshtml", ""),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string Index = \"Index\";", output);
        Assert.Contains("public static class Partials", output);
        Assert.Contains("public const string Grid = \"_Grid\";", output);
        Assert.Contains("public const string Pagination = \"_Pagination\";", output);
        Assert.Contains("public const string EditModal = \"_EditModal\";", output);
    }

    [Fact]
    public void Handles_Empty_Folder_Gracefully()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views/Empty"")]
public static partial class EmptyViews { }
";

        // No additional texts - empty folder
        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, Array.Empty<InMemoryAdditionalText>());

        Assert.NotNull(output);
        Assert.Contains("public static partial class EmptyViews", output);
        Assert.Contains("No .cshtml files found", output);
    }

    [Fact]
    public void Excludes_Subdirectory_Files_By_Default()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views/Admin"")]
public static partial class AdminViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Admin/Dashboard.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Admin/Users/Index.cshtml", ""), // Subdirectory - should be excluded
            new InMemoryAdditionalText("C:/Project/Views/Admin/Users/Edit.cshtml", ""),  // Subdirectory - should be excluded
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string Dashboard = \"Dashboard\";", output);
        Assert.DoesNotContain("Users", output);
    }

    [Fact]
    public void Includes_Subdirectory_Files_When_Enabled()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views/Admin"", IncludeSubdirectories = true)]
public static partial class AdminViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Admin/Dashboard.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Admin/Users/Index.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Admin/Users/Edit.cshtml", ""),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("Dashboard", output);
        Assert.Contains("Index", output);
        Assert.Contains("Edit", output);
    }

    [Fact]
    public void Handles_Backslash_Paths()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views\\Patterns"")]
public static partial class PatternViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:\\Project\\Views\\Patterns\\BasicSwap.cshtml", ""),
            new InMemoryAdditionalText("C:\\Project\\Views\\Patterns\\MultiComponent.cshtml", ""),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string BasicSwap = \"BasicSwap\";", output);
        Assert.Contains("public const string MultiComponent = \"MultiComponent\";", output);
    }

    [Fact]
    public void Converts_KebabCase_To_PascalCase()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views/Components"")]
public static partial class ComponentViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Components/user-profile.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Components/_nav-menu.cshtml", ""),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public const string UserProfile = \"user-profile\";", output);
        Assert.Contains("public const string NavMenu = \"_nav-menu\";", output);
    }

    [Fact]
    public void Preserves_Namespace()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace My.Deep.Namespace;

[SwapViewSource(""Views/Test"")]
public static partial class TestViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", ""),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("namespace My.Deep.Namespace", output);
    }

    [Fact]
    public void Ignores_Non_Cshtml_Files()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapViewSource(""Views/Test"")]
public static partial class TestViews { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/Index.cshtml", ""),
            new InMemoryAdditionalText("C:/Project/Views/Test/style.css", ""),
            new InMemoryAdditionalText("C:/Project/Views/Test/script.js", ""),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<ViewPathGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("Index", output);
        Assert.DoesNotContain("style", output);
        Assert.DoesNotContain("script", output);
    }
}
