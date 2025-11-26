using Xunit;

namespace Swap.Htmx.Generators.Tests;

public class StateClassGeneratorTests
{
    [Fact]
    public void Generates_Properties_From_SwapStateProp_Annotations()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace TestNamespace;

[SwapStateSource(""Views/Inventory/_InventoryState.cshtml"")]
public partial class InventoryState : SwapState { }
";

        var cshtmlContent = @"
<div data-swap-state>
    <input type=""hidden"" swap-state-prop=""Tab:string=all"" />
    <input type=""hidden"" swap-state-prop=""Page:int=1"" />
    <input type=""hidden"" swap-state-prop=""Search:string?"" />
</div>
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Inventory/_InventoryState.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public string Tab { get; set; } = \"all\";", output);
        Assert.Contains("public int Page { get; set; } = 1;", output);
        Assert.Contains("public string? Search { get; set; };", output);
    }

    [Fact]
    public void Handles_Boolean_Properties()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace TestNamespace;

[SwapStateSource(""Views/Test/_TestState.cshtml"")]
public partial class TestState : SwapState { }
";

        var cshtmlContent = @"
<input type=""hidden"" swap-state-prop=""IsActive:bool=true"" />
<input type=""hidden"" swap-state-prop=""ShowDeleted:bool=false"" />
<input type=""hidden"" swap-state-prop=""IsFlagged:bool?"" />
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/_TestState.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public bool IsActive { get; set; } = true;", output);
        Assert.Contains("public bool ShowDeleted { get; set; } = false;", output);
        Assert.Contains("public bool? IsFlagged { get; set; };", output);
    }

    [Fact]
    public void Handles_Numeric_Types()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace TestNamespace;

[SwapStateSource(""Views/Test/_TestState.cshtml"")]
public partial class TestState : SwapState { }
";

        var cshtmlContent = @"
<input type=""hidden"" swap-state-prop=""Count:int=10"" />
<input type=""hidden"" swap-state-prop=""Price:decimal=99.99"" />
<input type=""hidden"" swap-state-prop=""Rating:double=4.5"" />
<input type=""hidden"" swap-state-prop=""Quantity:long=1000000"" />
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/_TestState.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public int Count { get; set; } = 10;", output);
        Assert.Contains("public decimal Price { get; set; } = 99.99;", output);
        Assert.Contains("public double Rating { get; set; } = 4.5;", output);
        Assert.Contains("public long Quantity { get; set; } = 1000000;", output);
    }

    [Fact]
    public void Handles_Single_Quotes()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace TestNamespace;

[SwapStateSource(""Views/Test/_TestState.cshtml"")]
public partial class TestState : SwapState { }
";

        var cshtmlContent = @"
<input type='hidden' swap-state-prop='Category:string=books' />
<input type='hidden' swap-state-prop='Limit:int=25' />
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/_TestState.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public string Category { get; set; } = \"books\";", output);
        Assert.Contains("public int Limit { get; set; } = 25;", output);
    }

    [Fact]
    public void Preserves_Namespace()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace MyApp.Features.Inventory;

[SwapStateSource(""Views/Inventory/_State.cshtml"")]
public partial class InventoryState : SwapState { }
";

        var cshtmlContent = @"
<input type=""hidden"" swap-state-prop=""Page:int=1"" />
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Inventory/_State.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("namespace MyApp.Features.Inventory", output);
        Assert.Contains("public partial class InventoryState", output);
    }

    [Fact]
    public void Handles_String_With_Special_Characters()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace TestNamespace;

[SwapStateSource(""Views/Test/_TestState.cshtml"")]
public partial class TestState : SwapState { }
";

        var cshtmlContent = @"
<input type=""hidden"" swap-state-prop=""Filter:string=name:asc"" />
<input type=""hidden"" swap-state-prop=""Query:string=default"" />
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/_TestState.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public string Filter { get; set; } = \"name:asc\";", output);
        Assert.Contains("public string Query { get; set; } = \"default\";", output);
    }

    [Fact]
    public void Returns_Null_When_No_Matching_File()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace TestNamespace;

[SwapStateSource(""Views/NonExistent/_State.cshtml"")]
public partial class TestState : SwapState { }
";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Other/_OtherState.cshtml", "<input swap-state-prop=\"X:int=1\" />"),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.Null(output);
    }

    [Fact]
    public void Handles_Multiple_Properties_Same_Line()
    {
        var source = @"
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

namespace TestNamespace;

[SwapStateSource(""Views/Test/_TestState.cshtml"")]
public partial class TestState : SwapState { }
";

        // Properties on same line (unusual but should work)
        var cshtmlContent = @"<input swap-state-prop=""A:int=1"" /><input swap-state-prop=""B:int=2"" />";

        var additionalTexts = new[]
        {
            new InMemoryAdditionalText("C:/Project/Views/Test/_TestState.cshtml", cshtmlContent),
        };

        var output = GeneratorTestHelper.GetGeneratedOutput<StateClassGenerator>(source, additionalTexts);

        Assert.NotNull(output);
        Assert.Contains("public int A { get; set; } = 1;", output);
        Assert.Contains("public int B { get; set; } = 2;", output);
    }
}
