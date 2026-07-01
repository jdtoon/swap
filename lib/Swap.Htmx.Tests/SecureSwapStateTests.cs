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
        var state = new SecureState { Secret = "TopSecret", Count = 99887766 };
        
        var queryString = state.ToQueryString(_provider);
        
        Assert.Contains("Secret=", queryString);
        Assert.DoesNotContain("TopSecret", queryString); // Should be encrypted
        
        Assert.Contains("Count=", queryString);
        Assert.DoesNotContain("99887766", queryString); // Should be encrypted
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
    public void FromQueryString_FlagsTampering_AndDoesNotApply_WhenProtectedValueTampered()
    {
        var state = new SecureState { Secret = "TopSecret", Count = 42 };
        var queryString = state.ToQueryString(_provider);

        // Tamper with the secret
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString.TrimStart('?'));
        var tamperedValues = new Dictionary<string, StringValues>(query);
        tamperedValues["Secret"] = "TamperedValue"; // Not a valid protected token

        var newState = new SecureState();
        newState.FromQueryString(new QueryCollection(tamperedValues), _provider);

        // Fail closed: tamper is signalled and the forged value is never applied
        Assert.True(newState.Tampered);
        Assert.NotEqual("TamperedValue", newState.Secret);
    }

    [Fact]
    public void FromQueryString_FlagsTampering_WhenProtectedValueEmptied()
    {
        var state = new SecureState { Secret = "TopSecret", Count = 42 };
        var queryString = state.ToQueryString(_provider);

        // Clear a protected numeric field — must NOT silently become 0
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString.TrimStart('?'));
        var tamperedValues = new Dictionary<string, StringValues>(query);
        tamperedValues["Count"] = "";

        var newState = new SecureState { Count = 999 };
        newState.FromQueryString(new QueryCollection(tamperedValues), _provider);

        Assert.True(newState.Tampered);
        Assert.NotEqual(0, newState.Count);
    }

    [Fact]
    public void FromQueryString_DoesNotFlagTampering_WhenUntampered()
    {
        var state = new SecureState { Secret = "TopSecret", Count = 42 };
        var queryString = state.ToQueryString(_provider);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString.TrimStart('?'));

        var newState = new SecureState();
        newState.FromQueryString(new QueryCollection(query), _provider);

        Assert.False(newState.Tampered);
        Assert.Equal("TopSecret", newState.Secret);
        Assert.Equal(42, newState.Count);
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
