//using Xunit;
//using Moq;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Http;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using Microsoft.Extensions.Logging;
//using carestream.web.controllers;
//using carestream.core.interfaces.services;
//using carestream.core.dtos.admin;
//using Microsoft.AspNetCore.Mvc.Routing; // For IUrlHelper
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using carestream.web.Controllers; // For TempDataDictionary

//namespace carestream.tests.unit.controllers
//{
//    public class AdminControllerTests
//    {
//        private readonly Mock<IAdminUserService> _mockAdminUserService;
//        private readonly Mock<ILogger<AdminController>> _mockLogger;
//        private readonly AdminController _controller;
//        private readonly Mock<IUrlHelper> _mockUrlHelper;

//        public AdminControllerTests()
//        {
//            _mockAdminUserService = new Mock<IAdminUserService>();
//            _mockLogger = new Mock<ILogger<AdminController>>();
//            _mockUrlHelper = new Mock<IUrlHelper>();

//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.Role, "SystemAdmin") // Ensure admin role for [Authorize]
//            }, "mock"));

//            var httpContext = new DefaultHttpContext { User = user };
//            // Setup TempData
//            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());


//            _controller = new AdminController(
//                _mockAdminUserService.Object,
//                _mockLogger.Object
//            )
//            {
//                ControllerContext = new ControllerContext
//                {
//                    HttpContext = httpContext
//                },
//                Url = _mockUrlHelper.Object, // For Url.Action
//                TempData = tempData // For TempData in actions
//            };

//            // Default setup for Url.Action
//            _mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
//                          .Returns((UrlActionContext uac) => $"/{uac.Controller}/{uac.Action}");
//        }

//        [Fact]
//        public void Index_ShouldReturnPartialView()
//        {
//            // Act
//            var result = _controller.Index();

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Null(partialViewResult.ViewName); // Expects Index.cshtml by convention
//        }

//        [Fact]
//        public async Task UserListPartial_ShouldCallServiceAndReturnPartialViewWithViewModel()
//        {
//            // Arrange
//            string? searchTerm = "test";
//            int pageNumber = 1;
//            int pageSize = 10;
//            var expectedViewModel = new AdminUserManagementViewModel { SearchTerm = searchTerm, Users = new List<AdminUserListItemDto>() };
//            _mockAdminUserService.Setup(s => s.GetUserManagementViewModelAsync(searchTerm, pageNumber, pageSize))
//                                 .ReturnsAsync(expectedViewModel);

//            // Act
//            var result = await _controller.UserListPartial(searchTerm, pageNumber, pageSize);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_UserListPartial", partialViewResult.ViewName);
//            Assert.Same(expectedViewModel, partialViewResult.Model);
//            _mockAdminUserService.Verify(s => s.GetUserManagementViewModelAsync(searchTerm, pageNumber, pageSize), Times.Once);
//        }

//        [Fact]
//        public async Task LinkUserModal_UserFound_ShouldReturnModalPartialWithUser()
//        {
//            // Arrange
//            int userId = 1;
//            var expectedUser = new AdminUserListItemDto { UserId = userId, FullName = "Test User" };
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(userId)).ReturnsAsync(expectedUser);

//            // Act
//            var result = await _controller.LinkUserModal(userId);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_LinkUserModalPartial", partialViewResult.ViewName);
//            Assert.Same(expectedUser, partialViewResult.Model);
//        }

//        [Fact]
//        public async Task LinkUserModal_UserNotFound_ShouldReturnErrorContent()
//        {
//            // Arrange
//            int userId = 99;
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(userId)).ReturnsAsync((AdminUserListItemDto?)null);

//            // Act
//            var result = await _controller.LinkUserModal(userId);

//            // Assert
//            var contentResult = Assert.IsType<ContentResult>(result);
//            Assert.Contains("User not found", contentResult.Content);
//        }

