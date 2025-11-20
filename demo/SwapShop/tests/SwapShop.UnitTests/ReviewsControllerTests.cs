using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapShop.Controllers;
using SwapShop.Events;
using SwapShop.Models;
using SwapShop.Services;
using Xunit;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SwapShop.UnitTests;

public class ReviewsControllerTests
{
    private readonly Mock<IReviewService> _reviewServiceMock;
    private readonly Mock<ISwapEventService> _swapEventServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ITempDataDictionaryFactory> _tempDataFactoryMock;
    private readonly ReviewsController _controller;

    public ReviewsControllerTests()
    {
        _reviewServiceMock = new Mock<IReviewService>();
        _swapEventServiceMock = new Mock<ISwapEventService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _tempDataFactoryMock = new Mock<ITempDataDictionaryFactory>();

        // Setup ServiceProvider to return ISwapEventService and ITempDataDictionaryFactory
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(ISwapEventService)))
            .Returns(_swapEventServiceMock.Object);
            
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(ITempDataDictionaryFactory)))
            .Returns(_tempDataFactoryMock.Object);

        // Setup Controller Context
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProviderMock.Object
        };

        _controller = new ReviewsController(_reviewServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    [Fact]
    public void List_ReturnsPartialView_WhenHtmxRequest()
    {
        // Arrange
        _controller.Request.Headers.Add("HX-Request", "true");
        var reviews = new List<Review>();
        _reviewServiceMock.Setup(x => x.GetByProductId(1)).Returns(reviews);

        // Act
        var result = _controller.List(1) as PartialViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("_ReviewList", result.ViewName);
        Assert.Same(reviews, result.Model);
    }

    [Fact]
    public void List_ReturnsFullView_WhenNormalRequest()
    {
        // Arrange
        // No HX-Request header
        var reviews = new List<Review>();
        _reviewServiceMock.Setup(x => x.GetByProductId(1)).Returns(reviews);

        // Act
        var result = _controller.List(1) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("_ReviewList", result.ViewName);
        Assert.Same(reviews, result.Model);
    }

    [Fact]
    public void Add_TriggersEvent_WhenValid()
    {
        // Arrange
        var review = new Review { ProductId = 1, UserName = "Test", Comment = "Test", Rating = 5 };
        
        var builder = new SwapResponseBuilder(_controller);
        
        _swapEventServiceMock
            .Setup(x => x.Event(ReviewEvents.Added, _controller, review))
            .Returns(builder);

        // Act
        var result = _controller.Add(review);

        // Assert
        _reviewServiceMock.Verify(x => x.AddReview(review), Times.Once);
        _swapEventServiceMock.Verify(x => x.Event(ReviewEvents.Added, _controller, review), Times.Once);
    }

    [Fact]
    public void Add_ReturnsValidationErrors_WhenInvalid()
    {
        // Arrange
        var review = new Review { ProductId = 1 };
        _controller.ModelState.AddModelError("UserName", "Required");
        
        var builder = new SwapResponseBuilder(_controller);
        _swapEventServiceMock.Setup(x => x.Response(_controller)).Returns(builder);
        
        // Act
        var result = _controller.Add(review);
        
        // Assert
        Assert.IsType<Swap.Htmx.Results.SwapActionResult>(result);
        
        // Verify builder state
        Assert.Contains(builder.Toasts, t => t.Message == "Please correct the errors below.");
        Assert.Contains(builder.Triggers, t => t.EventName == "validationFailed");
    }
}
