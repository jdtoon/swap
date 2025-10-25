using Microsoft.Extensions.Logging;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.pharmacy;
using carestream.core.dtos.medication; // Added for MedicationInventoryViewModel
using System.Text.RegularExpressions;
using carestream.core.utilities;
using carestream.core.dtos.shared;

namespace carestream.core.services
{
    public class PharmacyService : IPharmacyService
    {
        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly ILogger<PharmacyService> _logger;
        private readonly IDispensationRepository _dispensationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IMedicationRepository _medicationRepository;

        // Regex:
        // ^\s*      -> Optional leading whitespace
        // (\d+)     -> Group 1: One or more digits (the numeric value)
        // \s*       -> Optional whitespace
        // (         -> Group 2: Start of the unit part (optional)
        //   [a-zA-Z].*?  -> Unit starts with a letter, followed by any characters non-greedily
        // )?        -> Group 2 is optional
        // \s*       -> Optional trailing whitespace
        // (\(.*\))? -> Group 3: Optional parenthesized detail like (100ml) - captured but not primary unit
        // \s*$      -> Optional trailing whitespace to the end of the string
        private static readonly Regex QuantityRegex = new Regex(@"^\s*(\d+)\s*([a-zA-Z][a-zA-Z\s\/\-\%]*?)?\s*(\(.*\))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex JustNumberRegex = new Regex(@"^\s*(\d+)\s*$", RegexOptions.Compiled);

        public PharmacyService(
            IPrescriptionRepository prescriptionRepository,
            ILogger<PharmacyService> logger,
            IDispensationRepository dispensationRepository,
            IUserRepository userRepository,
            IPasswordHasherService passwordHasherService,
            IMedicationRepository medicationRepository)
        {
            _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dispensationRepository = dispensationRepository ?? throw new ArgumentNullException(nameof(dispensationRepository)); // Ensure this is not null
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _passwordHasherService = passwordHasherService ?? throw new ArgumentNullException(nameof(passwordHasherService));
            _medicationRepository = medicationRepository ?? throw new ArgumentNullException(nameof(medicationRepository));
        }

        /// <summary>
        /// Gets the view model data for the Pharmacist Dashboard.
        /// </summary>
        /// <param name="pageNumber">For pagination of pending prescriptions (1-based).</param>
        /// <param name="pageSize">Number of items per page for pending prescriptions.</param>
        /// <returns>The view model for the pharmacist dashboard.</returns>
        public async Task<PharmacistDashboardViewModel> GetDashboardViewModelAsync(int pageNumber = 1, int pageSize = 25)
        {
            _logger.LogInformation("Service: Getting Pharmacist Dashboard ViewModel. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 25; // Or a default from config
            int offset = (pageNumber - 1) * pageSize;

            // Fetch data pieces concurrently
            var statsTask = _prescriptionRepository.GetPharmacistDashboardStatsAsync();
            var pendingPrescriptionsTask = _prescriptionRepository.GetPendingPrescriptionsSummaryAsync(limit: pageSize, offset: offset);

            await Task.WhenAll(statsTask, pendingPrescriptionsTask);

            var viewModel = new PharmacistDashboardViewModel
            {
                Stats = await statsTask ?? new PharmacistDashboardStatsDto(),
                PendingPrescriptions = (await pendingPrescriptionsTask)?.ToList() ?? new List<PendingPrescriptionSummaryDto>()
                // Future: Add pagination info to the ViewModel if we build a reusable pagination component
                // PageNumber = pageNumber,
                // PageSize = pageSize,
                // TotalPendingPrescriptions = stats.PendingPrescriptionsCount (or a separate COUNT(*) query)
            };

            _logger.LogInformation("Service: Pharmacist Dashboard ViewModel assembled. Pending Prescriptions: {PendingCount}", viewModel.PendingPrescriptions.Count);
            return viewModel;
        }

        /// <summary>
        /// Gets the detailed view model for a specific prescription (visit).
        /// </summary>
        /// <param name="visitId">The ID of the visit whose prescription details are to be fetched.</param>
        /// <returns>A view model containing the prescription header and item details, or null if not found.</returns>
        public async Task<ViewPrescriptionViewModel?> GetPrescriptionDetailsAsync(int visitId)
        {
            _logger.LogInformation("Service: Getting prescription details for VisitId: {VisitId}", visitId);

            if (visitId <= 0)
            {
                _logger.LogWarning("Service: GetPrescriptionDetailsAsync called with invalid VisitId: {VisitId}", visitId);
                return null;
            }

            // Fetch header and items concurrently
            var headerTask = _prescriptionRepository.GetPrescriptionDetailHeaderAsync(visitId);
            var itemsTask = _prescriptionRepository.GetPrescriptionDetailItemsAsync(visitId);

            await Task.WhenAll(headerTask, itemsTask);

            var header = await headerTask;
            var items = await itemsTask;

            if (header == null)
            {
                _logger.LogWarning("Service: No prescription header found for VisitId: {VisitId}. Assuming prescription does not exist or is not accessible.", visitId);
                return null; // If no header, no valid prescription to view
            }

            return new ViewPrescriptionViewModel
            {
                Header = header,
                Items = items?.ToList() ?? new List<PrescriptionDetailItemDto>()
            };
        }

        /// <summary>
        /// Gets the view model needed to start the dispensing process for a prescription.
        /// </summary>
        /// <param name="visitId">The ID of the visit whose prescription is to be dispensed.</param>
        /// <returns>The view model for the dispensing screen, or null if prescription not found/ready.</returns>
        public async Task<StartDispenseViewModel?> GetStartDispenseViewModelAsync(int visitId)
        {
            _logger.LogInformation("Service: Getting Start Dispense ViewModel for VisitId: {VisitId}", visitId);

            if (visitId <= 0)
            {
                _logger.LogWarning("Service: GetStartDispenseViewModelAsync called with invalid VisitId: {VisitId}", visitId);
                return null;
            }

            var headerTask = _prescriptionRepository.GetPrescriptionDetailHeaderAsync(visitId);
            var itemsForDispensingTask = _prescriptionRepository.GetItemsForDispensingAsync(visitId); // This returns List<DispenseItemDto>

            await Task.WhenAll(headerTask, itemsForDispensingTask);

            var header = await headerTask;
            var items = (await itemsForDispensingTask)?.ToList(); // Materialize to list to modify

            if (header == null)
            {
                _logger.LogWarning("Service: No prescription header found for VisitId: {VisitId} when preparing dispense model.", visitId);
                return null;
            }

            if (items == null || !items.Any())
            {
                _logger.LogInformation("Service: No items found for dispensing for VisitId: {VisitId}.", visitId);
                return new StartDispenseViewModel
                {
                    VisitId = visitId,
                    PatientName = header.PatientName,
                    PrescriptionIdentifier = header.PrescriptionIdentifier,
                    ItemsToDispense = new List<DispenseItemDto>()
                };
            }

            // Populate StockOnHand for each item
            foreach (var item in items)
            {
                // Default QtyToDispense and IsSelected
                item.QuantityToDispense = item.QuantityPrescribed;
                item.IsSelectedForDispense = true;

                // Fetch stock on hand
                var stock = await _medicationRepository.GetStockOnHandAsync(item.MedicationId);
                item.StockOnHand = stock ?? 0; // Default to 0 if not found or null
            }

            var viewModel = new StartDispenseViewModel
            {
                VisitId = visitId,
                PatientName = header.PatientName,
                PrescriptionIdentifier = header.PrescriptionIdentifier,
                ItemsToDispense = items // Use the now-populated list
            };

            return viewModel;
        }

        /// <summary>
        /// Processes the dispensing of medications based on pharmacist input.
        /// Logs dispense actions and updates prescription item statuses.
        /// </summary>
        /// <param name="dispenseInput">The view model containing items to dispense and quantities.</param>
        /// <param name="pharmacistUserId">The ID of the pharmacist performing the dispense.</param>
        /// <returns>A DTO confirming the dispense actions and any errors.</returns>
        public async Task<DispenseConfirmationDto> ProcessDispenseAsync(StartDispenseViewModel dispenseInput, int pharmacistUserId)
        {
            _logger.LogInformation("Service: Processing dispense for VisitId: {VisitId} by PharmacistId: {PharmacistId}",
                dispenseInput.VisitId, pharmacistUserId);

            var confirmation = new DispenseConfirmationDto
            {
                VisitId = dispenseInput.VisitId,
                PatientName = dispenseInput.PatientName,
                PrescriptionIdentifier = dispenseInput.PrescriptionIdentifier,
                OverallSuccess = true // Assume success initially
            };

            // 1. Pharmacist Verification Code Check
            if (string.IsNullOrWhiteSpace(dispenseInput.PharmacistVerificationCode))
            {
                _logger.LogWarning("Service: Pharmacist verification code not provided for VisitId: {VisitId}.", dispenseInput.VisitId);
                confirmation.OverallSuccess = false;
                confirmation.ErrorMessage = "Pharmacist verification code is required.";
                confirmation.NextStepMessage = "Please enter verification code to proceed.";
                return confirmation;
            }
            var verificationInfo = await _userRepository.GetUserVerificationCodeInfoAsync(pharmacistUserId);
            if (verificationInfo == null || string.IsNullOrEmpty(verificationInfo.HashedVerificationCode) || string.IsNullOrEmpty(verificationInfo.VerificationCodeSalt))
            {
                _logger.LogError("Service: Pharmacist (ID: {PharmacistId}) verification code not set up for VisitId: {VisitId}.", pharmacistUserId, dispenseInput.VisitId);
                confirmation.OverallSuccess = false;
                confirmation.ErrorMessage = "Pharmacist verification setup is incomplete. Contact administrator.";
                confirmation.NextStepMessage = "Dispense cannot proceed due to account setup issue.";
                return confirmation;
            }
            bool isValidCode = _passwordHasherService.VerifyPassword(dispenseInput.PharmacistVerificationCode, verificationInfo.HashedVerificationCode, verificationInfo.VerificationCodeSalt);
            if (!isValidCode)
            {
                _logger.LogWarning("Service: Invalid pharmacist verification code for VisitId: {VisitId} by PharmacistId: {PharmacistId}.", dispenseInput.VisitId, pharmacistUserId);
                confirmation.OverallSuccess = false;
                confirmation.ErrorMessage = "Invalid pharmacist verification code.";
                confirmation.NextStepMessage = "Dispense aborted due to incorrect verification code.";
                return confirmation;
            }
            _logger.LogInformation("Service: Pharmacist verification code validated successfully for VisitId: {VisitId}.", dispenseInput.VisitId);

            // 2. Check if any items are selected for dispensing
            if (dispenseInput.ItemsToDispense == null || !dispenseInput.ItemsToDispense.Any(i => i.IsSelectedForDispense))
            {
                _logger.LogInformation("Service: No items selected for dispensing for VisitId: {VisitId}.", dispenseInput.VisitId);
                confirmation.NextStepMessage = "No items were selected for dispensing.";
                return confirmation; // OverallSuccess remains true as no actual processing failed
            }

            // 3. Process each selected item
            foreach (var itemToDispense in dispenseInput.ItemsToDispense.Where(i => i.IsSelectedForDispense))
            {
                var itemConfirmation = new DispensedItemConfirmationDto
                {
                    PrescriptionItemId = itemToDispense.PrescriptionItemId,
                    MedicationName = itemToDispense.MedicationName,
                    QuantityPrescribed = itemToDispense.QuantityPrescribed,
                    QuantityActuallyDispensedInTransaction = itemToDispense.QuantityToDispense // Default, might change
                };

                // 3a. Validate quantity to dispense string
                if (string.IsNullOrWhiteSpace(itemToDispense.QuantityToDispense))
                {
                    _logger.LogWarning("Service: QuantityToDispense is empty for PID: {PID}. Skipping item.", itemToDispense.PrescriptionItemId);
                    itemConfirmation.Notes = "Skipped: No dispense quantity entered.";
                    itemConfirmation.QuantityActuallyDispensedInTransaction = QuantityParser.FormatQuantity(0, "units");
                    confirmation.DispensedItems.Add(itemConfirmation);
                    confirmation.OverallSuccess = false; // An item that was selected could not be processed
                    continue;
                }

                // 3b. Get current prescription item status
                var currentDispenseInfo = await _prescriptionRepository.GetPrescriptionItemCurrentDispenseInfoAsync(itemToDispense.PrescriptionItemId);
                if (currentDispenseInfo == null)
                {
                    _logger.LogError("Service: Original prescription item not found for PID: {PID}.", itemToDispense.PrescriptionItemId);
                    itemConfirmation.Notes = "Error: Original prescription item not found.";
                    confirmation.DispensedItems.Add(itemConfirmation);
                    confirmation.OverallSuccess = false;
                    continue;
                }

                itemConfirmation.TotalQuantityDispensedSoFar = currentDispenseInfo.QuantityDispensedSoFar ?? QuantityParser.FormatQuantity(0, "units");
                itemConfirmation.IsFullyDispensedNow = currentDispenseInfo.IsAlreadyFullyDispensed;

                if (currentDispenseInfo.IsAlreadyFullyDispensed)
                {
                    _logger.LogInformation("Service: PID: {PID} is already fully dispensed. Skipping.", itemToDispense.PrescriptionItemId);
                    itemConfirmation.Notes = "Already fully dispensed.";
                    itemConfirmation.QuantityActuallyDispensedInTransaction = QuantityParser.FormatQuantity(0, "units") + " (Already Fulfilled)";
                    confirmation.DispensedItems.Add(itemConfirmation);
                    continue; // Not an error, just nothing to do for this item
                }

                // 3c. Parse quantities
                bool parsePrescribedOk = QuantityParser.TryParseQuantityAndUnit(itemToDispense.QuantityPrescribed, out int numPrescribed, out string prescribedUnit, _logger);
                bool parseDispensedThisTxOk = QuantityParser.TryParseQuantityAndUnit(itemToDispense.QuantityToDispense, out int numDispensedThisTransaction, out string dispensedThisTxUnit, _logger);
                bool parseDispensedSoFarOk = QuantityParser.TryParseQuantityAndUnit(currentDispenseInfo.QuantityDispensedSoFar, out int numDispensedSoFar, out string _, _logger, allowEmptyOrNullAsZero: true);

                if (!parsePrescribedOk || !parseDispensedThisTxOk || !parseDispensedSoFarOk)
                {
                    _logger.LogError("Service: Could not parse quantities for PID: {PID}. Prescribed: '{QP}', Dispensing: '{QD}', SoFar: '{QSF}'",
                        itemToDispense.PrescriptionItemId, itemToDispense.QuantityPrescribed, itemToDispense.QuantityToDispense, currentDispenseInfo.QuantityDispensedSoFar);
                    itemConfirmation.Notes = "Error: Invalid quantity format for processing.";
                    confirmation.DispensedItems.Add(itemConfirmation);
                    confirmation.OverallSuccess = false;
                    continue;
                }

                string unitForTransactionLogging = string.IsNullOrWhiteSpace(dispensedThisTxUnit) || dispensedThisTxUnit.Equals("units", StringComparison.OrdinalIgnoreCase) || dispensedThisTxUnit.Equals("unit", StringComparison.OrdinalIgnoreCase)
                                                   ? prescribedUnit : dispensedThisTxUnit;
                string unitForTotalStorage = prescribedUnit;

                if (numDispensedThisTransaction <= 0)
                {
                    _logger.LogInformation("Service: QuantityToDispense is zero for PID: {PID}. Skipping actual dispense.", itemToDispense.PrescriptionItemId);
                    itemConfirmation.Notes = "Skipped: Zero quantity entered for dispense.";
                    itemConfirmation.QuantityActuallyDispensedInTransaction = QuantityParser.FormatQuantity(0, unitForTransactionLogging);
                    confirmation.DispensedItems.Add(itemConfirmation);
                    continue;
                }

                if ((numDispensedSoFar + numDispensedThisTransaction) > numPrescribed)
                {
                    _logger.LogWarning("Service: Over-dispense attempt for PID: {PID}. Prescribed: {NumP}{UnitP}, SoFar: {NumSF}, ThisTx: {NumTx}{UnitTx}",
                        itemToDispense.PrescriptionItemId, numPrescribed, prescribedUnit, numDispensedSoFar, numDispensedThisTransaction, dispensedThisTxUnit);
                    itemConfirmation.Notes = "Error: Dispense quantity exceeds remaining prescribed amount.";
                    itemConfirmation.QuantityActuallyDispensedInTransaction = QuantityParser.FormatQuantity(numDispensedThisTransaction, dispensedThisTxUnit); // Show what was attempted
                    confirmation.DispensedItems.Add(itemConfirmation);
                    confirmation.OverallSuccess = false;
                    continue;
                }

                // --- Stock Decrement Attempt ---
                bool stockDecrementedSuccessfully = true;
                if (itemToDispense.MedicationId > 0 && numDispensedThisTransaction > 0)
                {
                    stockDecrementedSuccessfully = await _medicationRepository.DecrementStockAsync(
                        itemToDispense.MedicationId,
                        numDispensedThisTransaction
                    );

                    if (!stockDecrementedSuccessfully)
                    {
                        _logger.LogWarning("Service: Stock issue for MedicationId: {MedId} (PID: {PID}). Could not decrement by {Qty}. This might be due to insufficient stock or item not found in stock.",
                            itemToDispense.MedicationId, itemToDispense.PrescriptionItemId, numDispensedThisTransaction);
                        itemConfirmation.Notes = "Dispensed, but with stock level warning. Inventory not updated or insufficient.";
                        confirmation.OverallSuccess = false;
                    }
                    else
                    {
                        _logger.LogInformation("Service: Stock successfully decremented for MedicationId: {MedId} by {Qty}.", itemToDispense.MedicationId, numDispensedThisTransaction);
                    }
                }
                // --- End Stock Decrement ---

                // 3d. Log the dispense action
                var logEntry = new DispenseLogEntryInputDto
                {
                    PrescriptionItemId = itemToDispense.PrescriptionItemId,
                    VisitId = dispenseInput.VisitId,
                    MedicationId = itemToDispense.MedicationId,
                    QuantityDispensedInTransaction = QuantityParser.FormatQuantity(numDispensedThisTransaction, unitForTransactionLogging),
                    DispensedByUserId = pharmacistUserId,
                    PharmacistNotes = dispenseInput.DispensingNotes,
                };
                int logId = await _dispensationRepository.LogDispenseActionAsync(logEntry);

                if (logId <= 0)
                {
                    _logger.LogError("Service: Failed to log dispense action for PID: {PID}", itemToDispense.PrescriptionItemId);
                    itemConfirmation.Notes = (itemConfirmation.Notes ?? "") + " Error: Failed to log this dispense action.";
                    confirmation.DispensedItems.Add(itemConfirmation);
                    confirmation.OverallSuccess = false;
                    continue;
                }

                // 3e. Update prescription item status
                int newTotalNumDispensed = numDispensedSoFar + numDispensedThisTransaction;
                bool isNowFullyDispensed = newTotalNumDispensed >= numPrescribed;
                string newTotalQuantityDispensedString = QuantityParser.FormatQuantity(newTotalNumDispensed, unitForTotalStorage);

                bool updateSuccess = await _prescriptionRepository.UpdatePrescriptionItemDispenseStatusAsync(
                    itemToDispense.PrescriptionItemId, newTotalQuantityDispensedString, isNowFullyDispensed, pharmacistUserId);

                if (!updateSuccess)
                {
                    _logger.LogError("Service: Failed to update prescription item status for PID: {PID}", itemToDispense.PrescriptionItemId);
                    itemConfirmation.Notes = (itemConfirmation.Notes ?? "") + " Error: Failed to update prescription item status after logging dispense.";
                    confirmation.OverallSuccess = false;
                }
                else
                {
                    if (string.IsNullOrEmpty(itemConfirmation.Notes) || (!itemConfirmation.Notes.StartsWith("Error:") && !itemConfirmation.Notes.StartsWith("Warning:")))
                    {
                        itemConfirmation.Notes = isNowFullyDispensed ? "Fully dispensed." : "Partially dispensed.";
                    }
                }
                itemConfirmation.TotalQuantityDispensedSoFar = newTotalQuantityDispensedString;
                itemConfirmation.IsFullyDispensedNow = isNowFullyDispensed;
                confirmation.DispensedItems.Add(itemConfirmation);
            }

            bool anyHardErrorsForItemProcessing = confirmation.DispensedItems.Any(di => di.Notes != null && di.Notes.StartsWith("Error:"));
            if (anyHardErrorsForItemProcessing)
            {
                confirmation.OverallSuccess = false;
            }

            if (confirmation.OverallSuccess)
            {
                bool anyActuallyDispensed = confirmation.DispensedItems.Any(di => di.Notes != null && (di.Notes.Contains("Fully dispensed") || di.Notes.Contains("Partially dispensed")));
                confirmation.NextStepMessage = anyActuallyDispensed ? "Selected medications processed." : "No new items required dispensing in this transaction.";
                confirmation.ErrorMessage = null;
            }
            else
            {
                if (string.IsNullOrEmpty(confirmation.ErrorMessage))
                {
                    confirmation.ErrorMessage = "One or more items encountered an error or were skipped. Please review.";
                }
                confirmation.NextStepMessage = "Review dispense summary for details.";
            }

            return confirmation;
        }

        /// <summary>
        /// Gets the view model for the dispensed history page, including items and pagination info.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>The view model for the dispensed history page.</returns>
        public async Task<DispensedHistoryViewModel> GetDispensedHistoryViewModelAsync(FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Service: Getting Dispensed History ViewModel with options: {@Options}", options);

            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25;

            var (items, totalCount) = await _dispensationRepository.GetDispensedHistoryAsync(options);

            var viewModel = new DispensedHistoryViewModel
            {
                DispensedItems = items?.ToList() ?? new List<DispensedHistoryItemDto>(),
                PaginationInfo = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)options.PageSize)
                },
                FilterStartDate = options.StartDate,
                FilterEndDate = options.EndDate,
                FilterPatientSearch = options.SearchTerm1,
                FilterMedicationSearch = options.SearchTerm2
            };

