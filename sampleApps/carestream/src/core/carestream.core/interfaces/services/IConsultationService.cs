using carestream.core.dtos.consultation;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for the Doctor/Nurse Consultation module.
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// Initiates or resumes a consultation session for a given visit, updating its status
        /// to 'ConsultationInProgress' and assigning the performing user as the assigned officer.
        /// </summary>
        /// <param name="visitId">The ID of the visit to start/resume.</param>
        /// <param name="patientId">The ID of the patient associated with the visit (for context/validation).</param>
        /// <param name="performingUserId">The ID of the user (doctor/nurse) who is starting the consultation.</param>
        /// <returns>True if the consultation session was successfully initiated/resumed, false otherwise.</returns>
        Task<bool> StartConsultationSessionAsync(int visitId, int patientId, int performingUserId);

        /// <summary>
        /// Prepares and retrieves the complete view model for the main consultation screen.
        /// This method aggregates data from various repositories.
        /// </summary>
        /// <param name="visitId">The unique ID of the patient's visit.</param>
        /// <param name="patientId">The unique ID of the patient.</param>
        /// <returns>A <see cref="ConsultationViewModel"/> containing all necessary data for the consultation UI.</returns>
        Task<ConsultationViewModel> GetConsultationViewModelAsync(int visitId, int patientId);

        /// <summary>
        /// Updates the doctor's consultation notes for a specific visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit for which to update notes.</param>
        /// <param name="notes">The new content of the doctor's notes.</param>
        /// <returns>True if the notes were successfully updated, false otherwise.</returns>
        Task<bool> UpdateDoctorNotesAsync(int visitId, string? notes);

        /// <summary>
        /// Searches for ICD-10 diagnosis codes based on a search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for (code or description).</param>
        /// <param name="limit">The maximum number of results to return (default is 10).</param>
        /// <returns>An enumerable of <see cref="Icd10CodeDto"/> matching the search criteria.</returns>
        Task<IEnumerable<Icd10CodeDto>> SearchIcd10CodesAsync(string searchTerm, int limit = 10);

        /// <summary>
        /// Searches for medical procedures based on a search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for (code or name).</param>
        /// <param name="limit">The maximum number of results to return (default is 10).</param>
        /// <returns>An enumerable of <see cref="ProcedureDto"/> matching the search criteria.</returns>
        Task<IEnumerable<ProcedureDto>> SearchProceduresAsync(string searchTerm, int limit = 10);

        /// <summary>
        /// Saves or links one or more ICD-10 diagnosis codes to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to link diagnoses to.</param>
        /// <param name="patientId">The ID of the patient associated with the visit.</param>
        /// <param name="icd10CodeIds">A collection of unique identifiers for the ICD-10 codes.</param>
        /// <param name="recordedByUserId">The ID of the user who recorded the diagnoses.</param>
        /// <returns>True if the diagnoses were successfully linked, false otherwise.</returns>
        Task<bool> SaveVisitDiagnosisAsync(int visitId, int patientId, IEnumerable<int> icd10CodeIds, int recordedByUserId);

        /// <summary>
        /// Saves or links one or more medical procedures to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to link procedures to.</param>
        /// <param name="patientId">The ID of the patient associated with the visit.</param>
        /// <param name="procedureIds">A collection of unique identifiers for the procedures.</param>
        /// <param name="performedByUserId">The ID of the user who performed/recorded the procedures.</param>
        /// <returns>True if the procedures were successfully linked, false otherwise.</returns>
        Task<bool> SaveVisitProceduresAsync(int visitId, int patientId, IEnumerable<int> procedureIds, int performedByUserId);

        /// <summary>
        /// Finalizes a patient consultation, typically by updating the visit status to 'Discharged'.
        /// </summary>
        /// <param name="visitId">The ID of the visit to finalize.</param>
        /// <param name="performingUserId">The ID of the user performing the finalization.</param>
        /// <returns>True if the consultation was successfully finalized, false otherwise.</returns>
        Task<bool> FinalizeConsultationAsync(int visitId, int performingUserId);
    }
}