using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.patientadmin;
using carestream.core.dtos.shared;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using carestream.core.dtos.visit; // For BasicVisitInfoDto if needed
using carestream.core.enums; // Added for VisitStatus enum

namespace carestream.core.services
{
    /// <summary>
    /// Provides service logic for managing the patient queue.
    /// </summary>
    public class PatientQueueService : IPatientQueueService
    {
        private readonly IVisitRepository _visitRepository;
        private readonly ILogger<PatientQueueService> _logger;

        public PatientQueueService(
            IVisitRepository visitRepository,
            ILogger<PatientQueueService> logger)
        {
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<PatientQueueListViewModel> GetPatientQueueListViewModelAsync(FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Service: Getting Patient Queue List ViewModel with options: {@Options}", options);

            // Define statuses relevant for Patient Admin Queue using enum string representations
            var queueStatuses = new List<string> {
                VisitStatus.WaitingForVitals.ToString(),
                VisitStatus.VitalsInProgress.ToString(),
                VisitStatus.ReadyForDoctor.ToString(),
                VisitStatus.ConsultationInProgress.ToString()
                // Removed "Pending Checkin" as it's not a valid VisitStatus enum value
            };

            var (allQueueItems, totalCount) = await _visitRepository.GetPatientAdminQueueAsync(options);

            var items = allQueueItems.ToList();
            await ApplyWaitTimeFormatting(items); // Apply formatting to each item

            return new PatientQueueListViewModel
            {
                QueueItems = items,
                PaginationInfo = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)options.PageSize)
                },
                CurrentFilters = options,
                CurrentViewType = "list"
            };
        }

        /// <inheritdoc/>
        public async Task<PatientQueueBoardViewModel> GetPatientQueueBoardViewModelAsync()
        {
            _logger.LogInformation("Service: Getting Patient Queue Board ViewModel.");

            // Define statuses relevant for Patient Admin Queue for the board view using enum string representations
            var queueStatuses = new List<string>
            {
                VisitStatus.WaitingForVitals.ToString(),
                VisitStatus.VitalsInProgress.ToString(),
                VisitStatus.ReadyForDoctor.ToString(),
                VisitStatus.ConsultationInProgress.ToString(),
                VisitStatus.PendingPrescription.ToString(), // "Waiting for Pharmacy" maps to this
                VisitStatus.InTreatment.ToString()
            };

            // Fetch all relevant queue items without pagination for the board
            var (allQueueItems, _) = await _visitRepository.GetPatientAdminQueueAsync(new FilterAndPaginationOptions { PageSize = int.MaxValue, PageNumber = 1 }); // Fetch all

            var items = allQueueItems.ToList();
            await ApplyWaitTimeFormatting(items); // Apply formatting to each item

            var patientsByStatus = new Dictionary<string, List<PatientQueueItemDto>>();

            // Populate the dictionary with empty lists for all expected columns first to ensure they always appear in order
            var columnOrder = new List<string>
            {
                VisitStatus.WaitingForVitals.ToString(),
                VisitStatus.VitalsInProgress.ToString(),
                VisitStatus.ReadyForDoctor.ToString(),
                VisitStatus.ConsultationInProgress.ToString(),
                VisitStatus.PendingPrescription.ToString(), // "Waiting for Pharmacy" maps to this
                VisitStatus.InTreatment.ToString()
            };

            foreach (var status in columnOrder)
            {
                patientsByStatus[status] = new List<PatientQueueItemDto>();
            }

            // Then, populate with actual data, ensuring patients go into the correct predefined columns
            foreach (var item in items)
            {
                // Ensure item.Status (string from DB) can be mapped to an expected column.
                // Using TryGetValue to avoid adding "Other" column unless truly unexpected status.
                if (patientsByStatus.TryGetValue(item.Status, out var patientList))
                {
                    patientList.Add(item);
                }
                else
                {
                    _logger.LogWarning("Service: PatientQueueBoardViewModel encountered unmapped status '{Status}' for VisitId {VisitId}. Item skipped for board view.", item.Status, item.VisitId);
                    // Decide if you want to skip or add to an "Other" column. For a clean board, skipping unexpected can be better.
                    // If adding to "Other", ensure "Other" is in columnOrder or handled as special case.
                    // For now, I'm just logging and not adding to "Other" unless you explicitly define it.
                }
            }

            // Calculate overall average wait time (conceptual, could be more complex or moved to repo)
            TimeSpan totalWaitDuration = TimeSpan.Zero; // Use TimeSpan.Empty for initialization
            if (items.Any()) // Only calculate if there are items to avoid division by zero
            {
                foreach (var item in items)
                {
                    totalWaitDuration += (DateTime.UtcNow - item.RelevantTimestamp);
                }
            }


            string overallAverageWaitTime = "N/A";
            if (items.Any())
            {
                double avgSeconds = totalWaitDuration.TotalSeconds / items.Count;
                if (avgSeconds < 60) overallAverageWaitTime = $"{(int)avgSeconds} sec";
                else if (avgSeconds < 3600) overallAverageWaitTime = $"{(int)(avgSeconds / 60)} min";
                else overallAverageWaitTime = $"{(int)(avgSeconds / 3600)} hr {(int)((avgSeconds % 3600) / 60)} min";
            }

            return new PatientQueueBoardViewModel
            {
                PatientsByStatus = patientsByStatus,
                TotalPatientsInQueue = items.Count,
                OverallAverageWaitTime = overallAverageWaitTime,
                CurrentViewType = "board",
                StatusColumnOrder = columnOrder
            };
        }

        /// <inheritdoc/>
        public async Task<bool> CallPatientAsync(int visitId, int actionedByUserId)
        {
            _logger.LogInformation("Service: Calling patient for VisitId: {VisitId} by User: {ActionedByUserId}", visitId, actionedByUserId);

            var visit = await _visitRepository.GetBasicVisitInfoByIdAsync(visitId);
            if (visit == null)
            {
                _logger.LogWarning("Service: CallPatientAsync: Visit {VisitId} not found.", visitId);
                return false;
            }

            // Parse the status string from DB to enum for comparison
            if (!Enum.TryParse(visit.Status, out VisitStatus currentStatusEnum))
            {
                _logger.LogWarning("Service: CallPatientAsync: Unrecognized status '{Status}' for VisitId {VisitId}. Cannot proceed.", visit.Status, visitId);
                return false;
            }

            // Simple logic: if waiting for Vitals or Doctor, mark as 'In Treatment' (or a new 'Called' status)
            if (currentStatusEnum == VisitStatus.WaitingForVitals || currentStatusEnum == VisitStatus.ReadyForDoctor)
            {
                return await _visitRepository.UpdateVisitStatusAsync(visitId, VisitStatus.InTreatment, actionedByUserId);
            }
            else
            {
                _logger.LogInformation("Service: CallPatientAsync: Visit {VisitId} already in status '{Status}', no change needed for 'Call Patient'.", visitId, visit.Status);
                return true; // Consider it successful as no action needed
            }
        }

        /// <summary>
        /// Helper method to calculate wait time display and color class for queue items.
        /// </summary>
        /// <param name="items">List of queue items to process.</param>
        private Task ApplyWaitTimeFormatting(List<PatientQueueItemDto> items)
        {
            foreach (var item in items)
            {
                var waitDuration = DateTime.UtcNow - item.RelevantTimestamp;
                string timeString;
                string colorClass = "text-base-content"; // Default

                if (waitDuration.TotalMinutes < 1)
                {
                    timeString = "< 1 min";
                }
                else if (waitDuration.TotalHours >= 1)
                {
                    timeString = $"{(int)waitDuration.TotalHours}h {(int)waitDuration.Minutes}m";
                }
                else
                {
                    timeString = $"{(int)waitDuration.TotalMinutes} min";
                }

                // Apply color coding based on status and wait duration
                // Parse the status string from DTO to enum for robust comparison
                if (Enum.TryParse(item.Status, out VisitStatus itemStatusEnum))
                {
                    if (itemStatusEnum == VisitStatus.WaitingForVitals || itemStatusEnum == VisitStatus.ReadyForDoctor || itemStatusEnum == VisitStatus.PendingPrescription)
                    {
                        if (waitDuration.TotalMinutes >= 20) colorClass = "text-error";
                        else if (waitDuration.TotalMinutes >= 10) colorClass = "text-warning";
                        else colorClass = "text-success";
                    }
                }
                // Priorities can also influence color, if not already handled by badge class
                if (item.Priority == "Urgent" || item.Priority == "High") // Assuming Priority is a string for now
                {
                    colorClass = "text-error font-bold"; // Override if urgent/high priority
                }

                item.WaitTimeDisplay = timeString;
                item.WaitTimeColorClass = colorClass;
            }
            return Task.CompletedTask;
        }
    }
}