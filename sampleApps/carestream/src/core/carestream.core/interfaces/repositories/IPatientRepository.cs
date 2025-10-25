using carestream.core.dtos.patient;
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Data;
using System.Threading.Tasks;

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations related to patients.
    /// </summary>
    public interface IPatientRepository
    {
        /// <summary>
        /// Finds patient details by their Force Number.
        /// </summary>
        /// <param name="forceNumber">The force number to search for.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="PatientDetailDto"/> or null if not found.</returns>
        Task<PatientDetailDto?> FindByForceNumberAsync(string forceNumber, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves basic patient information (ID, Name, Rank) by their internal Patient ID.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="PatientBasicInfoDto"/> or null if not found.</returns>
        Task<PatientBasicInfoDto?> GetPatientBasicInfoByIdAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the personal information of a patient.
        /// </summary>
        /// <param name="patientInfo">The DTO containing the updated personal information.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdatePatientPersonalInfoAsync(EditPatientPersonalInfoDto patientInfo, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves detailed patient information by their internal Patient ID.
        /// </summary>
        /// <param name="patientId">The unique identifier for the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="PatientDetailDto"/> if found; otherwise, null.</returns>
        Task<PatientDetailDto?> GetPatientDetailByIdAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a patient's contact information for editing.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An <see cref="EditPatientContactInfoDto"/> if found; otherwise, null.</returns>
        Task<EditPatientContactInfoDto?> GetPatientContactInfoForEditAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates a patient's contact information.
        /// </summary>
        /// <param name="contactInfo">The DTO containing the updated contact information.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdatePatientContactInfoAsync(EditPatientContactInfoDto contactInfo, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a patient's emergency contact information for editing.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An <see cref="EditPatientEmergencyContactInfoDto"/> if found; otherwise, null.</returns>
        Task<EditPatientEmergencyContactInfoDto?> GetPatientEmergencyContactInfoForEditAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates a patient's emergency contact information.
        /// </summary>
        /// <param name="emergencyContactInfo">The DTO containing the updated emergency contact information.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdatePatientEmergencyContactInfoAsync(EditPatientEmergencyContactInfoDto emergencyContactInfo, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new patient record in the database.
        /// </summary>
        /// <param name="newPatientData">The DTO containing the data for the new patient.</param>
        /// <param name="createdByUserId">The ID of the user who created the patient record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created patient, or 0 if creation failed.</returns>
        Task<int> CreatePatientAsync(CreatePatientInputDto newPatientData, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated list of all patients, with optional search.
        /// </summary>
        /// <param name="options">Options for filtering, searching, and pagination.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the enumerable of <see cref="PatientBasicInfoDto"/> and the total count.</returns>
        Task<(IEnumerable<PatientBasicInfoDto> Items, int TotalCount)> GetAllPatientsAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}