using carestream.core.dtos.patient;
using carestream.core.dtos.checkin;
using carestream.core.dtos.visit;
using System.Threading.Tasks;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations related to patient management, lookup, and check-in processes.
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// Retrieves detailed information for a patient based on their Force Number.
        /// </summary>
        /// <param name="forceNumber">The Force Number of the patient to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the <see cref="PatientDetailDto"/> if found; otherwise, null.
        /// </returns>
        Task<PatientDetailDto?> GetPatientByForceNumberAsync(string forceNumber);

        /// <summary>
        /// Retrieves detailed patient information by their internal Patient ID.
        /// </summary>
        /// <param name="patientId">The unique identifier for the patient.</param>
        /// <returns>A <see cref="PatientDetailDto"/> if found; otherwise, null.</returns>
        Task<PatientDetailDto?> GetPatientDetailByIdAsync(int patientId);

        /// <summary>
        /// Retrieves information about the most recent active (non-terminal status) visit for a given patient.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains an <see cref="ActiveVisitDto"/> if an active visit is found; otherwise, null.
        /// </returns>
        Task<ActiveVisitDto?> GetActiveVisitForPatientAsync(int patientId);

        /// <summary>
        /// Creates a new visit for the specified patient and sets its status to 'Waiting for Vitals'.
        /// This method should typically be called when no other active visit exists for the patient.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient for whom the visit is being created.</param>
        /// <param name="performingUserId">The unique identifier of the user performing the check-in.</param>
        /// <param name="briefReason">A brief reason for the visit.</param>
        /// <param name="additionalNotes">Optional additional notes for the visit.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="CheckinConfirmationDto"/> indicating the outcome of the operation.
        /// </returns>
        Task<CheckinConfirmationDto> CreateNewVisitAndCheckinAsync(int patientId, int performingUserId, string briefReason, string? additionalNotes); // MODIFIED: briefReason is NOT NULLABLE here

        /// <summary>
        /// Resumes an existing active visit by updating its status to 'Waiting for Vitals'.
        /// </summary>
        /// <param name="visitId">The unique identifier of the visit to resume.</param>
        /// <param name="patientId">The unique identifier of the patient associated with the visit (for verification).</param>
        /// <param name="performingUserId">The unique identifier of the user performing the action.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="CheckinConfirmationDto"/> indicating the outcome of the operation.
        /// </returns>
        Task<CheckinConfirmationDto> ResumeActiveVisitAsync(int visitId, int patientId, int performingUserId);

        /// <summary>
        /// Administratively closes an existing (typically active) visit and then creates a new visit
        /// for the specified patient, setting the new visit's status to 'Waiting for Vitals'.
        /// </summary>
        /// <param name="oldVisitId">The unique identifier of the visit to be closed.</param>
        /// <param name="patientId">The unique identifier of the patient for whom the new visit is being created.</param>
        /// <param name="performingUserId">The unique identifier of the user performing the action.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="CheckinConfirmationDto"/> indicating the outcome of the operation.
        /// </returns>
        Task<CheckinConfirmationDto> CloseAndStartNewVisitAsync(int oldVisitId, int patientId, int performingUserId);

        /// <summary>
        /// Updates the personal information of a patient.
        /// </summary>
        /// <param name="patientInfo">The DTO containing the updated personal information.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdatePatientPersonalInfoAsync(EditPatientPersonalInfoDto patientInfo);

        /// <summary>
        /// Retrieves the personal information of a patient for editing purposes.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <returns>An <see cref="EditPatientPersonalInfoDto"/> if found; otherwise, null.</returns>
        Task<EditPatientPersonalInfoDto?> GetPatientPersonalInfoForEditAsync(int patientId);

        /// <summary>
        /// Retrieves the contact information of a patient for editing.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <returns>An <see cref="EditPatientContactInfoDto"/> if found; otherwise, null.</returns>
        Task<EditPatientContactInfoDto?> GetPatientContactInfoForEditAsync(int patientId);

        /// <summary>
        /// Updates the contact information of a patient.
        /// </summary>
        /// <param name="contactInfo">The DTO containing the updated contact information.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdatePatientContactInfoAsync(EditPatientContactInfoDto contactInfo);

        /// <summary>
        /// Retrieves the emergency contact information of a patient for editing.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <returns>An <see cref="EditPatientEmergencyContactInfoDto"/> if found; otherwise, null.</returns>
        Task<EditPatientEmergencyContactInfoDto?> GetPatientEmergencyContactInfoForEditAsync(int patientId);

        /// <summary>
        /// Updates the emergency contact information of a patient.
        /// </summary>
        /// <param name="emergencyContactInfo">The DTO containing the updated emergency contact information.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdatePatientEmergencyContactInfoAsync(EditPatientEmergencyContactInfoDto emergencyContactInfo);
    }
}