//        [Fact]
//        public async Task LinkUser_SuccessfulLink_ShouldReturnUserListPartialAndSuccessTrigger()
//        {
//            // Arrange
//            int userId = 1;
//            string logtoSub = "logto|123";
//            var updatedUserListViewModel = new AdminUserManagementViewModel(); // Assume service returns this after linking
//            _mockAdminUserService.Setup(s => s.LinkUserToLogtoAsync(userId, logtoSub)).ReturnsAsync(true);
//            _mockAdminUserService.Setup(s => s.GetUserManagementViewModelAsync(null, 1, 25)).ReturnsAsync(updatedUserListViewModel); // For reloading list

//            // Act
//            var result = await _controller.LinkUser(userId, logtoSub);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_UserListPartial", partialViewResult.ViewName);
//            Assert.Same(updatedUserListViewModel, partialViewResult.Model);
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger-After-Swap"));
//            Assert.Contains("showToastSuccess", _controller.Response.Headers["HX-Trigger-After-Swap"].ToString());
//            Assert.Contains("closeLinkUserModal", _controller.Response.Headers["HX-Trigger-After-Swap"].ToString());
//        }

//        [Fact]
//        public async Task LinkUser_FailedLink_ShouldReturnModalPartialWithErrorTriggerAndMessage()
//        {
//            // Arrange
//            int userId = 1;
//            string logtoSub = "logto|123";
//            var userToRelink = new AdminUserListItemDto { UserId = userId };
//            _mockAdminUserService.Setup(s => s.LinkUserToLogtoAsync(userId, logtoSub)).ReturnsAsync(false); // Simulate link failure
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(userId)).ReturnsAsync(userToRelink); // For re-rendering modal

//            // Act
//            var result = await _controller.LinkUser(userId, logtoSub);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_LinkUserModalPartial", partialViewResult.ViewName);
//            Assert.Same(userToRelink, partialViewResult.Model);
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger-After-Swap"));
//            Assert.Contains("showToastError", _controller.Response.Headers["HX-Trigger-After-Swap"].ToString());
//            Assert.NotNull(partialViewResult.ViewData["LinkErrorMessage"]);
//        }

//        [Theory]
//        [InlineData(0, "logto|123")]
//        [InlineData(1, " ")]
//        public async Task LinkUser_InvalidInput_ShouldReRenderModalWithErrorToast(int userId, string logtoSub)
//        {
//            // Arrange
//            var userToRelink = new AdminUserListItemDto { UserId = userId }; // User to pass back to modal
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(userId)).ReturnsAsync(userToRelink);


//            // Act
//            var result = await _controller.LinkUser(userId, logtoSub);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_LinkUserModalPartial", partialViewResult.ViewName);
//            // If userId is 0, GetUserForAdminByIdAsync might return null, handle this
//            if (userId > 0)
//            {
//                Assert.Same(userToRelink, partialViewResult.Model);
//            }
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger-After-Swap"));
//            Assert.Contains("showToastError", _controller.Response.Headers["HX-Trigger-After-Swap"].ToString());
//            Assert.Contains("User ID and Logto ID are required", _controller.Response.Headers["HX-Trigger-After-Swap"].ToString());
//        }

//        [Fact]
//        public async Task SetVerificationCodeModal_UserFound_ShouldReturnModalPartialWithUserModel()
//        {
//            // Arrange
//            int userId = 1;
//            var userDto = new AdminUserListItemDto { UserId = userId, FullName = "Test User" };
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(userId)).ReturnsAsync(userDto);

//            // Act
//            var result = await _controller.SetVerificationCodeModal(userId);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_SetUserVerificationCodeModalPartial", partialViewResult.ViewName);
//            var model = Assert.IsType<SetVerificationCodeInputDto>(partialViewResult.Model);
//            Assert.Equal(userId, model.UserId);
//            Assert.Equal(userDto.FullName, model.UserName);
//        }

//        [Fact]
//        public async Task SetVerificationCodeModal_UserNotFound_ShouldReturnErrorContent()
//        {
//            // Arrange
//            int userId = 99;
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(userId)).ReturnsAsync((AdminUserListItemDto?)null);

//            // Act
//            var result = await _controller.SetVerificationCodeModal(userId);

//            // Assert
//            var contentResult = Assert.IsType<ContentResult>(result);
//            Assert.Contains("User not found", contentResult.Content);
//        }

