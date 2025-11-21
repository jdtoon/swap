using Xunit;

namespace Swap.Htmx.Generators.Tests;

public class EventSourceGeneratorTests
{
    [Fact]
    public void Generates_EventKeys_For_Marked_Class()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapEventSource]
public partial class TestEvents
{
    public const string UserCreated = ""user.created"";
    public const string UserDeleted = ""user.deleted"";
}";

        var output = GeneratorTestHelper.GetGeneratedOutput(source);

        Assert.NotNull(output);
        Assert.Contains("public static partial class User", output);
        Assert.Contains("public static readonly EventKey Created = new EventKey(\"user.created\");", output);
        Assert.Contains("public static readonly EventKey Deleted = new EventKey(\"user.deleted\");", output);
    }

    [Fact]
    public void Generates_Nested_Classes_For_Dot_Notation()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapEventSource]
public partial class ComplexEvents
{
    public const string OrderItemAdded = ""order.item.added"";
}";

        var output = GeneratorTestHelper.GetGeneratedOutput(source);

        Assert.NotNull(output);
        Assert.Contains("public static partial class Order", output);
        Assert.Contains("public static partial class Item", output);
        Assert.Contains("public static readonly EventKey Added = new EventKey(\"order.item.added\");", output);
    }

    [Fact]
    public void Ignores_Non_Const_Strings()
    {
        var source = @"
using Swap.Htmx.Attributes;

namespace TestNamespace;

[SwapEventSource]
public partial class IgnoredEvents
{
    public string NotConst = ""should.ignore"";
    public const int NotString = 123;
}";

        var output = GeneratorTestHelper.GetGeneratedOutput(source);

        // Should generate the class structure but no events
        Assert.NotNull(output);
        Assert.DoesNotContain("NotConst", output);
        Assert.DoesNotContain("NotString", output);
    }
}
