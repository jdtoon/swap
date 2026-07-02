using System.Threading.Tasks;
using Swap.Htmx.Generators.CodeFixes;
using Xunit;
using Verify = Swap.Htmx.Generators.Tests.CSharpCodeFixVerifier<
    Swap.Htmx.Generators.HandlerValidationAnalyzer,
    Swap.Htmx.Generators.CodeFixes.SwapEventHandlerCodeFixProvider>;

namespace Swap.Htmx.Generators.Tests;

public class SwapEventHandlerCodeFixProviderTests
{
    private const string Source = @"
public class OrderCreated { }

public interface ISwapEventHandler<T>
{
    System.Threading.Tasks.Task HandleAsync(T e);
}

public class Builder
{
    public Builder WithTrigger(string e, object payload) => this;
}

public static class Usage
{
    public static void Configure(Builder b)
    {
        {|#0:b.WithTrigger(""order.created"", new OrderCreated())|};
    }
}";

    // The unchanged prefix is derived from `Source` itself (markup stripped) rather than duplicated
    // as a second literal, so the comparison stays byte-for-byte correct regardless of this file's own
    // line-ending convention (e.g. after a checkout normalizes LF -> CRLF via core.autocrlf). Only the
    // newly scaffolded handler class — appended by the code fix and reformatted by the Roslyn test
    // harness using its own default (CRLF) formatting options — is appended with explicit "\r\n"
    // escapes, since that reformatting is independent of the original file's line endings.
    private static readonly string FixedSource =
        Source.Replace("{|#0:", string.Empty).Replace("|}", string.Empty) +
        "\r\n" +
        "\r\npublic class OrderCreatedHandler : ISwapEventHandler<OrderCreated>\r\n" +
        "{\r\n" +
        "    public System.Threading.Tasks.Task HandleAsync(OrderCreated e)\r\n" +
        "    {\r\n" +
        "        throw new System.NotImplementedException();\r\n" +
        "    }\r\n" +
        "}";

    [Fact]
    public async Task ScaffoldsISwapEventHandlerImplementation_ForUnhandledEvent()
    {
        var expected = Verify.Diagnostic("SWAP001").WithLocation(0).WithArguments("order.created");

        await Verify.VerifyCodeFixAsync(Source, expected, FixedSource);
    }
}
