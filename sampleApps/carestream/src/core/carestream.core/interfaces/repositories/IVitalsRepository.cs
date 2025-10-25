using carestream.core.dtos.vitals; // For VitalsCaptureInputDto
using System.Data;                 // For IDbConnection, IDbTransaction

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations related to patient vital signs.
    /// </summary>
    public interface IVitalsRepository
    {
        /// <summary>
        /// Saves a new set of vital signs to the database.
        /// </summary>
        /// <param name="vitalsData">The DTO containing the vital signs to be saved.</param>
        /// <param name="connection">Optional existing database connection to use for the operation.</param>
        /// <param name="transaction">Optional existing database transaction to enlist in.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the ID of the newly created vitals record, or 0 if failed.
        /// </returns>
        Task<int> CreateVitalsRecordAsync(VitalsCaptureInputDto vitalsData, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves vital signs recorded for a specific visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="connection">Optional existing database connection to use for the operation.</param>
        /// <param name="transaction">Optional existing database transaction to enlist in.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the <see cref="VitalsCaptureInputDto"/> (or a specific VitalsDisplayDto)
        /// for the visit, or null if no vitals are recorded for that visit.
        /// </returns>
        Task<VitalsCaptureInputDto?> GetVitalsForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}