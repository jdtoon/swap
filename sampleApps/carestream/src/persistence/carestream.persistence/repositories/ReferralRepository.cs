using carestream.core.dtos.consultation; // For ReferralDto, CreateUpdateReferralDto
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq; // For Enumerable.Empty()
using System.Text; // For StringBuilder (if needed)
using System.Threading.Tasks;

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing patient referral data persistence.
    /// </summary>
    public class ReferralRepository : BaseRepository, IReferralRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public ReferralRepository(IConfiguration configuration, ILogger<ReferralRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<ReferralDto?> GetReferralByIdAsync(int referralId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    r.referral_id AS ReferralId,
                    r.visit_id AS VisitId,
                    r.patient_id AS PatientId,
                    r.referred_by_user_id AS ReferredByUserId,
                    rb.first_name || ' ' || rb.last_name AS ReferredByUserName,
                    r.referred_to_department_id AS ReferredToDepartmentId,
                    d.name AS ReferredToDepartmentName,
                    r.referred_to_facility_id AS ReferredToFacilityId,
                    f.name AS ReferredToFacilityName,
                    r.referral_reason AS ReferralReason,
                    r.referral_notes AS ReferralNotes,
                    r.referral_date AS ReferralDate,
                    r.status AS Status,
                    r.completed_date AS CompletedDate,
                    r.completed_by_user_id AS CompletedByUserId,
                    cb.first_name || ' ' || cb.last_name AS CompletedByUserName
                FROM app.referrals r
                JOIN app.visits v ON r.visit_id = v.visit_id -- For facility filtering
                LEFT JOIN app.users rb ON r.referred_by_user_id = rb.user_id
                LEFT JOIN app.departments d ON r.referred_to_department_id = d.department_id
                LEFT JOIN app.facilities f ON r.referred_to_facility_id = f.facility_id
                LEFT JOIN app.users cb ON r.completed_by_user_id = cb.user_id
                WHERE r.referral_id = @ReferralIdParam
                  AND v.facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<ReferralDto>(sql, new { ReferralIdParam = referralId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ReferralDto>> GetReferralsByVisitIdAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    r.referral_id AS ReferralId,
                    r.visit_id AS VisitId,
                    r.patient_id AS PatientId,
                    r.referred_by_user_id AS ReferredByUserId,
                    rb.first_name || ' ' || rb.last_name AS ReferredByUserName,
                    r.referred_to_department_id AS ReferredToDepartmentId,
                    d.name AS ReferredToDepartmentName,
                    r.referred_to_facility_id AS ReferredToFacilityId,
                    f.name AS ReferredToFacilityName,
                    r.referral_reason AS ReferralReason,
                    r.referral_notes AS ReferralNotes,
                    r.referral_date AS ReferralDate,
                    r.status AS Status,
                    r.completed_date AS CompletedDate,
                    r.completed_by_user_id AS CompletedByUserId,
                    cb.first_name || ' ' || cb.last_name AS CompletedByUserName
                FROM app.referrals r
                JOIN app.visits v ON r.visit_id = v.visit_id -- For facility filtering
                LEFT JOIN app.users rb ON r.referred_by_user_id = rb.user_id
                LEFT JOIN app.departments d ON r.referred_to_department_id = d.department_id
                LEFT JOIN app.facilities f ON r.referred_to_facility_id = f.facility_id
                LEFT JOIN app.users cb ON r.completed_by_user_id = cb.user_id
                WHERE r.visit_id = @VisitIdParam
                  AND v.facility_id = @FacilityId
                ORDER BY r.referral_date DESC;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<ReferralDto>(sql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<ReferralDto>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ReferralDto>> GetPatientReferralHistoryAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    r.referral_id AS ReferralId,
                    r.visit_id AS VisitId,
                    r.patient_id AS PatientId,
                    r.referred_by_user_id AS ReferredByUserId,
                    rb.first_name || ' ' || rb.last_name AS ReferredByUserName,
                    r.referred_to_department_id AS ReferredToDepartmentId,
                    d.name AS ReferredToDepartmentName,
                    r.referred_to_facility_id AS ReferredToFacilityId,
                    f.name AS ReferredToFacilityName,
                    r.referral_reason AS ReferralReason,
                    r.referral_notes AS ReferralNotes,
                    r.referral_date AS ReferralDate,
                    r.status AS Status,
                    r.completed_date AS CompletedDate,
                    r.completed_by_user_id AS CompletedByUserId,
                    cb.first_name || ' ' || cb.last_name AS CompletedByUserName
                FROM app.referrals r
                JOIN app.visits v ON r.visit_id = v.visit_id -- For facility filtering (even though patient is global, context is visit)
                LEFT JOIN app.users rb ON r.referred_by_user_id = rb.user_id
                LEFT JOIN app.departments d ON r.referred_to_department_id = d.department_id
                LEFT JOIN app.facilities f ON r.referred_to_facility_id = f.facility_id
                LEFT JOIN app.users cb ON r.completed_by_user_id = cb.user_id
                WHERE r.patient_id = @PatientIdParam
                  AND v.facility_id = @FacilityId -- Filter by current facility's patient referrals
                ORDER BY r.referral_date DESC;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<ReferralDto>(sql, new { PatientIdParam = patientId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<ReferralDto>();
        }

        /// <inheritdoc/>
        public async Task<int> CreateReferralAsync(CreateUpdateReferralDto referralData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.referrals (
                    visit_id, patient_id, referred_by_user_id,
                    referred_to_department_id, referred_to_facility_id,
                    referral_reason, referral_notes, referral_date, status
                ) VALUES (
                    @VisitId, @PatientId, @ReferredByUserId,
                    @ReferredToDepartmentId, @ReferredToFacilityId,
                    @ReferralReason, @ReferralNotes, NOW(), @Status
                )
                RETURNING referral_id;";

            var parameters = new DynamicParameters(referralData);
            // referralData.ReferredByUserId is assumed to be set in DTO or passed from service.
            // referralData.Status should be set to initial value (e.g., "Pending").

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateReferralStatusAsync(int referralId, string newStatus, int? completedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (referralId <= 0) return false;

            const string sql = @"
                UPDATE app.referrals r
                SET
                    status = @NewStatus,
                    completed_date = CASE WHEN @NewStatus = 'Completed' THEN NOW() ELSE r.completed_date END, -- Set completion date only if status is 'Completed'
                    completed_by_user_id = COALESCE(@CompletedByUserId, r.completed_by_user_id), -- Update completed_by_user_id
                    updated_at = NOW() -- Update general updated_at
                FROM app.visits v -- For facility filtering
                WHERE r.visit_id = v.visit_id
                  AND r.referral_id = @ReferralIdParam
                  AND v.facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ReferralIdParam = referralId, NewStatus = newStatus, CompletedByUserId = completedByUserId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateReferralAsync(CreateUpdateReferralDto referralData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (referralData.ReferralId <= 0) return false;

            const string sql = @"
                UPDATE app.referrals r
                SET
                    referred_to_department_id = @ReferredToDepartmentId,
                    referred_to_facility_id = @ReferredToFacilityId,
                    referral_reason = @ReferralReason,
                    referral_notes = @ReferralNotes,
                    updated_at = NOW()
                FROM app.visits v -- For facility filtering
                WHERE r.visit_id = v.visit_id
                  AND r.referral_id = @ReferralId
                  AND v.facility_id = @FacilityId;";

            var parameters = new DynamicParameters(referralData);
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId); // Ensure facility_id param is added

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }
    }
}