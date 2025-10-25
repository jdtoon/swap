using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using carestream.web.controllers;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.pharmacy;
using Microsoft.AspNetCore.Mvc.Routing;
using carestream.core.dtos.shared;

namespace carestream.tests.unit.controllers
{
    public class PharmacyControllerTests
    {
        private readonly Mock<IPharmacyService> _mockPharmacyService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<PharmacyController>> _mockLogger;
        private readonly PharmacyController _controller;
        private readonly Mock<IUrlHelper> _mockUrlHelper;

        public PharmacyControllerTests()
        {
            _mockPharmacyService = new Mock<IPharmacyService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<PharmacyController>>();
            _mockUrlHelper = new Mock<IUrlHelper>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("sub", "test-pharmacist-sub-id"),
                new Claim(ClaimTypes.Role, "Pharmacist")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };

            _controller = new PharmacyController(
                _mockPharmacyService.Object,
                _mockLogger.Object,
                _mockUserRepository.Object
            )
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext
                },
                // Provide TempData if actions use it for error messages passed to _ErrorPartial
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()),
                Url = _mockUrlHelper.Object
            };

            _mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                         .Returns((UrlActionContext uac) => $"/{uac.Controller}/{uac.Action}");
        }

        [Fact]
        public async Task ConfirmDispense_ValidModelAndUser_ServiceSucceeds_ShouldReturnConfirmationPartialWithSuccess()
        {
            // Arrange
            var dispenseInput = new StartDispenseViewModel { VisitId = 1, ItemsToDispense = new List<DispenseItemDto> { new DispenseItemDto { IsSelectedForDispense = true } } };
            var pharmacistInternalId = 101;
            var confirmationDto = new DispenseConfirmationDto { OverallSuccess = true, VisitId = 1 };

            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-pharmacist-sub-id", null, null)).ReturnsAsync(pharmacistInternalId);
            _mockPharmacyService.Setup(s => s.ProcessDispenseAsync(dispenseInput, pharmacistInternalId)).ReturnsAsync(confirmationDto);
            _controller.ModelState.Clear(); // Ensure model state is valid

            // Act
            var result = await _controller.ConfirmDispense(dispenseInput);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DispenseConfirmation", partialViewResult.ViewName);
            Assert.Same(confirmationDto, partialViewResult.Model);
            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
            Assert.Contains("showToastSuccess", _controller.Response.Headers["HX-Trigger"].ToString());
            _mockPharmacyService.Verify(s => s.ProcessDispenseAsync(dispenseInput, pharmacistInternalId), Times.Once);
        }

        [Fact]
        public async Task ConfirmDispense_InvalidModelState_ShouldReturnConfirmationPartialWithError()
        {
            // Arrange
            var dispenseInput = new StartDispenseViewModel { VisitId = 1 };
            _controller.ModelState.AddModelError("ItemsToDispense", "At least one item must be selected.");

            // Act
            var result = await _controller.ConfirmDispense(dispenseInput);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DispenseConfirmation", partialViewResult.ViewName);
            var model = Assert.IsType<DispenseConfirmationDto>(partialViewResult.Model);
            Assert.False(model.OverallSuccess);
            Assert.Contains("Invalid data submitted", model.ErrorMessage);
            Assert.True(_controller.Response.Headers.ContainsKey("HX-Retarget")); // As per controller logic for invalid model state
            _mockPharmacyService.Verify(s => s.ProcessDispenseAsync(It.IsAny<StartDispenseViewModel>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmDispense_UserNotLinked_ShouldReturnConfirmationPartialWithError()
        {
            // Arrange
            var dispenseInput = new StartDispenseViewModel { VisitId = 1 };
            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-pharmacist-sub-id", null, null)).ReturnsAsync((int?)null); // Simulate unlinked user
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.ConfirmDispense(dispenseInput);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DispenseConfirmation", partialViewResult.ViewName);
            var model = Assert.IsType<DispenseConfirmationDto>(partialViewResult.Model);
            Assert.False(model.OverallSuccess);
            Assert.Contains("Pharmacist account not fully configured", model.ErrorMessage);
            _mockPharmacyService.Verify(s => s.ProcessDispenseAsync(It.IsAny<StartDispenseViewModel>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmDispense_ServiceReturnsFailure_ShouldReturnConfirmationPartialWithServiceError()
        {
            // Arrange
            var dispenseInput = new StartDispenseViewModel { VisitId = 1, ItemsToDispense = new List<DispenseItemDto> { new DispenseItemDto { IsSelectedForDispense = true } } };
            var pharmacistInternalId = 101;
            var serviceErrorConfirmation = new DispenseConfirmationDto { OverallSuccess = false, VisitId = 1, ErrorMessage = "Service processing error" };

            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-pharmacist-sub-id", null, null)).ReturnsAsync(pharmacistInternalId);
            _mockPharmacyService.Setup(s => s.ProcessDispenseAsync(dispenseInput, pharmacistInternalId)).ReturnsAsync(serviceErrorConfirmation);
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.ConfirmDispense(dispenseInput);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DispenseConfirmation", partialViewResult.ViewName);
            Assert.Same(serviceErrorConfirmation, partialViewResult.Model); // Check that the DTO from service is passed through
            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger"));
            Assert.Contains("showToastWarning", _controller.Response.Headers["HX-Trigger"].ToString()); // Service failure should trigger warning
            Assert.Contains(serviceErrorConfirmation.ErrorMessage, _controller.Response.Headers["HX-Trigger"].ToString());
        }

        [Fact]
        public void DispensedHistory_GET_ShouldReturnInitialPartialViewWithDefaultModel()
        {
            // Arrange
            // No service call needed for the initial shell view if it just sets up hx-get

            // Act
            var result = _controller.DispensedHistory();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Null(partialViewResult.ViewName); // Expects DispensedHistory.cshtml by convention
            var model = Assert.IsType<DispensedHistoryViewModel>(partialViewResult.Model);
            Assert.NotNull(model.PaginationInfo);
            Assert.Equal(1, model.PaginationInfo.CurrentPage);
            Assert.Equal(25, model.PaginationInfo.PageSize); // Default from action
        }

        [Fact]
        public async Task DispensedHistoryList_GET_ShouldCallServiceAndReturnListPartialWithViewModel()
        {
            // Arrange
            var options = new FilterAndPaginationOptions { PageNumber = 2, PageSize = 10 };
            var expectedViewModel = new DispensedHistoryViewModel
            {
                DispensedItems = new List<DispensedHistoryItemDto> { new DispensedHistoryItemDto { DispensationLogItemId = 1 } },
                PaginationInfo = new PaginationDto { CurrentPage = 2, PageSize = 10, TotalItems = 20 }
            };

            _mockPharmacyService.Setup(s => s.GetDispensedHistoryViewModelAsync(options))
                                .ReturnsAsync(expectedViewModel);

            // Mock Url.Action for setting HxGetUrl in pagination info
            string expectedHxGetUrl = "/Pharmacy/DispensedHistoryList";
            _mockUrlHelper.Setup(x => x.Action(It.Is<UrlActionContext>(uac =>
                                uac.Action == "DispensedHistoryList" && uac.Controller == "Pharmacy")))
                          .Returns(expectedHxGetUrl);

            // Act
            var result = await _controller.DispensedHistoryList(options);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_DispensedHistoryList", partialViewResult.ViewName);
            var model = Assert.IsType<DispensedHistoryViewModel>(partialViewResult.Model);

            Assert.Same(expectedViewModel.DispensedItems, model.DispensedItems);
            Assert.NotNull(model.PaginationInfo);
            Assert.Equal(expectedViewModel.PaginationInfo.CurrentPage, model.PaginationInfo.CurrentPage);
            Assert.Equal(expectedViewModel.PaginationInfo.PageSize, model.PaginationInfo.PageSize);
            Assert.Equal(expectedViewModel.PaginationInfo.TotalItems, model.PaginationInfo.TotalItems);

            // Assert that HxGetUrl was set correctly for pagination controls
            Assert.Equal(expectedHxGetUrl, model.PaginationInfo.HxGetUrl);
            Assert.Equal("#dispensed-history-list-container", model.PaginationInfo.HxTarget);


            _mockPharmacyService.Verify(s => s.GetDispensedHistoryViewModelAsync(options), Times.Once);
        }

        [Fact]
        public async Task DispensedHistoryList_GET_WithDefaultOptions_ShouldCallServiceWithDefaults()
        {
            // Arrange
            var options = new FilterAndPaginationOptions(); // Controller will default PageNumber and PageSize
            var expectedViewModel = new DispensedHistoryViewModel { PaginationInfo = new PaginationDto() };

            _mockPharmacyService
                .Setup(s => s.GetDispensedHistoryViewModelAsync(It.Is<FilterAndPaginationOptions>(o => o.PageNumber == 1 && o.PageSize == 25)))
                .ReturnsAsync(expectedViewModel);

            // Act
            await _controller.DispensedHistoryList(options);

            // Assert
            _mockPharmacyService.Verify(s => s.GetDispensedHistoryViewModelAsync(
                It.Is<FilterAndPaginationOptions>(o => o.PageNumber == 1 && o.PageSize == 25)),
                Times.Once);
        }
    }
}