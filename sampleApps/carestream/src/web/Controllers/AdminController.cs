using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.dtos.admin;
using System.Security.Claims;
using carestream.core.interfaces.repositories;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for administrative functions, requires SystemAdmin role.
    /// </summary>
    [Authorize(Roles = "SystemAdmin")]
    public class AdminController : Controller
    {
        private readonly IAdminUserService _adminUserService;
        private readonly ILogger<AdminController> _logger;
        private readonly IFacilityRepository _facilityRepository;

        public AdminController(
            IAdminUserService adminUserService, 
            ILogger<AdminController> logger,
            IFacilityRepository facilityRepository)
        {
            _adminUserService = adminUserService ?? throw new ArgumentNullException(nameof(adminUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _facilityRepository = facilityRepository ?? throw new ArgumentNullException(nameof(facilityRepository));
        }

        /// <summary>
        /// GET: /Admin/Index or /Admin
        /// Displays the main admin page, initially focused on user management.
        /// This action loads the main layout for admin, which then might load user list via HTMX.
        /// </summary>
        /// <returns>A partial view for the main admin index.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Admin Index page requested.");
            return PartialView();
        }

        /// <summary>
        /// GET: /Admin/TogglePartial
        /// Returns the partial view for the admin sub-navigation (tabs).
        /// </summary>
        /// <param name="currentViewType">The currently active admin tab.</param>
        /// <returns>A partial view with the admin sub-navigation.</returns>
        [HttpGet]
        public IActionResult TogglePartial([FromQuery] string currentViewType = "users")
        {
            _logger.LogInformation("Controller: Admin/TogglePartial called with currentViewType: {CurrentViewType}", currentViewType);
            ViewData["ActiveAdminTab"] = currentViewType;
            return PartialView("_AdminSubNavPartial");
        }


        /// <summary>
        /// GET: /Admin/UserListPartial
        /// Fetches and returns the partial view for the user list.
        /// </summary>
        /// <param name="searchTerm">Optional search term for users.</param>
        /// <param name="pageNumber">Current page number for pagination.</param>
        /// <param name="pageSize">Number of users per page.</param>
        /// <returns>A partial view displaying the user list.</returns>
        [HttpGet]
        public async Task<IActionResult> UserListPartial(string? searchTerm, int pageNumber = 1, int pageSize = 25)
        {
            _logger.LogInformation("Fetching user list partial for admin. Search: '{SearchTerm}', Page: {Page}, Size: {Size}", searchTerm, pageNumber, pageSize);
            var viewModel = await _adminUserService.GetUserManagementViewModelAsync(searchTerm, pageNumber, pageSize);
            return PartialView("_UserListPartial", viewModel);
        }

        /// <summary>
        /// GET: /Admin/LinkUserModal
        /// Returns a partial view for a modal/form to link a Logto sub ID to a Carestream user.
        /// </summary>
        /// <param name="userId">The ID of the Carestream user to link.</param>
        /// <returns>A partial view for the link user modal.</returns>
        [HttpGet]
        public async Task<IActionResult> LinkUserModal(int userId)
        {
            _logger.LogInformation("Fetching link user modal for UserId: {UserId}", userId);
            if (userId <= 0) return BadRequest("Invalid User ID.");

            var user = await _adminUserService.GetUserForAdminByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for LinkUserModal, UserId: {UserId}", userId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User not found.\"}");
                return Content("", "text/html");
            }
            return PartialView("_LinkUserModalPartial", user);
        }

        /// <summary>
        /// POST: /Admin/LinkUser
        /// Links a Logto subject ID to a Carestream user.
        /// </summary>
        /// <param name="userId">The ID of the Carestream user.</param>
        /// <param name="logtoSub">The Logto subject ID to link.</param>
        /// <returns>A partial view (typically the updated user list) or an error response.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkUser(int userId, string logtoSub)
        {
            _logger.LogInformation("Attempting to link UserId {UserId} with LogtoSub {LogtoSub}", userId, logtoSub);
            if (userId <= 0 || string.IsNullOrWhiteSpace(logtoSub))
            {
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastError\": \"User ID and Logto ID are required.\"}");
                return PartialView("_LinkUserModalPartial", await _adminUserService.GetUserForAdminByIdAsync(userId));
            }

            bool success = await _adminUserService.LinkUserToLogtoAsync(userId, logtoSub);

            if (success)
            {
                _logger.LogInformation("Successfully linked UserId {UserId} with LogtoSub {LogtoSub}", userId, logtoSub);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"User successfully linked!\", \"closeAdminGenericModal\": true }");
                var viewModel = await _adminUserService.GetUserManagementViewModelAsync();
                return PartialView("_UserListPartial", viewModel);
            }
            else
            {
                _logger.LogWarning("Failed to link UserId {UserId} with LogtoSub {LogtoSub}", userId, logtoSub);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastError\": \"Failed to link user. The Logto ID might already be in use or user not found.\"}");
                var userToRelink = await _adminUserService.GetUserForAdminByIdAsync(userId);
                ViewData["LinkErrorMessage"] = "Failed to link user. Logto ID might be in use.";
                return PartialView("_LinkUserModalPartial", userToRelink);
            }
        }

        /// <summary>
        /// GET: /Admin/SetVerificationCodeModal
        /// Returns a partial view for a modal to set/reset a user's verification code.
        /// </summary>
        /// <param name="userId">The ID of the Carestream user.</param>
        /// <returns>A partial view for the set verification code modal.</returns>
        [HttpGet]
        public async Task<IActionResult> SetVerificationCodeModal(int userId)
        {
            _logger.LogInformation("Fetching set verification code modal for UserId: {UserId}", userId);
            if (userId <= 0) return BadRequest("Invalid User ID.");

            var user = await _adminUserService.GetUserForAdminByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for SetVerificationCodeModal, UserId: {UserId}", userId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User not found.\"}");
                return Content("", "text/html");
            }

            var model = new SetVerificationCodeInputDto
            {
                UserId = user.UserId,
                UserName = user.FullName
            };
            return PartialView("_SetUserVerificationCodeModalPartial", model);
        }

        /// <summary>
        /// POST: /Admin/SetVerificationCode
        /// Sets or resets a user's verification code.
        /// </summary>
        /// <param name="input">DTO containing user ID and new verification code.</param>
        /// <returns>An empty OK result with HTMX triggers for toast and modal close, or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetVerificationCode([FromForm] SetVerificationCodeInputDto input)
        {
            _logger.LogInformation("Attempting to set verification code for UserId: {UserId}", input.UserId);

            var userForModal = await _adminUserService.GetUserForAdminByIdAsync(input.UserId);
            if (userForModal == null)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User not found.\"}");
                return Ok();
            }
            input.UserName = userForModal.FullName;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for SetVerificationCode, UserId: {UserId}. Errors: {Errors}",
                    input.UserId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                Response.Headers.Append("HX-Retarget", $"#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_SetUserVerificationCodeModalPartial", input);
            }

            bool success = await _adminUserService.SetUserVerificationCodeAsync(input.UserId, input.NewVerificationCode);

            if (success)
            {
                _logger.LogInformation("Successfully set verification code for UserId: {UserId}", input.UserId);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"Verification code set successfully!\", \"closeAdminGenericModal\": true }");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Failed to set verification code for UserId: {UserId}", input.UserId);
                ViewData["SetCodeErrorMessage"] = "Failed to set verification code. Please try again.";
                Response.Headers.Append("HX-Retarget", $"#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to set code.\"}");
                return PartialView("_SetUserVerificationCodeModalPartial", input);
            }
        }

        /// <summary>
        /// GET: /Admin/UserDetailModal/{userId}
        /// Returns a partial view for a modal displaying comprehensive user details for admin.
        /// </summary>
        /// <param name="userId">The ID of the user to display details for.</param>
        /// <returns>A partial view for the user detail modal.</returns>
        [HttpGet]
        public async Task<IActionResult> UserDetailModal(int userId)
        {
            _logger.LogInformation("Fetching user detail modal for UserId: {UserId}", userId);
            if (userId <= 0) return BadRequest("Invalid User ID.");

            var userDetail = await _adminUserService.GetAdminUserDetailAsync(userId);
            if (userDetail == null)
            {
                _logger.LogWarning("User detail not found for UserId: {UserId}", userId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User details not found.\"}");
                return Content("", "text/html");
            }
            return PartialView("_UserDetailPartial", userDetail);
        }

        /// <summary>
        /// GET: /Admin/EditUserPersonalInfoPartial/{userId}
        /// Returns a partial view for inline editing of a user's personal information.
        /// </summary>
        /// <param name="userId">The ID of the user to edit.</param>
        /// <returns>A partial view with the personal info edit form.</returns>
        [HttpGet]
        public async Task<IActionResult> EditUserPersonalInfoPartial(int userId)
        {
            _logger.LogInformation("Fetching edit user personal info partial for UserId: {UserId}", userId);
            var user = await _adminUserService.GetAdminUserDetailAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for EditUserPersonalInfoPartial, UserId: {UserId}", userId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User not found for editing.\"}");
                return Content("", "text/html");
            }

            var editDto = new AdminUserEditInputDto
            {
                UserId = user.UserId,
                Rank = user.Rank,
                Department = user.Department,
                IsActive = user.IsActive
            };
            return PartialView("_EditUserPersonalInfoPartial", editDto);
        }

        /// <summary>
        /// POST: /Admin/UpdateUserPersonalInfo
        /// Handles the update of a user's personal information from the admin panel.
        /// </summary>
        /// <param name="dto">The DTO containing the updated personal info.</param>
        /// <returns>The updated display partial or the form with validation errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserPersonalInfo([FromForm] AdminUserEditInputDto dto)
        {
            _logger.LogInformation("Attempting to update personal info for UserId: {UserId}", dto.UserId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for UpdateUserPersonalInfo, UserId: {UserId}. Errors: {Errors}",
                    dto.UserId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                Response.Headers.Append("HX-Retarget", $"#user-personal-info-{dto.UserId}");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_EditUserPersonalInfoPartial", dto);
            }

            bool success = await _adminUserService.UpdateAdminUserPersonalInfoAsync(dto);

            if (success)
            {
                _logger.LogInformation("Successfully updated personal info for UserId: {UserId}", dto.UserId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"User personal info updated successfully!\"}");
                var updatedUserDetail = await _adminUserService.GetAdminUserDetailAsync(dto.UserId);
                return PartialView("_UserPersonalInfoDisplayPartial", updatedUserDetail);
            }
            else
            {
                _logger.LogError("Failed to update personal info for UserId: {UserId}", dto.UserId);
                Response.Headers.Append("HX-Retarget", $"#user-personal-info-{dto.UserId}");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to update personal info.\"}");
                ModelState.AddModelError("", "An unexpected error occurred during update.");
                return PartialView("_EditUserPersonalInfoPartial", dto);
            }
        }

        /// <summary>
        /// POST: /Admin/LinkUserToFacility
        /// Links a user to a selected facility from the admin panel.
        /// </summary>
        /// <param name="dto">DTO containing UserId and FacilityId.</param>
        /// <returns>Updates the user's facility list partial or returns error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkUserToFacility([FromForm] AdminUserFacilityLinkInputDto dto)
        {
            _logger.LogInformation("Attempting to link UserId {UserId} to FacilityId {FacilityId}.", dto.UserId, dto.FacilityId);

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                _logger.LogError("Admin: LinkUserToFacility - Admin user ID claim missing/invalid.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for LinkUserToFacility. UserId: {UserId}, FacilityId: {FacilityId}", dto.UserId, dto.FacilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed for linking facility.\"}");
                var userDetail = await _adminUserService.GetAdminUserDetailAsync(dto.UserId); // Needed for re-render
                return PartialView("_UserFacilitiesPartial", userDetail);
            }

            bool success = await _adminUserService.LinkUserToFacilityAsync(dto, adminUserId);

            if (success)
            {
                _logger.LogInformation("Successfully linked UserId {UserId} to FacilityId {FacilityId}.", dto.UserId, dto.FacilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"User linked to facility successfully!\"}");
            }
            else
            {
                _logger.LogError("Failed to link UserId {UserId} to FacilityId {FacilityId}.", dto.UserId, dto.FacilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to link user to facility. It might be already linked.\"}");
            }

            var updatedUserDetail = await _adminUserService.GetAdminUserDetailAsync(dto.UserId);
            return PartialView("_UserFacilitiesPartial", updatedUserDetail);
        }

        /// <summary>
        /// POST: /Admin/UnlinkUserFromFacility
        /// Unlinks a user from a specific facility from the admin panel.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="facilityId">The ID of the facility to unlink from.</param>
        /// <returns>Updates the user's facility list partial or returns error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlinkUserFromFacility(int userId, int facilityId)
        {
            _logger.LogInformation("Attempting to unlink UserId {UserId} from FacilityId {FacilityId}.", userId, facilityId);

            if (userId <= 0 || facilityId <= 0)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid user or facility ID.\"}");
                return Content("", "text/html");
            }

            bool success = await _adminUserService.UnlinkUserFromFacilityAsync(userId, facilityId);

            if (success)
            {
                _logger.LogInformation("Successfully unlinked UserId {UserId} from FacilityId {FacilityId}.", userId, facilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"User unlinked from facility successfully!\"}");
            }
            else
            {
                _logger.LogError("Failed to unlink UserId {UserId} from FacilityId {FacilityId}.", userId, facilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to unlink user from facility. User must be linked to at least one facility.\"}");
            }

            var updatedUserDetail = await _adminUserService.GetAdminUserDetailAsync(userId);
            return PartialView("_UserFacilitiesPartial", updatedUserDetail);
        }

        /// <summary>
        /// POST: /Admin/SetUserDefaultFacility
        /// Sets a specific facility as the default for a user from the admin panel.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="facilityId">The ID of the facility to set as default.</param>
        /// <returns>Updates the user's facility list partial or returns error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserDefaultFacility(int userId, int facilityId)
        {
            _logger.LogInformation("Attempting to set UserId {UserId}'s default facility to {FacilityId}.", userId, facilityId);

            if (userId <= 0 || facilityId <= 0)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid user or facility ID.\"}");
                return Content("", "text/html");
            }

            bool success = await _adminUserService.SetAdminUserDefaultFacilityAsync(userId, facilityId);

            if (success)
            {
                _logger.LogInformation("Successfully set UserId {UserId}'s default facility to {FacilityId}.", userId, facilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"User's default facility updated successfully!\"}");
            }
            else
            {
                _logger.LogError("Failed to set UserId {UserId}'s default facility to {FacilityId}.", userId, facilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to set default facility. Ensure user is linked to this facility.\"}");
            }

            var updatedUserDetail = await _adminUserService.GetAdminUserDetailAsync(userId);
            return PartialView("_UserFacilitiesPartial", updatedUserDetail);
        }

        /// <summary>
        /// GET: /Admin/CreateUserModal
        /// Returns a partial view for a modal/form to create a new Carestream user.
        /// </summary>
        /// <returns>A partial view for the create user modal.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateUserModal()
        {
            _logger.LogInformation("Fetching create user modal.");
            var allActiveFacilities = (await _facilityRepository.GetAllActiveFacilitiesAsync()).ToList();

            var model = new CreateUserInputDto();
            ViewData["AllActiveFacilities"] = allActiveFacilities; // Pass facilities for dropdown
            return PartialView("_CreateUserModalPartial", model);
        }

        /// <summary>
        /// POST: /Admin/CreateUser
        /// Creates a new Carestream user.
        /// </summary>
        /// <param name="dto">DTO containing user creation details.</param>
        /// <returns>A partial view (typically the updated user list) or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([FromForm] CreateUserInputDto dto)
        {
            _logger.LogInformation("Attempting to create new user with ForceNumber: {ForceNumber}", dto.ForceNumber);

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                _logger.LogError("Admin: CreateUser - Admin user ID claim missing/invalid.");
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            // Re-populate facilities for the dropdown in case of model state errors for re-render
            ViewData["AllActiveFacilities"] = (await _facilityRepository.GetAllActiveFacilitiesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for CreateUser, ForceNumber: {ForceNumber}. Errors: {Errors}",
                    dto.ForceNumber, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                Response.Headers.Append("HX-Retarget", $"#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_CreateUserModalPartial", dto);
            }

            int? newUserId = await _adminUserService.CreateUserAsync(dto, adminUserId);

            if (newUserId.HasValue)
            {
                _logger.LogInformation("Successfully created user with UserId: {UserId}", newUserId.Value);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"User created successfully!\", \"closeAdminGenericModal\": true, \"refreshUserList\": true }");
                var viewModel = await _adminUserService.GetUserManagementViewModelAsync(); // Refresh list
                return PartialView("_UserListPartial", viewModel);
            }
            else
            {
                _logger.LogError("Failed to create user with ForceNumber: {ForceNumber}", dto.ForceNumber);
                Response.Headers.Append("HX-Retarget", $"#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to create user. Please check if Force Number is unique or initial facility is valid.\"}");
                ModelState.AddModelError("", "Failed to create user. Force Number might already exist or initial facility is invalid.");
                return PartialView("_CreateUserModalPartial", dto);
            }
        }

        /// <summary>
        /// GET: /Admin/GetUserPersonalInfoDisplayPartial/{userId}
        /// Returns a partial view for displaying a user's personal information (non-editable).
        /// Used after editing or for initial display in modal.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A partial view displaying the personal info.</returns>
        [HttpGet]
        public async Task<IActionResult> GetUserPersonalInfoDisplayPartial(int userId)
        {
            _logger.LogInformation("Fetching user personal info display partial for UserId: {UserId}", userId);
            if (userId <= 0) return BadRequest("Invalid User ID.");

            var user = await _adminUserService.GetAdminUserDetailAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for GetUserPersonalInfoDisplayPartial, UserId: {UserId}", userId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User not found.\"}");
                return Content("", "text/html");
            }
            return PartialView("_UserPersonalInfoDisplayPartial", user);
        }

        /// <summary>
        /// GET: /Admin/GetUserFacilitiesPartial/{userId}
        /// Returns a partial view for displaying and managing a user's linked facilities.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A partial view with the user's facility list and linking options.</returns>
        [HttpGet]
        public async Task<IActionResult> GetUserFacilitiesPartial(int userId)
        {
            _logger.LogInformation("Fetching user facilities partial for UserId: {UserId}", userId);
            if (userId <= 0) return BadRequest("Invalid User ID.");

            var userDetail = await _adminUserService.GetAdminUserDetailAsync(userId);
            if (userDetail == null)
            {
                _logger.LogWarning("User detail not found for GetUserFacilitiesPartial, UserId: {UserId}", userId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User details not found for facilities.\"}");
                return Content("", "text/html");
            }
            return PartialView("_UserFacilitiesPartial", userDetail);
        }
    }
}