using carestream.core.dtos.patient; // For PatientMedicalHistoryDto, CreateUpdatePatientMedicalHistoryDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions (though not directly used in this implementation, included for consistency)
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing patient medical history data persistence.
    /// </summary>
    public class PatientMedicalHistoryRepository : BaseRepository, IPatientMedicalHistoryRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PatientMedicalHistoryRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public PatientMedicalHistoryRepository(IConfiguration configuration, ILogger<PatientMedicalHistoryRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PatientMedicalHistoryDto>> GetMedicalHistoryByPatientIdAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    pmh.history_id AS HistoryId,
                    pmh.patient_id AS PatientId,
                    pmh.type AS Type,
                    pmh.description AS Description,
                    pmh.onset_date AS OnsetDate,
                    pmh.resolution_date AS ResolutionDate,
                    pmh.severity AS Severity,
                    pmh.notes AS Notes,
                    pmh.recorded_at AS RecordedAt,
                    pmh.recorded_by_user_id AS RecordedByUserId,
                    u.first_name || ' ' || u.last_name AS RecordedByUserName,
                    pmh.is_active AS IsActive
                FROM app.patient_medical_history pmh
                LEFT JOIN app.users u ON pmh.recorded_by_user_id = u.user_id
                WHERE pmh.patient_id = @PatientIdParam
                ORDER BY pmh.onset_date DESC, pmh.recorded_at DESC;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<PatientMedicalHistoryDto>(sql, new { PatientIdParam = patientId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<PatientMedicalHistoryDto>();
        }

        /// <inheritdoc/>
        public async Task<PatientMedicalHistoryDto?> GetMedicalHistoryByIdAsync(int historyId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    pmh.history_id AS HistoryId,
                    pmh.patient_id AS PatientId,
                    pmh.type AS Type,
                    pmh.description AS Description,
                    pmh.onset_date AS OnsetDate,
                    pmh.resolution_date AS ResolutionDate,
                    pmh.severity AS Severity,
                    pmh.notes AS Notes,
                    pmh.recorded_at AS RecordedAt,
                    pmh.recorded_by_user_id AS RecordedByUserId,
                    u.first_name || ' ' || u.last_name AS RecordedByUserName,
                    pmh.is_active AS IsActive
                FROM app.patient_medical_history pmh
                LEFT JOIN app.users u ON pmh.recorded_by_user_id = u.user_id
                WHERE pmh.history_id = @HistoryIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<PatientMedicalHistoryDto>(sql, new { HistoryIdParam = historyId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreateMedicalHistoryEntryAsync(CreateUpdatePatientMedicalHistoryDto historyData, int patientId, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.patient_medical_history (
                    patient_id, type, description, onset_date, resolution_date, severity, notes,
                    recorded_at, recorded_by_user_id, is_active
                ) VALUES (
                    @PatientId, @Type, @Description, @OnsetDate, @ResolutionDate, @Severity, @Notes,
                    NOW(), @RecordedByUserId, @IsActive
                )
                RETURNING history_id;";

            var parameters = new DynamicParameters(historyData);
            parameters.Add("PatientId", patientId); // Ensure patientId is passed from method param, not from DTO if it's not there
            parameters.Add("RecordedByUserId", recordedByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateMedicalHistoryEntryAsync(CreateUpdatePatientMedicalHistoryDto historyData, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (historyData.HistoryId <= 0) return false;

            const string sql = @"
                UPDATE app.patient_medical_history
                SET
                    type = @Type,
                    description = @Description,
                    onset_date = @OnsetDate,
                    resolution_date = @ResolutionDate,
                    severity = @Severity,
                    notes = @Notes,
                    is_active = @IsActive,
                    recorded_at = NOW(), -- Updating recorded_at for update
                    recorded_by_user_id = @RecordedByUserId -- Updating recorded_by_user_id for update
                WHERE history_id = @HistoryId;";

            var parameters = new DynamicParameters(historyData);
            parameters.Add("RecordedByUserId", recordedByUserId); // Ensure this is explicitly set from method param

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateMedicalHistoryEntryAsync(int historyId, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (historyId <= 0) return false;

            const string sql = @"
                UPDATE app.patient_medical_history
                SET
                    is_active = FALSE,
                    recorded_at = NOW(),
                    recorded_by_user_id = @RecordedByUserId
                WHERE history_id = @HistoryId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { HistoryId = historyId, RecordedByUserId = recordedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> ActivateMedicalHistoryEntryAsync(int historyId, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (historyId <= 0) return false;

            const string sql = @"
                UPDATE app.patient_medical_history
                SET
                    is_active = TRUE,
                    recorded_at = NOW(),
                    recorded_by_user_id = @RecordedByUserId
                WHERE history_id = @HistoryId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { HistoryId = historyId, RecordedByUserId = recordedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }
    }
}