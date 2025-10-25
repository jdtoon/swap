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
    /// Controller for administrative management of Departments.
    /// </summary>
    [Authorize(Roles = "SystemAdmin")]
    public class DepartmentAdminController : Controller
    {
        private readonly IDepartmentAdminService _departmentAdminService;
        private readonly IFacilityAdminService _facilityAdminService;
        private readonly ICurrentFacilityContext _facilityContext;
        private readonly ILogger<DepartmentAdminController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepartmentAdminController"/> class.
        /// </summary>
        /// <param name="departmentAdminService">The service for department administration.</param>
        /// <param name="facilityAdminService">The service for facility administration (for dropdowns).</param>
        /// <param name="logger">The logger for this controller.</param>
        public DepartmentAdminController(
            IDepartmentAdminService departmentAdminService,
            IFacilityAdminService facilityAdminService,
            ILogger<DepartmentAdminController> logger,
            ICurrentFacilityContext facilityContext)
        {
            _departmentAdminService = departmentAdminService ?? throw new ArgumentNullException(nameof(departmentAdminService));
            _facilityAdminService = facilityAdminService ?? throw new ArgumentNullException(nameof(facilityAdminService));
            _facilityContext = facilityContext ?? throw new ArgumentNullException(nameof(facilityContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /DepartmentAdmin/Index/{facilityId}
        /// Displays the list of departments for a specific facility in the admin panel.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to show departments for.</param>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A partial view displaying the department list.</returns>
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Controller: DepartmentAdmin/Index requested for Facility ID: {FacilityId} with options: {@Options}", _facilityContext.CurrentFacilityId, options);
            if (_facilityContext.CurrentFacilityId <= 0)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid Facility ID provided.\"}");
                return Content("<div class='alert alert-error'>Invalid Facility ID.</div>", "text/html");
            }
            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 10;

            var viewModel = await _departmentAdminService.GetDepartmentsViewModelAsync(_facilityContext.CurrentFacilityId, options);
            viewModel.PaginationInfo.HxGetUrl = Url.Action("Index", "DepartmentAdmin") ?? "";
            viewModel.PaginationInfo.HxTarget = "#department-list-container";
            viewModel.PaginationInfo.HxSwap = "innerHTML";
            return PartialView("_DepartmentListPartial", viewModel);
        }

        /// <summary>
        /// GET: /DepartmentAdmin/CreateEditModal/{facilityId}/{departmentId?}
        /// Returns a partial view for a modal to create or edit a department.
        /// </summary>
        /// <param name="facilityId">The ID of the facility the department belongs to.</param>
        /// <param name="departmentId">Optional ID of the department to edit. If null, a new department is created.</param>
        /// <returns>A partial view for the create/edit modal.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateEditModal(int facilityId, int? departmentId)
        {
            _logger.LogInformation("Controller: Fetching CreateEditModal for Department ID: {DepartmentId} in Facility ID: {FacilityId}", departmentId, facilityId);
            CreateUpdateDepartmentDto model;
            string targetFacilityName = "Unknown";

            var facility = await _facilityAdminService.GetFacilityByIdAsync(facilityId);
            if (facility == null)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Facility not found.\"}");
                return Content("", "text/html");
            }
            targetFacilityName = facility.Name;

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                var department = await _departmentAdminService.GetDepartmentByIdAsync(departmentId.Value);
                if (department == null || department.FacilityId != facilityId) // Ensure department belongs to target facility
                {
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Department not found or does not belong to facility.\"}");
                    return Content("", "text/html");
                }
                model = new CreateUpdateDepartmentDto
                {
                    DepartmentId = department.DepartmentId,
                    FacilityId = department.FacilityId,
                    Name = department.Name,
                    Description = department.Description,
                    IsActive = department.IsActive
                };
            }
            else
            {
                model = new CreateUpdateDepartmentDto { FacilityId = facilityId }; // New department
            }
            ViewData["TargetFacilityName"] = targetFacilityName;
            return PartialView("_CreateEditDepartmentPartial", model);
        }

        /// <summary>
        /// POST: /DepartmentAdmin/CreateOrUpdate
        /// Handles the creation or update of a department.
        /// </summary>
        /// <param name="dto">The DTO containing department data.</param>
        /// <returns>Refreshes the department list or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrUpdate([FromForm] CreateUpdateDepartmentDto dto)
        {
            _logger.LogInformation("Controller: CreateOrUpdate Department ID: {DepartmentId} (Name: {Name}) in Facility ID: {FacilityId}", dto.DepartmentId, dto.Name, dto.FacilityId);

            var facility = await _facilityAdminService.GetFacilityByIdAsync(dto.FacilityId);
            if (facility == null)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Target facility not found.\"}");
                return Content("", "text/html");
            }
            ViewData["TargetFacilityName"] = facility.Name; // For re-rendering modal with errors

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for Department ID: {DepartmentId}.", dto.DepartmentId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_CreateEditDepartmentPartial", dto);
            }

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            bool success;
            string successMessage;
            if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
            {
                success = await _departmentAdminService.UpdateDepartmentAsync(dto, adminUserId);
                successMessage = "Department updated successfully!";
            }
            else
            {
                success = await _departmentAdminService.CreateDepartmentAsync(dto, adminUserId);
                successMessage = "Department created successfully!";
            }

            if (success)
            {
                _logger.LogInformation("Controller: Department ID {DepartmentId} {Action} successfully.", dto.DepartmentId, dto.DepartmentId.HasValue ? "updated" : "created");
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"{successMessage}\", \"closeAdminGenericModal\": true, \"refreshDepartmentList\": true }}");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to {Action} department ID {DepartmentId}.", dto.DepartmentId.HasValue ? "update" : "create", dto.DepartmentId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save department. Check for duplicate name or system error.\"}");
                ModelState.AddModelError("", "Failed to save department. Name might be a duplicate within the facility, or a system error occurred.");
                return PartialView("_CreateEditDepartmentPartial", dto);
            }
        }

        /// <summary>
        /// POST: /DepartmentAdmin/Deactivate
        /// Deactivates a department.
        /// </summary>
        /// <param name="departmentId">The ID of the department to deactivate.</param>
        /// <returns>Refreshes the department list or returns error.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int departmentId)
        {
            _logger.LogInformation("Controller: Deactivating Department ID: {DepartmentId}", departmentId);

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            bool success = await _departmentAdminService.DeactivateDepartmentAsync(departmentId, adminUserId);

            if (success)
            {
                _logger.LogInformation("Controller: Department ID {DepartmentId} deactivated successfully.", departmentId);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"Department deactivated!\", \"refreshDepartmentList\": true }");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to deactivate Department ID: {DepartmentId}", departmentId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to deactivate department. It might be already inactive or has active dependencies.\"}");
                return Content("", "text/html");
            }
        }
    }
}