//        [Fact]
//        public async Task SetVerificationCode_ValidModel_ServiceSucceeds_ShouldReturnEmptyContentWithSuccessTrigger()
//        {
//            // Arrange
//            var input = new SetVerificationCodeInputDto { UserId = 1, NewVerificationCode = "1234", ConfirmNewVerificationCode = "1234" };
//            // User needed for re-rendering modal on failure (though not expected here)
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(input.UserId)).ReturnsAsync(new AdminUserListItemDto { UserId = input.UserId });
//            _mockAdminUserService.Setup(s => s.SetUserVerificationCodeAsync(input.UserId, input.NewVerificationCode)).ReturnsAsync(true);
//            _controller.ModelState.Clear(); // Ensure valid model state

//            // Act
//            var result = await _controller.SetVerificationCode(input);

//            // Assert
//            var contentResult = Assert.IsType<ContentResult>(result);
//            Assert.Empty(contentResult.Content); // Expecting empty content, feedback via HX-Trigger
//            Assert.True(_controller.Response.Headers.ContainsKey("HX-Trigger-After-Swap"));
//            var triggerValue = _controller.Response.Headers["HX-Trigger-After-Swap"].ToString();
//            Assert.Contains("showToastSuccess", triggerValue);
//            Assert.Contains("closeSetCodeModal", triggerValue);
//        }

//        [Fact]
//        public async Task SetVerificationCode_InvalidModelState_ShouldReRenderModalPartial()
//        {
//            // Arrange
//            var input = new SetVerificationCodeInputDto { UserId = 1, NewVerificationCode = "123", ConfirmNewVerificationCode = "1234" }; // Code too short, no match
//                                                                                                                                          // User needed for re-rendering modal
//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(input.UserId)).ReturnsAsync(new AdminUserListItemDto { UserId = input.UserId, FullName = "Test User" });
//            _controller.ModelState.AddModelError(nameof(input.NewVerificationCode), "Verification code must be 4 to 6 digits.");
//            _controller.ModelState.AddModelError(nameof(input.ConfirmNewVerificationCode), "Verification code and confirmation code do not match.");

//            // Act
//            var result = await _controller.SetVerificationCode(input);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_SetUserVerificationCodeModalPartial", partialViewResult.ViewName);
//            Assert.Same(input, partialViewResult.Model); // Returns the input model with errors
//            Assert.False(_controller.ModelState.IsValid);
//        }

//        [Fact]
//        public async Task SetVerificationCode_ServiceFails_ShouldReRenderModalPartialWithErrorData()
//        {
//            // Arrange
//            var input = new SetVerificationCodeInputDto { UserId = 1, NewVerificationCode = "1234", ConfirmNewVerificationCode = "1234" };
//            var userForModal = new AdminUserListItemDto { UserId = input.UserId, FullName = "Test User" };

//            _mockAdminUserService.Setup(s => s.GetUserForAdminByIdAsync(input.UserId)).ReturnsAsync(userForModal);
//            _mockAdminUserService.Setup(s => s.SetUserVerificationCodeAsync(input.UserId, input.NewVerificationCode))
//                                 .ReturnsAsync(false); // Simulate service failure
//            _controller.ModelState.Clear();

//            // Act
//            var result = await _controller.SetVerificationCode(input);

//            // Assert
//            var partialViewResult = Assert.IsType<PartialViewResult>(result);
//            Assert.Equal("_SetUserVerificationCodeModalPartial", partialViewResult.ViewName);
//            var model = Assert.IsType<SetVerificationCodeInputDto>(partialViewResult.Model);
//            Assert.Equal(userForModal.FullName, model.UserName); // Ensure username was repopulated in the DTO passed to the view
//            Assert.NotNull(partialViewResult.ViewData["SetCodeErrorMessage"]);
//            Assert.Contains("Failed to set verification code", partialViewResult.ViewData["SetCodeErrorMessage"]!.ToString());

//            // In this specific failure path, the controller re-renders the modal with an inline error message
//            // via ViewData, and does NOT set an HX-Trigger for a toast.
//            Assert.False(_controller.Response.Headers.ContainsKey("HX-Trigger-After-Swap"));
//        }
//    }
//}