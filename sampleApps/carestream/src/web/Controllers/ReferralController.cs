using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories; // Needed for IUserRepository
using carestream.core.dtos.consultation; // For ReferralDto, CreateUpdateReferralDto
using System.Security.Claims;
using carestream.core.infrastructure; // For List

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for managing patient referrals.
    /// Accessible by Doctor and Nurse roles.
    /// </summary>
    [Authorize(Roles = "Doctor,Nurse")]
    public class ReferralController : Controller
    {
        private readonly IReferralService _referralService;
        private readonly IUserRepository _userRepository; // To get internal user ID for recording actions
        private readonly IFacilityAdminService _facilityAdminService; // NEW: For populating facility dropdown
        private readonly IDepartmentAdminService _departmentAdminService; // NEW: For populating department dropdown
        private readonly ILogger<ReferralController> _logger;
        private readonly ICurrentFacilityContext _facilityContext;

        public ReferralController(
            IReferralService referralService,
            IUserRepository userRepository,
            IFacilityAdminService facilityAdminService, // NEW: Inject
            IDepartmentAdminService departmentAdminService, // NEW: Inject
            ILogger<ReferralController> logger,
            ICurrentFacilityContext facilityContext)
        {
            _referralService = referralService ?? throw new ArgumentNullException(nameof(referralService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _facilityAdminService = facilityAdminService ?? throw new ArgumentNullException(nameof(facilityAdminService)); // NEW: Assign
            _departmentAdminService = departmentAdminService ?? throw new ArgumentNullException(nameof(departmentAdminService)); // NEW: Assign
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _facilityContext = facilityContext ?? throw new ArgumentNullException(nameof(facilityContext));
        }

        /// <summary>
        /// GET: /Referral/Index/{visitId}/{patientId}
        /// Displays the main container for a patient's referrals related to a specific visit.
        /// This is designed to be loaded into a tab within the consultation.
        /// </summary>
        /// <param name="visitId">The ID of the visit to show referrals for.</param>
        /// <param name="patientId">The ID of the patient (for context in the view).</param>
        /// <returns>A partial view for the referral list.</returns>
        [HttpGet]
        public IActionResult Index(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Referral/Index requested for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Controller: Referral/Index called with invalid VisitId ({VisitId}) or PatientId ({PatientId}).", visitId, patientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid Visit or Patient ID for referrals.\"}");
                return Content("", "text/html");
            }

            ViewData["VisitId"] = visitId; // Pass to partial for form context
            ViewData["PatientId"] = patientId; // Pass to partial for form context
            return PartialView(); // Renders Views/Referral/Index.cshtml
        }

        /// <summary>
        /// GET: /Referral/ReferralListPartial/{visitId}/{patientId}
        /// Fetches and returns the partial view containing the list of referral entries for a visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view displaying the list of referrals.</returns>
        [HttpGet]
        public async Task<IActionResult> ReferralListPartial(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Fetching ReferralListPartial for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);
            if (visitId <= 0 || patientId <= 0)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid Visit or Patient ID for referral list.\"}");
                return Content("", "text/html");
            }

            var referrals = await _referralService.GetReferralsForVisitAsync(visitId);
            ViewData["VisitId"] = visitId;
            ViewData["PatientId"] = patientId;
            return PartialView("_ReferralListPartial", referrals);
        }

        /// <summary>
        /// GET: /Referral/CreateReferralModal/{visitId}/{patientId}
        /// Returns a partial view for a modal to create a new referral.
        /// </summary>
        /// <param name="visitId">The ID of the current visit.</param>
        /// <param name="patientId">The ID of the patient for the referral.</param>
        /// <returns>A partial view for the create referral modal.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateReferralModal(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Fetching CreateReferralModal for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            if (visitId <= 0 || patientId <= 0)
            {
                return BadRequest("Invalid Visit or Patient ID.");
            }

            // Get current user ID for ReferredByUserId
            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Controller: CreateReferralModal - Could not identify referring user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot create referral.\"}");
                return Content("", "text/html");
            }

            // Fetch facilities and departments for dropdowns
            var allFacilities = (await _facilityAdminService.GetFacilitiesViewModelAsync(new carestream.core.dtos.shared.FilterAndPaginationOptions { PageSize = int.MaxValue })).Facilities.ToList();
            // Assuming that a doctor/nurse can only refer to departments within their accessible facilities, or all active facilities
            // For simplicity, fetching all for now, filter by context if needed.
            var allDepartments = (await _departmentAdminService.GetAllDepartmentsForFacilityAsync(_facilityContext.CurrentFacilityId)).ToList();


            var model = new CreateUpdateReferralDto
            {
                VisitId = visitId,
                PatientId = patientId,
                ReferredByUserId = currentUserId.Value // Set the referring user
            };

            ViewData["AllFacilities"] = allFacilities; // Pass for dropdown
            ViewData["AllDepartments"] = allDepartments; // Pass for dropdown
            ViewData["VisitId"] = visitId; // Pass to modal for context
            ViewData["PatientId"] = patientId; // Pass to modal for context
            return PartialView("_CreateReferralModalPartial", model);
        }

        /// <summary>
        /// POST: /Referral/CreateReferral
        /// Handles the creation of a new referral.
        /// </summary>
        /// <param name="dto">The DTO containing the referral data.</param>
        /// <returns>Refreshes the referral list or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReferral([FromForm] CreateUpdateReferralDto dto)
        {
            _logger.LogInformation("Controller: Creating new referral for VisitId: {VisitId}, PatientId: {PatientId}", dto.VisitId, dto.PatientId);

            // Re-populate dropdown data for ModelState re-render
            ViewData["AllFacilities"] = (await _facilityAdminService.GetFacilitiesViewModelAsync(new carestream.core.dtos.shared.FilterAndPaginationOptions { PageSize = int.MaxValue })).Facilities.ToList();
            ViewData["AllDepartments"] = (await _departmentAdminService.GetAllDepartmentsForFacilityAsync(_facilityContext.CurrentFacilityId)).ToList();
            ViewData["VisitId"] = dto.VisitId;
            ViewData["PatientId"] = dto.PatientId;


            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue || currentUserId.Value != dto.ReferredByUserId) // Ensure submitted ReferredByUserId matches current user for security
            {
                _logger.LogError("Controller: CreateReferral - User identity mismatch or invalid. CurrentUserId: {CurrentUserId}, DTO.ReferredByUserId: {DtoReferredByUserId}", currentUserId, dto.ReferredByUserId);
                ModelState.AddModelError(string.Empty, "User session invalid or identity mismatch. Cannot create referral.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for referral creation for VisitId: {VisitId}, PatientId: {PatientId}.", dto.VisitId, dto.PatientId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content"); // Assuming generic modal
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_CreateReferralModalPartial", dto);
            }

            int newReferralId = await _referralService.CreateReferralAsync(dto);

            if (newReferralId > 0)
            {
                _logger.LogInformation("Controller: Referral {ReferralId} created successfully for VisitId {VisitId}.", newReferralId, dto.VisitId);
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"Referral created successfully!\", \"closeAdminGenericModal\": true, \"refreshReferralList\": {{ \"visitId\": {dto.VisitId}, \"patientId\": {dto.PatientId} }} }}");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to create referral for VisitId: {VisitId}.", dto.VisitId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to create referral. Please try again.\"}");
                ModelState.AddModelError("", "Failed to create referral due to a system error.");
                return PartialView("_CreateReferralModalPartial", dto);
            }
        }

        /// <summary>
        /// POST: /Referral/UpdateStatus
        /// Updates the status of a referral.
        /// </summary>
        /// <param name="referralId">The ID of the referral to update.</param>
        /// <param name="newStatus">The new status string (e.g., "Accepted", "Completed").</param>
        /// <param name="visitId">The ID of the visit (for refresh context).</param>
        /// <param name="patientId">The ID of the patient (for refresh context).</param>
        /// <returns>Triggers a refresh of the referral list and a toast.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int referralId, string newStatus, int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Updating referral ID: {ReferralId} status to '{NewStatus}' for VisitId: {VisitId}", referralId, newStatus, visitId);

            if (referralId <= 0 || visitId <= 0 || patientId <= 0 || string.IsNullOrWhiteSpace(newStatus))
            {
                _logger.LogWarning("Controller: UpdateStatus called with invalid input. ReferralId: {ReferralId}, NewStatus: '{NewStatus}', VisitId: {VisitId}, PatientId: {PatientId}", referralId, newStatus, visitId, patientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid input for status update.\"}");
                return Ok();
            }

            var performingUserId = await GetInternalUserId();
            if (!performingUserId.HasValue)
            {
                _logger.LogError("Controller: UpdateStatus - Could not identify performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot update referral status.\"}");
                return Ok();
            }

            bool success = await _referralService.UpdateReferralStatusAsync(referralId, newStatus, performingUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Referral ID {ReferralId} status updated to '{NewStatus}' successfully.", referralId, newStatus);
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"Referral status updated!\", \"refreshReferralList\": {{ \"visitId\": {visitId}, \"patientId\": {patientId} }} }}");
            }
            else
            {
                _logger.LogError("Controller: Failed to update referral ID: {ReferralId} status to '{NewStatus}'.", referralId, newStatus);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to update referral status. Please try again.\"}");
            }
            return Ok();
        }

        // Helper to get the internal user ID from claims (copied from other controllers)
        private async Task<int?> GetInternalUserId()
        {
            var userIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                var logtoSub = User.FindFirstValue("sub");
                if (!string.IsNullOrEmpty(logtoSub))
                {
                    return await _userRepository.GetUserIdByLogtoSubAsync(logtoSub);
                }
                return null;
            }
            return userId;
        }
    }
}