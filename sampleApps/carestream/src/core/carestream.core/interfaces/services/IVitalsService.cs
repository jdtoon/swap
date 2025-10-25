using carestream.core.dtos.vitals; // For VitalsCaptureInputDto
using System.Threading.Tasks;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations related to capturing and managing patient vital signs.
    /// </summary>
    public interface IVitalsService
    {
        /// <summary>
        /// Prepares the Vitals Capture DTO with necessary context (e.g., patient name, rank)
        /// for displaying the vitals capture form.
        /// It might also load previously recorded vitals for this visit if re-editing is allowed.
        /// </summary>
        /// <param name="visitId">The ID of the visit for which vitals are being captured.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="VitalsCaptureInputDto"/> pre-populated
        /// with patient context and any existing vitals for the visit, or null if the visit/patient is not found.
        /// </returns>
        Task<VitalsCaptureInputDto?> GetVitalsCaptureModelAsync(int visitId, int patientId);

        /// <summary>
        /// Saves the captured vital signs for a specific visit.
        /// This will also update the visit status (e.g., to 'ReadyForDoctor').
        /// </summary>
        /// <param name="inputDto">The DTO containing the captured vital signs.</param>
        /// <param name="performingUserId">The ID of the nurse recording the vitals.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result indicates whether the save operation was successful.
        /// (Consider returning a more detailed result object later if needed, e.g., with confirmation ID or error messages).
        /// </returns>
        Task<bool> SaveVitalsAsync(VitalsCaptureInputDto inputDto, int performingUserId);
    }
}