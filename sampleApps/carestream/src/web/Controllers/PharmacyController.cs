using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.pharmacy;
using carestream.core.dtos.shared;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.dtos.medication;

namespace carestream.web.controllers
{
    [Authorize(Roles = "Pharmacist")]
    public class PharmacyController : Controller
    {
        private readonly ILogger<PharmacyController> _logger;
        private readonly IPharmacyService _pharmacyService;
        private readonly IUserRepository _userRepository;

        public PharmacyController(
            IPharmacyService pharmacyService,
            ILogger<PharmacyController> logger,
            IUserRepository userRepository)
        {
            _pharmacyService = pharmacyService ?? throw new ArgumentNullException(nameof(pharmacyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        // --- Pharmacist Dashboard Actions (Existing) ---
        public async Task<IActionResult> PharmacistDashboard(int pageNumber = 1, int pageSize = 15)
        {
            _logger.LogInformation("[CONTROLLER] PharmacistDashboard action called. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 15;
            var viewModel = await _pharmacyService.GetDashboardViewModelAsync(pageNumber, pageSize);
            return PartialView(viewModel);
        }

        /// <summary>
        /// GET: /Pharmacy/ViewPrescriptionDetails
        /// Displays the detailed view of a specific prescription identified by visitId.
        /// </summary>
        /// <param name="visitId">The ID of the visit associated with the prescription.</param>
        /// <returns>A partial view with the prescription details, or an error view if not found.</returns>
        [HttpGet]
        public async Task<IActionResult> ViewPrescriptionDetails(int visitId)
        {
            _logger.LogInformation("Controller: Requesting prescription details for VisitId: {VisitId}", visitId);
            if (visitId <= 0)
            {
                _logger.LogWarning("Controller: ViewPrescriptionDetails called with invalid VisitId: {VisitId}", visitId);
                TempData["ErrorMessage"] = "Invalid prescription identifier.";
                return PartialView("~/Views/Shared/_ErrorPartial.cshtml");
            }
            var viewModel = await _pharmacyService.GetPrescriptionDetailsAsync(visitId);
            if (viewModel == null)
            {
                _logger.LogWarning("Controller: No prescription details found by service for VisitId: {VisitId}", visitId);
                TempData["ErrorMessage"] = $"Prescription details not found for Visit ID: {visitId}. It may have been processed or does not exist.";
                return PartialView("~/Views/Shared/_ErrorPartial.cshtml");
            }
            _logger.LogInformation("Controller: Successfully retrieved prescription details for VisitId: {VisitId}. Displaying details view.", visitId);
            return PartialView("_PrescriptionDetails", viewModel);
        }

        /// <summary>
        /// GET: /Pharmacy/StartDispensing
        /// Displays the form for dispensing medications for a given visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit whose prescription is to be dispensed.</param>
        /// <returns>A partial view with the dispense medications form.</returns>
        public async Task<IActionResult> StartDispensing(int visitId)
        {
            _logger.LogInformation("Controller: Requesting to start dispensing for VisitId: {VisitId}", visitId);
            if (visitId <= 0)
            {
                _logger.LogWarning("Controller: StartDispensing called with invalid VisitId: {VisitId}", visitId);
                TempData["ErrorMessage"] = "Invalid prescription identifier for dispensing.";
                return PartialView("~/Views/Shared/_ErrorPartial.cshtml");
            }
            var viewModel = await _pharmacyService.GetStartDispenseViewModelAsync(visitId);
            if (viewModel == null)
            {
                _logger.LogWarning("Controller: Could not retrieve model for StartDispensing, VisitId: {VisitId}. Prescription may not exist or not be ready.", visitId);
                TempData["ErrorMessage"] = $"Prescription (Visit ID: {visitId}) not found or not ready for dispensing.";
                return PartialView("~/Views/Shared/_ErrorPartial.cshtml");
            }
            if (!viewModel.ItemsToDispense.Any())
            {
                _logger.LogInformation("Controller: No items to dispense for VisitId: {VisitId}. Displaying message.", visitId);
            }
            _logger.LogInformation("Controller: Displaying dispense form for VisitId: {VisitId} with {ItemCount} items.", visitId, viewModel.ItemsToDispense.Count);
            return PartialView("_DispenseMedicationsForm", viewModel);
        }

        /// <summary>
        /// POST: /Pharmacy/ConfirmDispense
        /// Processes the submitted dispense form, logs dispensations, and updates statuses.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDispense([FromForm] StartDispenseViewModel dispenseInput)
        {
            _logger.LogInformation("Controller: ConfirmDispense called for VisitId: {VisitId}", dispenseInput.VisitId);
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: ConfirmDispense called with invalid ModelState for VisitId: {VisitId}. Errors: {Errors}",
                    dispenseInput.VisitId,
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                var errorConfirmation = new DispenseConfirmationDto
                {
                    VisitId = dispenseInput.VisitId,
                    PatientName = dispenseInput.PatientName,
                    PrescriptionIdentifier = dispenseInput.PrescriptionIdentifier,
                    OverallSuccess = false,
                    ErrorMessage = "Invalid data submitted. Please check quantities and verification code (if applicable)."
                };
                Response.Headers.Append("HX-Retarget", "#dispense-form-wrapper");
                Response.Headers.Append("HX-Reswap", "outerHTML");
                return PartialView("_DispenseConfirmation", errorConfirmation);
            }
            var logtoSub = User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(logtoSub))
            {
                return PartialView("_DispenseConfirmation", PrepareErrorConfirmation(dispenseInput, "User session invalid. Cannot process dispense."));
            }
            var pharmacistUserId = await _userRepository.GetUserIdByLogtoSubAsync(logtoSub);
            if (!pharmacistUserId.HasValue)
            {
                return PartialView("_DispenseConfirmation", PrepareErrorConfirmation(dispenseInput, "Pharmacist account not fully configured. Cannot process dispense."));
            }
            var confirmationDto = await _pharmacyService.ProcessDispenseAsync(dispenseInput, pharmacistUserId.Value);
            if (confirmationDto.OverallSuccess && confirmationDto.DispensedItems.All(di => string.IsNullOrEmpty(di.Notes) || di.Notes.Contains("dispensed")))
            {
                _logger.LogInformation("Controller: Dispense processed successfully for VisitId: {VisitId}", dispenseInput.VisitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Dispense actions logged successfully!\"}");
            }
            else
            {
                _logger.LogWarning("Controller: Dispense processing had issues for VisitId: {VisitId}. Error: {Error}", dispenseInput.VisitId, confirmationDto.ErrorMessage);
                string toastMessage = string.IsNullOrWhiteSpace(confirmationDto.ErrorMessage) ?
                                      "Some items had issues during dispense. Review summary." :
                                      confirmationDto.ErrorMessage;
                Response.Headers.Append("HX-Trigger", $"{{\"showToastWarning\": \"{System.Web.HttpUtility.JavaScriptStringEncode(toastMessage)}\"}}");
            }
            return PartialView("_DispenseConfirmation", confirmationDto);
        }

        private DispenseConfirmationDto PrepareErrorConfirmation(StartDispenseViewModel input, string message)
        {
            _logger.LogError("Controller: ConfirmDispense - {ErrorMessage} for VisitId: {VisitId}", message, input?.VisitId);
            return new DispenseConfirmationDto
            {
                VisitId = input?.VisitId ?? 0,
                PatientName = input?.PatientName ?? "N/A",
                PrescriptionIdentifier = input?.PrescriptionIdentifier ?? "N/A",
                OverallSuccess = false,
                ErrorMessage = message
            };
        }

        /// <summary>
        /// GET: /Pharmacy/DispensedHistory
        /// Displays the main page for viewing dispensed medication history,
        /// including filter options and the list of items (which will be loaded via HTMX).
        /// </summary>
        public IActionResult DispensedHistory()
        {
            _logger.LogInformation("Controller: Displaying initial Dispensed History page shell.");
            var viewModel = new DispensedHistoryViewModel
            {
                PaginationInfo = new PaginationDto { PageSize = 25, CurrentPage = 1 }
            };
            return PartialView(viewModel);
        }

        /// <summary>
        /// GET: /Pharmacy/DispensedHistoryList
        /// Fetches and returns the partial view for the list of dispensed items,
        /// supporting filtering and pagination. This is the target for HTMX requests.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DispensedHistoryList([FromQuery] FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Controller: Fetching dispensed history list with options: {@Options}", options);
            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25;
            var viewModel = await _pharmacyService.GetDispensedHistoryViewModelAsync(options);
            viewModel.PaginationInfo.HxGetUrl = Url.Action("DispensedHistoryList", "Pharmacy") ?? "";
            viewModel.PaginationInfo.HxTarget = "#dispensed-history-list-container";
            viewModel.PaginationInfo.HxSwap = "innerHTML";
            return PartialView("_DispensedHistoryList", viewModel);
        }


        /// <summary>
        /// GET: /Pharmacy/Inventory
        /// Displays the main container for the medication inventory list.
        /// </summary>
        /// <returns>A partial view for the medication inventory page.</returns>
        [HttpGet]
        public IActionResult Inventory()
        {
            _logger.LogInformation("Controller: Pharmacy/Inventory requested.");
            return PartialView();
        }

        /// <summary>
        /// GET: /Pharmacy/InventoryListPartial
        /// Fetches and returns the partial view containing the paginated list of medication stock.
        /// Supports filtering and pagination.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A partial view displaying the medication inventory list.</returns>
        [HttpGet]
        public async Task<IActionResult> InventoryListPartial([FromQuery] FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Controller: Fetching InventoryListPartial with options: {@Options}", options);
            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25; // Default page size for inventory

            var viewModel = await _pharmacyService.GetMedicationInventoryViewModelAsync(options);
            viewModel.Pagination.HxGetUrl = Url.Action("InventoryListPartial", "Pharmacy") ?? "";
            viewModel.Pagination.HxTarget = "#inventory-list-container";
            viewModel.Pagination.HxSwap = "innerHTML";

            return PartialView("_InventoryListPartial", viewModel);
        }

        /// <summary>
        /// GET: /Pharmacy/AdjustStockModal/{medicationId}
        /// Returns a partial view for a modal to adjust a medication's stock.
        /// </summary>
        /// <param name="medicationId">The ID of the medication to adjust stock for.</param>
        /// <returns>A partial view for the adjust stock modal.</returns>
        [HttpGet]
        public async Task<IActionResult> AdjustStockModal(int medicationId)
        {
            _logger.LogInformation("Controller: Fetching AdjustStockModal for MedicationId: {MedicationId}", medicationId);

            if (medicationId <= 0)
            {
                return BadRequest("Invalid Medication ID.");
            }

            var stockDetail = (await _pharmacyService.GetMedicationStockDetailAsync(medicationId));
            if (stockDetail == null)
            {
                _logger.LogWarning("Controller: Medication {MedicationId} not found for AdjustStockModal.", medicationId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Medication not found for stock adjustment.\"}");
                return Content("", "text/html");
            }

            var model = new AdjustStockInputDto
            {
                MedicationId = medicationId,
                MedicationName = stockDetail.Name,
                CurrentQuantity = stockDetail.QuantityOnHand
            };

            return PartialView("_AdjustStockModalPartial", model);
        }

        /// <summary>
        /// POST: /Pharmacy/AdjustStock
        /// Handles the submission of the stock adjustment form.
        /// </summary>
        /// <param name="dto">The DTO containing the stock adjustment data.</param>
        /// <returns>Refreshes the inventory list or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock([FromForm] AdjustStockInputDto dto)
        {
            _logger.LogInformation("Controller: Adjusting stock for MedicationId: {MedicationId}. Quantity: {Quantity}, IsIncrement: {IsIncrement}",
                dto.MedicationId, dto.Quantity, dto.IsIncrement);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for AdjustStock for MedicationId: {MedicationId}.", dto.MedicationId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content"); // Target the modal content for re-render
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_AdjustStockModalPartial", dto);
            }

            var performingUserId = await GetInternalUserId();
            if (!performingUserId.HasValue)
            {
                _logger.LogError("Controller: AdjustStock - Could not identify performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot adjust stock.\"}");
                return Content("", "text/html");
            }

            bool success = await _pharmacyService.AdjustMedicationStockAsync(dto.MedicationId, dto.Quantity, dto.IsIncrement, performingUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Stock adjusted successfully for MedicationId: {MedicationId}.", dto.MedicationId);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"Stock adjusted successfully!\", \"closeAdminGenericModal\": true, \"refreshInventoryList\": true }");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to adjust stock for MedicationId: {MedicationId}.", dto.MedicationId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to adjust stock. Check quantity or item status.\"}");
                ModelState.AddModelError("", "Failed to adjust stock. Ensure quantity is valid and stock isn't negative.");
                return PartialView("_AdjustStockModalPartial", dto);
            }
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