using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.dtos.admin.facility;
using carestream.core.dtos.shared;
using System.Security.Claims;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for administrative management of Facilities.
    /// </summary>
    [Authorize(Roles = "SystemAdmin")]
    public class FacilityAdminController : Controller
    {
        private readonly IFacilityAdminService _facilityAdminService;
        private readonly ILogger<FacilityAdminController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityAdminController"/> class.
        /// </summary>
        /// <param name="facilityAdminService">The service for facility administration.</param>
        /// <param name="logger">The logger for this controller.</param>
        public FacilityAdminController(
            IFacilityAdminService facilityAdminService,
            ILogger<FacilityAdminController> logger)
        {
            _facilityAdminService = facilityAdminService ?? throw new ArgumentNullException(nameof(facilityAdminService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /FacilityAdmin/Index
        /// Displays the main list of facilities in the admin panel.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A partial view displaying the facility list.</returns>
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Controller: FacilityAdmin/Index requested with options: {@Options}", options);
            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 10;

            var viewModel = await _facilityAdminService.GetFacilitiesViewModelAsync(options);
            viewModel.PaginationInfo.HxGetUrl = Url.Action("Index", "FacilityAdmin") ?? "";
            viewModel.PaginationInfo.HxTarget = "#admin-content-area";
            viewModel.PaginationInfo.HxSwap = "innerHTML";
            return PartialView("_FacilityListPartial", viewModel);
        }

        /// <summary>
        /// GET: /FacilityAdmin/CreateEditModal/{facilityId?}
        /// Returns a partial view for a modal to create or edit a facility.
        /// </summary>
        /// <param name="facilityId">Optional ID of the facility to edit. If null, a new facility is created.</param>
        /// <returns>A partial view for the create/edit modal.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateEditModal(int? facilityId)
        {
            _logger.LogInformation("Controller: Fetching CreateEditModal for Facility ID: {FacilityId}", facilityId);
            CreateUpdateFacilityDto model;

            if (facilityId.HasValue && facilityId.Value > 0)
            {
                var facility = await _facilityAdminService.GetFacilityByIdAsync(facilityId.Value);
                if (facility == null)
                {
                    _logger.LogWarning("Controller: Facility ID {FacilityId} not found for edit modal.", facilityId);
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Facility not found for editing.\"}");
                    return Content("", "text/html");
                }
                model = new CreateUpdateFacilityDto
                {
                    FacilityId = facility.FacilityId,
                    Name = facility.Name,
                    ShortCode = facility.ShortCode,
                    IsActive = facility.IsActive,
                    City = facility.City,
                    AddressLine1 = facility.AddressLine1,
                    AddressLine2 = facility.AddressLine2,
                    Country = facility.Country!,
                    EmailAddress = facility.EmailAddress,
                    PhoneNumber = facility.PhoneNumber,
                    Province = facility.Province
                };
            }
            else
            {
                model = new CreateUpdateFacilityDto(); // New facility
            }
            return PartialView("_CreateEditFacilityPartial", model);
        }

        /// <summary>
        /// POST: /FacilityAdmin/CreateOrUpdate
        /// Handles the creation or update of a facility.
        /// </summary>
        /// <param name="dto">The DTO containing facility data.</param>
        /// <returns>Refreshes the facility list or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrUpdate([FromForm] CreateUpdateFacilityDto dto)
        {
            _logger.LogInformation("Controller: CreateOrUpdate Facility ID: {FacilityId} (Name: {Name})", dto.FacilityId, dto.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for Facility ID: {FacilityId}.", dto.FacilityId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_CreateEditFacilityPartial", dto); // Re-render modal with errors
            }

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                _logger.LogError("Controller: Admin user ID claim missing/invalid for facility create/update.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            bool success;
            string successMessage;
            if (dto.FacilityId.HasValue && dto.FacilityId.Value > 0)
            {
                success = await _facilityAdminService.UpdateFacilityAsync(dto, adminUserId);
                successMessage = "Facility updated successfully!";
            }
            else
            {
                success = await _facilityAdminService.CreateFacilityAsync(dto, adminUserId);
                successMessage = "Facility created successfully!";
            }

            if (success)
            {
                _logger.LogInformation("Controller: Facility ID {FacilityId} {Action} successfully.", dto.FacilityId, dto.FacilityId.HasValue ? "updated" : "created");
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"{successMessage}\", \"closeAdminGenericModal\": true, \"refreshFacilityList\": true }}");
                return Content("", "text/html"); // Success, modal closes and list refreshes
            }
            else
            {
                _logger.LogError("Controller: Failed to {Action} facility ID {FacilityId}.", dto.FacilityId.HasValue ? "update" : "create", dto.FacilityId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save facility. Check for duplicate name/short code or system error.\"}");
                ModelState.AddModelError("", "Failed to save facility. Name or Short Code might be a duplicate, or a system error occurred.");
                return PartialView("_CreateEditFacilityPartial", dto); // Re-render modal with error
            }
        }

        /// <summary>
        /// POST: /FacilityAdmin/Deactivate
        /// Deactivates a facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to deactivate.</param>
        /// <returns>Refreshes the facility list or returns error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int facilityId)
        {
            _logger.LogInformation("Controller: Deactivating Facility ID: {FacilityId}", facilityId);

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            bool success = await _facilityAdminService.DeactivateFacilityAsync(facilityId, adminUserId);

            if (success)
            {
                _logger.LogInformation("Controller: Facility ID {FacilityId} deactivated successfully.", facilityId);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"Facility deactivated!\", \"refreshFacilityList\": true }");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to deactivate Facility ID: {FacilityId}", facilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to deactivate facility. It might be already inactive or has active dependencies.\"}");
                return Content("", "text/html");
            }
        }
    }
}