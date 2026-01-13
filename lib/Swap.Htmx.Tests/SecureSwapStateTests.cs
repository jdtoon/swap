using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Swap.Htmx.State;
using Xunit;

namespace Swap.Htmx.Tests;

public class SecureState : SwapState
{
    public override bool Protected => true;
    public override bool UrlSync => true;

    public string Secret { get; set; } = "hidden";
    public int Count { get; set; } = 0;
}

public class SecureSwapStateTests
{
    private readonly IDataProtectionProvider _provider;

    public SecureSwapStateTests()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        _provider = sp.GetRequiredService<IDataProtectionProvider>();
    }

    [Fact]
    public void ToQueryString_EncryptsValues_WhenProtected()
    {
        var state = new SecureState { Secret = "TopSecret", Count = 42 };
        
        var queryString = state.ToQueryString(_provider);
        
        Assert.Contains("Secret=", queryString);
        Assert.DoesNotContain("TopSecret", queryString); // Should be encrypted
        
        Assert.Contains("Count=", queryString);
        Assert.DoesNotContain("42", queryString); // Should be encrypted
    }

    [Fact]
    public void FromQueryString_DecryptsValues_WhenProtected()
    {
        var state = new SecureState { Secret = "TopSecret", Count = 42 };
        var queryString = state.ToQueryString(_provider);
        
        // Parse the query string back
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString.TrimStart('?'));
        
        var newState = new SecureState();
        newState.FromQueryString(new QueryCollection(query), _provider);
        
        Assert.Equal("TopSecret", newState.Secret);
        Assert.Equal(42, newState.Count);
    }

    [Fact]
    public void FromQueryString_FailsGracefully_WithTamperedData()
    {
        var state = new SecureState { Secret = "TopSecret", Count = 42 };
        var queryString = state.ToQueryString(_provider);
        
        // Tamper with the secret
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString.TrimStart('?'));
        var tamperedValues = new Dictionary<string, StringValues>(query);
        tamperedValues["Secret"] = "TamperedValue"; // Not a valid encrypted string
        
        var newState = new SecureState();
        newState.FromQueryString(new QueryCollection(tamperedValues), _provider);
        
        // Should ignore invalid values and stick to defaults
        Assert.Equal("hidden", newState.Secret); // Default value
        Assert.Equal(42, newState.Count); // Valid value should still work
    }

    [Fact]
    public void Renderer_GetFormattedValue_Encrypts_WhenProtected()
    {
        var state = new SecureState { Secret = "MySecret" };
        
        var encrypted = SwapStateRenderer.GetFormattedValue(state, "Secret", "MySecret", _provider);
        
        Assert.NotEqual("MySecret", encrypted);
        
        // Verify we can decrypt it
        var protector = _provider.CreateProtector("SwapState", state.ContainerId, "Secret");
        var decrypted = protector.Unprotect(encrypted);
        Assert.Equal("MySecret", decrypted);
    }
}