            return viewModel;
        }

        /// <summary>
        /// Retrieves a view model containing a paginated list of medication stock details for the inventory UI.
        /// Includes overall stock status and low stock items count.
        /// </summary>
        /// <param name="options">Filtering and pagination options for the inventory list.</param>
        /// <returns>A <see cref="MedicationInventoryViewModel"/> containing the paginated inventory list and associated data.</returns>
        public async Task<MedicationInventoryViewModel> GetMedicationInventoryViewModelAsync(FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Service: Getting Medication Inventory ViewModel with options: {@Options}", options);

            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25;

            var (items, totalCount) = await _medicationRepository.GetAllMedicationStockAsync(options);

            // Calculate low stock items count
            int lowStockCount = items.Count(item => item.QuantityOnHand <= item.MinimumStockLevel);

            var viewModel = new MedicationInventoryViewModel
            {
                InventoryItems = items?.ToList() ?? new List<MedicationStockDetailDto>(),
                Pagination = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)options.PageSize)
                },
                Filters = options,
                LowStockItemsCount = lowStockCount
            };

            return viewModel;
        }

        /// <summary>
        /// Adjusts the stock level for a specific medication by incrementing or decrementing its quantity.
        /// </summary>
        /// <param name="medicationId">The ID of the medication whose stock is to be adjusted.</param>
        /// <param name="quantity">The amount by which to adjust the stock.</param>
        /// <param name="isIncrement">If true, stock is incremented; if false, it is decremented.</param>
        /// <param name="performingUserId">The ID of the user performing the stock adjustment.</param>
        /// <returns>True if the stock was successfully adjusted, false otherwise.</returns>
        public async Task<bool> AdjustMedicationStockAsync(int medicationId, int quantity, bool isIncrement, int performingUserId)
        {
            _logger.LogInformation("Service: Adjusting stock for MedicationId: {MedicationId}. Quantity: {Quantity}, IsIncrement: {IsIncrement}, By User: {PerformingUserId}",
                medicationId, quantity, isIncrement, performingUserId);

            if (medicationId <= 0 || quantity <= 0 || performingUserId <= 0)
            {
                _logger.LogWarning("Service: AdjustMedicationStockAsync called with invalid input. MedicationId: {MedicationId}, Quantity: {Quantity}, PerformingUserId: {PerformingUserId}",
                    medicationId, quantity, performingUserId);
                return false;
            }

            bool success;
            if (isIncrement)
            {
                success = await _medicationRepository.IncrementStockAsync(medicationId, quantity, performingUserId);
            }
            else
            {
                // For decrement, check if sufficient stock exists first.
                // Assuming DecrementStockAsync handles insufficient stock returning false.
                success = await _medicationRepository.DecrementStockAsync(medicationId, quantity);
            }

            if (success)
            {
                _logger.LogInformation("Service: Successfully adjusted stock for MedicationId: {MedicationId}. New operation: {Operation}", medicationId, isIncrement ? "Increment" : "Decrement");
            }
            else
            {
                _logger.LogError("Service: Failed to adjust stock for MedicationId: {MedicationId}. Operation: {Operation}", medicationId, isIncrement ? "Increment" : "Decrement");
            }
            return success;
        }

        public Task<MedicationStockDetailDto?> GetMedicationStockDetailAsync(int medicationId)
        {
            _logger.LogInformation("Service: Getting medication stock detail for MedicationId: {MedicationId}", medicationId);
            if (medicationId <= 0)
            {
                _logger.LogWarning("Service: GetMedicationStockDetailAsync called with invalid MedicationId: {MedicationId}", medicationId);
                return Task.FromResult<MedicationStockDetailDto?>(null);
            }
            return _medicationRepository.GetMedicationStockDetailAsync(medicationId);
        }
    }
}