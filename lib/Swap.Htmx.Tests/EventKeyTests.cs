using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Tests for type-safe EventKey usage in SwapResponseBuilder.
/// </summary>
public class EventKeyTests
{
    private class TestController : SwapController
    {
        public SwapResponseBuilder GetBuilder() => SwapResponse();
    }
    
    private static TestController CreateController()
    {
        var controller = new TestController();
        
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        controller.ViewData = new ViewDataDictionary(
            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        
        controller.TempData = new TempDataDictionary(
            httpContext,
            new SessionStateTempDataProvider());
        
        return controller;
    }
    
    // Test event definitions
    private static class TestEvents
    {
        public static readonly EventKey ProductCreated = new("product.created");
        public static readonly EventKey ProductUpdated = new("product.updated");
        public static readonly EventKey ProductDeleted = new("product.deleted");
        
        public static class Cart
        {
            public static readonly EventKey ItemAdded = new("cart.itemAdded");
            public static readonly EventKey ItemRemoved = new("cart.itemRemoved");
        }
        
        // Factory method for dynamic events
        public static EventKey UserRoleChanged(string role) => new($"user.role.{role}");
    }

    [Fact]
    public void EventKey_StoresName()
    {
        // Arrange
        var key = new EventKey("test.event");

        // Assert
        Assert.Equal("test.event", key.Name);
    }

    [Fact]
    public void EventKey_ToString_ReturnsName()
    {
        // Arrange
        var key = new EventKey("test.event");

        // Act
        var result = key.ToString();

        // Assert
        Assert.Equal("test.event", result);
    }

    [Fact]
    public void EventKey_ImplicitConversion_ToString()
    {
        // Arrange
        var key = new EventKey("test.event");

        // Act
        string str = key;

        // Assert
        Assert.Equal("test.event", str);
    }

    [Fact]
    public void EventKey_Equality_WorksCorrectly()
    {
        // Arrange
        var key1 = new EventKey("test.event");
        var key2 = new EventKey("test.event");
        var key3 = new EventKey("other.event");

        // Assert
        Assert.Equal(key1, key2);
        Assert.NotEqual(key1, key3);
    }

    [Fact]
    public void SwapEvents_UI_ContainsCommonEvents()
    {
        // Assert - Verify common UI events exist
        Assert.Equal("ui.refreshList", SwapEvents.UI.RefreshList.Name);
        Assert.Equal("ui.refreshPage", SwapEvents.UI.RefreshPage.Name);
        Assert.Equal("ui.openModal", SwapEvents.UI.OpenModal.Name);
        Assert.Equal("ui.closeModal", SwapEvents.UI.CloseModal.Name);
        Assert.Equal("ui.showToast", SwapEvents.UI.ShowToast.Name);
    }

    [Fact]
    public void SwapEvents_Entity_GeneratesCorrectNames()
    {
        // Act
        var created = SwapEvents.Entity.Created("product");
        var updated = SwapEvents.Entity.Updated("order");
        var deleted = SwapEvents.Entity.Deleted("customer");

        // Assert
        Assert.Equal("product.created", created.Name);
        Assert.Equal("order.updated", updated.Name);
        Assert.Equal("customer.deleted", deleted.Name);
    }

    [Fact]
    public void SwapEvents_Auth_ContainsAuthEvents()
    {
        // Assert
        Assert.Equal("auth.loggedIn", SwapEvents.Auth.LoggedIn.Name);
        Assert.Equal("auth.loggedOut", SwapEvents.Auth.LoggedOut.Name);
        Assert.Equal("auth.sessionExpired", SwapEvents.Auth.SessionExpired.Name);
    }

    [Fact]
    public void SwapEvents_Form_ContainsFormEvents()
    {
        // Assert
        Assert.Equal("form.validationFailed", SwapEvents.Form.ValidationFailed.Name);
        Assert.Equal("form.validationPassed", SwapEvents.Form.ValidationPassed.Name);
        Assert.Equal("form.submitted", SwapEvents.Form.Submitted.Name);
    }

    [Fact]
    public void SwapEvents_Notification_ContainsNotificationTypes()
    {
        // Assert
        Assert.Equal("notification.success", SwapEvents.Notification.Success.Name);
        Assert.Equal("notification.error", SwapEvents.Notification.Error.Name);
        Assert.Equal("notification.warning", SwapEvents.Notification.Warning.Name);
        Assert.Equal("notification.info", SwapEvents.Notification.Info.Name);
    }

    [Fact]
    public void StaticEventKey_CanBeUsedInBuilder()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();

        // Act
        builder.WithTrigger(TestEvents.ProductCreated);

        // Assert
        Assert.Single(builder.Triggers);
        Assert.Equal("product.created", builder.Triggers[0].EventName);
    }

