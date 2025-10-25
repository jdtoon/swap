using System.Threading.Tasks;
using carestream.core.dtos.patient; // Added for new DTOs
using carestream.core.dtos.patientadmin;
using carestream.core.dtos.shared;

namespace carestream.core.interfaces.services
{
    public interface IPatientAdminService
    {
        /// <summary>
        /// Retrieves a view model containing patient queue items for the Patient Admin dashboard.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="PatientQueueViewModel"/> containing the paginated queue and filter information.</returns>
        Task<PatientQueueViewModel> GetPatientQueueViewModelAsync(FilterAndPaginationOptions options);

        /// <summary>
        /// Processes the "Call Patient" action from the Patient Admin queue.
        /// Updates the visit status to an appropriate "called" or "in-progress" state.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <param name="performingUserId">The ID of the Patient Admin performing the action.</param>
        /// <returns>True if the status was successfully updated, false otherwise.</returns>
        Task<bool> CallPatientAsync(int visitId, int performingUserId);

        /// <summary>
        /// Retrieves a view model containing a paginated list of all patients for administrative purposes.
        /// Supports filtering and sorting.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="PatientListViewModel"/> containing the paginated patient list and filter information.</returns>
        Task<PatientListViewModel> GetAllPatientsForAdminAsync(FilterAndPaginationOptions options);

        /// <summary>
        /// Registers a new patient in the system.
        /// Includes validation to ensure the force number is unique.
        /// </summary>
        /// <param name="patientData">The DTO containing the new patient's details.</param>
        /// <param name="createdByUserId">The ID of the user creating the patient record.</param>
        /// <returns>A <see cref="PatientRegistrationResultDto"/> indicating success or failure and any relevant messages.</returns>
        Task<PatientRegistrationResultDto> RegisterNewPatientAsync(CreatePatientInputDto patientData, int createdByUserId);
    }
}