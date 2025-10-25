using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.dtos.admin.facility;
using carestream.core.dtos.shared;
using System.Security.Claims;
using carestream.core.infrastructure;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for administrative management of Wards.
    /// </summary>
    [Authorize(Roles = "SystemAdmin")]
    public class WardAdminController : Controller
    {
        private readonly IWardAdminService _wardAdminService;
        private readonly IFacilityAdminService _facilityAdminService;
        private readonly IDepartmentAdminService _departmentAdminService;
        private readonly ILogger<WardAdminController> _logger;
        private readonly ICurrentFacilityContext _facilityContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="WardAdminController"/> class.
        /// </summary>
        /// <param name="wardAdminService">The service for ward administration.</param>
        /// <param name="facilityAdminService">The service for facility administration (for dropdowns).</param>
        /// <param name="departmentAdminService">The service for department administration (for dropdowns).</param>
        /// <param name="logger">The logger for this controller.</param>
        public WardAdminController(
            IWardAdminService wardAdminService,
            IFacilityAdminService facilityAdminService,
            IDepartmentAdminService departmentAdminService,
            ILogger<WardAdminController> logger,
            ICurrentFacilityContext facilityContext)
        {
            _wardAdminService = wardAdminService ?? throw new ArgumentNullException(nameof(wardAdminService));
            _facilityAdminService = facilityAdminService ?? throw new ArgumentNullException(nameof(facilityAdminService));
            _departmentAdminService = departmentAdminService ?? throw new ArgumentNullException(nameof(departmentAdminService));
            _facilityContext = facilityContext ?? throw new ArgumentNullException(nameof(facilityContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /WardAdmin/Index/{facilityId?}
        /// Displays the list of wards for a specific facility (or all) in the admin panel.
        /// </summary>
        /// <param name="facilityId">Optional ID of the facility to show wards for. If null, might show all wards or require selection.</param>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A partial view displaying the ward list.</returns>
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FilterAndPaginationOptions options)
        {
            if (_facilityContext.CurrentFacilityId <= 0)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Please select a Facility to view Wards.\"}");
                return PartialView("_WardListPartial", new WardListViewModel { AllFacilities = (await _facilityAdminService.GetFacilitiesViewModelAsync(new FilterAndPaginationOptions { PageSize = int.MaxValue })).Facilities.ToList() });
            }

            _logger.LogInformation("Controller: WardAdmin/Index requested for Facility ID: {FacilityId} with options: {@Options}", _facilityContext.CurrentFacilityId, options);
            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 10;

            var viewModel = await _wardAdminService.GetWardsViewModelAsync(_facilityContext.CurrentFacilityId, options);
            viewModel.PaginationInfo.HxGetUrl = Url.Action("Index", "WardAdmin") ?? "";
            viewModel.PaginationInfo.HxTarget = "#ward-list-container";
            viewModel.PaginationInfo.HxSwap = "innerHTML";
            return PartialView("_WardListPartial", viewModel);
        }

        /// <summary>
        /// GET: /WardAdmin/CreateEditModal/{facilityId}/{wardId?}
        /// Returns a partial view for a modal to create or edit a ward.
        /// </summary>
        /// <param name="facilityId">The ID of the facility the ward belongs to.</param>
        /// <param name="wardId">Optional ID of the ward to edit. If null, a new ward is created.</param>
        /// <returns>A partial view for the create/edit modal.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateEditModal(int facilityId, int? wardId)
        {
            _logger.LogInformation("Controller: Fetching CreateEditModal for Ward ID: {WardId} in Facility ID: {FacilityId}", wardId, facilityId);
            CreateUpdateWardDto model;
            string targetFacilityName = "Unknown";
            IEnumerable<DepartmentDto> departmentsForDropdown = Enumerable.Empty<DepartmentDto>();

            var facility = await _facilityAdminService.GetFacilityByIdAsync(facilityId);
            if (facility == null)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Facility not found.\"}");
                return Content("", "text/html");
            }
            targetFacilityName = facility.Name;
            departmentsForDropdown = await _departmentAdminService.GetAllDepartmentsForFacilityAsync(facilityId);


            if (wardId.HasValue && wardId.Value > 0)
            {
                var ward = await _wardAdminService.GetWardByIdAsync(wardId.Value);
                if (ward == null || ward.FacilityId != facilityId) // Ensure ward belongs to target facility
                {
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Ward not found or does not belong to facility.\"}");
                    return Content("", "text/html");
                }
                model = new CreateUpdateWardDto
                {
                    WardId = ward.WardId,
                    FacilityId = ward.FacilityId,
                    DepartmentId = ward.DepartmentId,
                    Name = ward.Name,
                    Description = ward.Description,
                    IsActive = ward.IsActive
                };
            }
            else
            {
                model = new CreateUpdateWardDto { FacilityId = facilityId }; // New ward
            }
            ViewData["TargetFacilityName"] = targetFacilityName;
            ViewData["DepartmentsForDropdown"] = departmentsForDropdown;
            return PartialView("_CreateEditWardPartial", model);
        }

        /// <summary>
        /// POST: /WardAdmin/CreateOrUpdate
        /// Handles the creation or update of a ward.
        /// </summary>
        /// <param name="dto">The DTO containing ward data.</param>
        /// <returns>Refreshes the ward list or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrUpdate([FromForm] CreateUpdateWardDto dto)
        {
            _logger.LogInformation("Controller: CreateOrUpdate Ward ID: {WardId} (Name: {Name}) in Facility ID: {FacilityId}", dto.WardId, dto.Name, dto.FacilityId);

            var facility = await _facilityAdminService.GetFacilityByIdAsync(dto.FacilityId);
            if (facility == null)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Target facility not found.\"}");
                return Content("", "text/html");
            }
            ViewData["TargetFacilityName"] = facility.Name;
            ViewData["DepartmentsForDropdown"] = await _departmentAdminService.GetAllDepartmentsForFacilityAsync(dto.FacilityId); // For re-rendering modal with errors

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for Ward ID: {WardId}.", dto.WardId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_CreateEditWardPartial", dto);
            }

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            bool success;
            string successMessage;
            if (dto.WardId.HasValue && dto.WardId.Value > 0)
            {
                success = await _wardAdminService.UpdateWardAsync(dto, adminUserId);
                successMessage = "Ward updated successfully!";
            }
            else
            {
                success = await _wardAdminService.CreateWardAsync(dto, adminUserId);
                successMessage = "Ward created successfully!";
            }

            if (success)
            {
                _logger.LogInformation("Controller: Ward ID {WardId} {Action} successfully.", dto.WardId, dto.WardId.HasValue ? "updated" : "created");
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"{successMessage}\", \"closeAdminGenericModal\": true, \"refreshWardList\": true }}");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to {Action} ward ID {WardId}.", dto.WardId.HasValue ? "update" : "create", dto.WardId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save ward. Check for duplicate name or system error.\"}");
                ModelState.AddModelError("", "Failed to save ward. Name might be a duplicate within the facility/department, or a system error occurred.");
                return PartialView("_CreateEditWardPartial", dto);
            }
        }

        /// <summary>
        /// POST: /WardAdmin/Deactivate
        /// Deactivates a ward.
        /// </summary>
        /// <param name="wardId">The ID of the ward to deactivate.</param>
        /// <returns>Refreshes the ward list or returns error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int wardId)
        {
            _logger.LogInformation("Controller: Deactivating Ward ID: {WardId}", wardId);

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            bool success = await _wardAdminService.DeactivateWardAsync(wardId, adminUserId);

            if (success)
            {
                _logger.LogInformation("Controller: Ward ID {WardId} deactivated successfully.", wardId);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"Ward deactivated!\", \"refreshWardList\": true }");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to deactivate Ward ID: {WardId}", wardId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to deactivate ward. It might be already inactive or has active dependencies.\"}");
                return Content("", "text/html");
            }
        }

        /// <summary>
        /// GET: /WardAdmin/GetDepartmentsByFacilityForDropdown/{facilityId}
        /// Retrieves active departments for a given facility, used for dynamic dropdowns in forms.
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <returns>A partial view containing options for a select element.</returns>
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByFacilityForDropdown(int ReferredToFacilityId)
        {
            _logger.LogInformation("Controller: Fetching departments for dropdown for FacilityId: {FacilityId}", ReferredToFacilityId);
            if (ReferredToFacilityId <= 0)
            {
                return Content("<option value=''>Select Facility First</option>", "text/html");
            }
            var departments = await _departmentAdminService.GetAllDepartmentsForFacilityAsync(ReferredToFacilityId);
            return PartialView("_DepartmentSelectOptions", departments);
        }
    }
}