    [Fact]
    public void StaticEventKey_WithPayload_CanBeUsedInBuilder()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();
        var payload = new { id = 123, name = "Test Product" };

        // Act
        builder.WithTrigger(TestEvents.ProductCreated, payload);

        // Assert
        Assert.Single(builder.Triggers);
        Assert.Equal("product.created", builder.Triggers[0].EventName);
        Assert.Same(payload, builder.Triggers[0].Payload);
    }

    [Fact]
    public void NestedEventKey_CanBeUsedInBuilder()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();

        // Act
        builder.WithTrigger(TestEvents.Cart.ItemAdded);

        // Assert
        Assert.Single(builder.Triggers);
        Assert.Equal("cart.itemAdded", builder.Triggers[0].EventName);
    }

    [Fact]
    public void FactoryEventKey_CanBeUsedInBuilder()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();

        // Act
        builder.WithTrigger(TestEvents.UserRoleChanged("admin"));

        // Assert
        Assert.Single(builder.Triggers);
        Assert.Equal("user.role.admin", builder.Triggers[0].EventName);
    }

    [Fact]
    public void MultipleEventKeys_CanBeChained()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();

        // Act
        builder
            .WithTrigger(TestEvents.ProductCreated)
            .WithTrigger(TestEvents.Cart.ItemAdded)
            .WithTrigger(SwapEvents.UI.RefreshList);

        // Assert
        Assert.Equal(3, builder.Triggers.Count);
        Assert.Equal("product.created", builder.Triggers[0].EventName);
        Assert.Equal("cart.itemAdded", builder.Triggers[1].EventName);
        Assert.Equal("ui.refreshList", builder.Triggers[2].EventName);
    }

    [Fact]
    public void BuiltInSwapEvents_CanBeMixedWithCustomEvents()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();

        // Act
        builder
            .WithTrigger(SwapEvents.UI.ShowSpinner)
            .WithTrigger(TestEvents.ProductCreated)
            .WithTrigger(SwapEvents.UI.HideSpinner)
            .WithTrigger(SwapEvents.Notification.Success);

        // Assert
        Assert.Equal(4, builder.Triggers.Count);
        Assert.Equal("ui.showSpinner", builder.Triggers[0].EventName);
        Assert.Equal("product.created", builder.Triggers[1].EventName);
        Assert.Equal("ui.hideSpinner", builder.Triggers[2].EventName);
        Assert.Equal("notification.success", builder.Triggers[3].EventName);
    }

    [Fact]
    public void StringTrigger_StillWorks_ForBackwardCompatibility()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();

        // Act
        builder.WithTrigger("custom.event");

        // Assert
        Assert.Single(builder.Triggers);
        Assert.Equal("custom.event", builder.Triggers[0].EventName);
    }

    [Fact]
    public void EventKey_AndString_CanBeMixedInBuilder()
    {
        // Arrange
        var controller = CreateController();
        var builder = controller.GetBuilder();

        // Act
        builder
            .WithTrigger(TestEvents.ProductCreated)
            .WithTrigger("legacy.event")
            .WithTrigger(SwapEvents.UI.RefreshList);

        // Assert
        Assert.Equal(3, builder.Triggers.Count);
        Assert.Equal("product.created", builder.Triggers[0].EventName);
        Assert.Equal("legacy.event", builder.Triggers[1].EventName);
        Assert.Equal("ui.refreshList", builder.Triggers[2].EventName);
    }
}
