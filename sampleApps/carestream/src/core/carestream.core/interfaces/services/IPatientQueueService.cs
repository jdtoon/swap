using System.Collections.Generic;
using System.Threading.Tasks;
using carestream.core.dtos.patientadmin; // For PatientQueueListViewModel, PatientQueueBoardViewModel, PatientQueueItemDto
using carestream.core.dtos.shared;      // For FilterAndPaginationOptions

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for managing the patient queue,
    /// providing data for both list and board (Kanban) views.
    /// </summary>
    public interface IPatientQueueService
    {
        /// <summary>
        /// Retrieves the view model for the patient queue in a list format, supporting filtering and pagination.
        /// </summary>
        /// <param name="options">Options for filtering and pagination.</param>
        /// <returns>A <see cref="PatientQueueListViewModel"/>.</returns>
        Task<PatientQueueListViewModel> GetPatientQueueListViewModelAsync(FilterAndPaginationOptions options);

        /// <summary>
        /// Retrieves the view model for the patient queue in a board (Kanban) format, grouped by status.
        /// </summary>
        /// <returns>A <see cref="PatientQueueBoardViewModel"/>.</returns>
        Task<PatientQueueBoardViewModel> GetPatientQueueBoardViewModelAsync();

        /// <summary>
        /// Updates the status of a patient's visit to 'Called' (or similar) and logs the action.
        /// This action is typically performed by Patient Admin staff from the queue views.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <param name="actionedByUserId">The ID of the user performing the 'call' action.</param>
        /// <returns>True if the status was successfully updated, false otherwise.</returns>
        Task<bool> CallPatientAsync(int visitId, int actionedByUserId);
    }
}