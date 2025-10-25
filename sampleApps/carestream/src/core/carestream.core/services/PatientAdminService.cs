using Microsoft.Extensions.Logging;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.patient; // Added for new DTOs
using carestream.core.dtos.patientadmin;
using carestream.core.dtos.shared;
using carestream.core.dtos.visit;
using carestream.core.enums;
using System.Collections.Generic;

namespace carestream.core.services
{
    public class PatientAdminService : IPatientAdminService
    {
        private readonly IVisitRepository _visitRepository;
        private readonly IPatientRepository _patientRepository; // Added new repository
        private readonly ILogger<PatientAdminService> _logger;

        public PatientAdminService(IVisitRepository visitRepository, IPatientRepository patientRepository, ILogger<PatientAdminService> logger)
        {
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository)); // Initialize new repository
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a view model containing patient queue items for the Patient Admin dashboard.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="PatientQueueViewModel"/> containing the paginated queue and filter information.</returns>
        public async Task<PatientQueueViewModel> GetPatientQueueViewModelAsync(FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Service: Getting Patient Admin Queue ViewModel with options: {@Options}", options);

            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 15; // Default page size for this queue

            var (items, totalCount) = await _visitRepository.GetPatientAdminQueueAsync(options);

            var viewModel = new PatientQueueViewModel
            {
                QueueItems = items?.ToList() ?? new List<PatientQueueItemDto>(),
                PaginationInfo = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / options.PageSize) // Calculate total pages
                },
                CurrentFilters = options // Pass back current filters for UI binding
            };
            return viewModel;
        }

        /// <summary>
        /// Processes the "Call Patient" action from the Patient Admin queue.
        /// Updates the visit status to an appropriate "called" or "in-progress" state.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <param name="performingUserId">The ID of the Patient Admin performing the action.</param>
        /// <returns>True if the status was successfully updated, false otherwise.</returns>
        public async Task<bool> CallPatientAsync(int visitId, int performingUserId)
        {
            _logger.LogInformation("Service: Processing 'Call Patient' for VisitId: {VisitId} by User: {PerformingUserId}", visitId, performingUserId);

            if (visitId <= 0)
            {
                _logger.LogWarning("Service: CallPatientAsync called with invalid VisitId: {VisitId}", visitId);
                return false;
            }

            // Fetch the current visit to determine its current status
            BasicVisitInfoDto? currentVisit = await _visitRepository.GetBasicVisitInfoByIdAsync(visitId);
            if (currentVisit == null)
            {
                _logger.LogWarning("Service: VisitId {VisitId} not found for CallPatientAsync.", visitId);
                return false;
            }

            VisitStatus newStatusEnum;
            // Parse the status string from DB to enum for comparison
            if (!Enum.TryParse(currentVisit.Status, out VisitStatus currentStatusEnum))
            {
                _logger.LogWarning("Service: CallPatientAsync: Unrecognized status '{Status}' for VisitId {VisitId}. Cannot proceed.", currentVisit.Status, visitId);
                return false;
            }

            // Determine the next status based on the current status
            switch (currentStatusEnum)
            {
                case VisitStatus.WaitingForVitals:
                    newStatusEnum = VisitStatus.VitalsInProgress;
                    break;
                case VisitStatus.ReadyForDoctor:
                    newStatusEnum = VisitStatus.ConsultationInProgress;
                    break;
                // Add other cases if a Patient Admin can "call" from other statuses
                default:
                    _logger.LogInformation("Service: Patient for VisitId {VisitId} is in status '{CurrentStatus}', no 'Call Patient' action defined for this status by PA.", visitId, currentStatusEnum.ToString());
                    return false; // Or true if no action is considered success for these states
            }

            // Update the visit status and assign the performing user (e.g., as the one who actioned it)
            bool success = await _visitRepository.UpdateVisitStatusAsync(visitId, newStatusEnum, performingUserId);

            if (success)
            {
                _logger.LogInformation("Service: Successfully updated VisitId {VisitId} to status '{NewStatus}' by User {PerformingUserId}.", visitId, newStatusEnum.ToString(), performingUserId);
                // TODO: Future - trigger real-time notification (e.g., SignalR to Vitals/Doctor display board)
            }
            else
            {
                _logger.LogError("Service: Failed to update status for VisitId {VisitId} during CallPatientAsync.", visitId);
            }
            return success;
        }

        /// <summary>
        /// Retrieves a view model containing a paginated list of all patients for administrative purposes.
        /// Supports filtering and sorting.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="PatientListViewModel"/> containing the paginated patient list and filter information.</returns>
        public async Task<PatientListViewModel> GetAllPatientsForAdminAsync(FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Service: Getting all patients for admin with options: {@Options}", options);

            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25; // Default page size for all patients

            var (patients, totalCount) = await _patientRepository.GetAllPatientsAsync(options);

            var viewModel = new PatientListViewModel
            {
                Patients = patients?.ToList() ?? new List<PatientBasicInfoDto>(),
                Pagination = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / options.PageSize)
                },
                Filters = options
            };

            return viewModel;
        }

        /// <summary>
        /// Registers a new patient in the system.
        /// Includes validation to ensure the force number is unique.
        /// </summary>
        /// <param name="patientData">The DTO containing the new patient's details.</param>
        /// <param name="createdByUserId">The ID of the user creating the patient record.</param>
        /// <returns>A <see cref="PatientRegistrationResultDto"/> indicating success or failure and any relevant messages.</returns>
        public async Task<PatientRegistrationResultDto> RegisterNewPatientAsync(CreatePatientInputDto patientData, int createdByUserId)
        {
            _logger.LogInformation("Service: Registering new patient with Force Number: {ForceNumber} by User: {CreatedByUserId}", patientData.ForceNumber, createdByUserId);

            // Validate input data (basic validation using data annotations is done by MVC, but additional business logic validation here)
            if (string.IsNullOrWhiteSpace(patientData.ForceNumber) ||
                string.IsNullOrWhiteSpace(patientData.FirstName) ||
                string.IsNullOrWhiteSpace(patientData.LastName))
            {
                _logger.LogWarning("Service: Patient registration failed due to missing required fields.");
                return new PatientRegistrationResultDto { IsSuccess = false, Message = "Force Number, First Name, and Last Name are required." };
            }

            // Check for unique force number
            var existingPatient = await _patientRepository.FindByForceNumberAsync(patientData.ForceNumber);
            if (existingPatient != null)
            {
                _logger.LogWarning("Service: Patient registration failed: Duplicate Force Number '{ForceNumber}'.", patientData.ForceNumber);
                return new PatientRegistrationResultDto { IsSuccess = false, Message = $"Patient with Force Number '{patientData.ForceNumber}' already exists.", IsDuplicateForceNumber = true };
            }

            try
            {
                int newPatientId = await _patientRepository.CreatePatientAsync(patientData, createdByUserId);

                if (newPatientId > 0)
                {
                    _logger.LogInformation("Service: Successfully registered new patient with ID: {PatientId} and Force Number: {ForceNumber}.", newPatientId, patientData.ForceNumber);
                    return new PatientRegistrationResultDto { IsSuccess = true, PatientId = newPatientId, Message = "Patient registered successfully." };
                }
                else
                {
                    _logger.LogError("Service: Patient registration failed, repository returned no ID.");
                    return new PatientRegistrationResultDto { IsSuccess = false, Message = "Failed to register patient. Please try again." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred during patient registration for Force Number {ForceNumber}.", patientData.ForceNumber);
                return new PatientRegistrationResultDto { IsSuccess = false, Message = $"An unexpected error occurred: {ex.Message}" };
            }
        }
    }
}