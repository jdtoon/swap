using carestream.core.interfaces.repositories;
using carestream.core.dtos.visit;
using carestream.core.dtos.vitals;
using carestream.core.dtos.doctor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using Npgsql;
using System.Data;
using carestream.core.dtos.patientadmin;
using carestream.core.dtos.shared;
using System.Text;
using carestream.core.infrastructure;
using carestream.core.enums;
using System.Collections.Generic;
using System.Linq;
using carestream.core.dtos.consultation;
using System;

namespace carestream.persistence.repositories
{
    public class VisitRepository : BaseRepository, IVisitRepository
    {
        public VisitRepository(IConfiguration configuration, ILogger<VisitRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <summary>
        /// Updates the status of a visit.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="newStatus">The new status for the visit, using VisitStatus enum.</param>
        /// <param name="assignedOfficerUserId"></param>
        /// <returns></returns>
        public Task<bool> UpdateVisitStatusAsync(int visitId, VisitStatus newStatus, int? assignedOfficerUserId = null, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.visits
                SET status = @NewStatus,
                    assigned_officer_user_id = COALESCE(@AssignedOfficerUserId, assigned_officer_user_id),
                    updated_at = NOW()
                    -- updated_by_user_id = @ActionedByUserId -- Ensure this is uncommented if the column exists AND you pass it
                WHERE visit_id = @VisitId AND facility_id = @FacilityId;";

            return ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { VisitId = visitId, NewStatus = newStatus.ToString(), AssignedOfficerUserId = assignedOfficerUserId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <summary>
        /// Finds the latest active (non-terminal status) visit for a specific patient.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An <see cref="ActiveVisitDto"/> representing the active visit, or null if none found.</returns>
        public async Task<ActiveVisitDto?> FindLatestActiveVisitAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    v.visit_id AS VisitId,
                    v.patient_id AS PatientId,
                    v.visit_timestamp AS VisitTimestamp,
                    v.brief_reason AS BriefReason,
                    v.status AS Status,
                    v.checked_in_by_user_id AS CheckedInByUserId,
                    u.first_name AS CheckedInByFirstName,
                    u.last_name AS CheckedInByLastName,
                    u.rank AS CheckedInByRank,
                    v.assigned_officer_user_id AS AssignedOfficerUserId,
                    ao.first_name AS AssignedOfficerFirstName,
                    ao.last_name AS AssignedOfficerLastName,
                    ao.rank AS AssignedOfficerRank
                FROM app.visits v
                LEFT JOIN app.users u ON v.checked_in_by_user_id = u.user_id
                LEFT JOIN app.users ao ON v.assigned_officer_user_id = ao.user_id
                WHERE v.patient_id = @PatientId
                  AND v.status NOT IN (@DischargedStatus, @AdministrativelyClosedStatus)
                  AND v.facility_id = @FacilityId
                ORDER BY v.visit_timestamp DESC
                LIMIT 1;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var activeVisit = await conn.QueryFirstOrDefaultAsync<ActiveVisitDto>(sql, new { PatientId = patientId, DischargedStatus = VisitStatus.Discharged.ToString(), AdministrativelyClosedStatus = VisitStatus.AdministrativelyClosed.ToString(), FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return activeVisit;
            }, connection, transaction);
        }

        /// <summary>
        /// Creates a new visit record in the database.
        /// </summary>
        /// <param name="patientId">The ID of the patient associated with this visit.</param>
        /// <param name="status">The initial status of the visit (e.g., 'WaitingForVitals'), using VisitStatus enum.</param>
        /// <param name="checkedInByUserId">The ID of the user who performed the check-in.</param>
        /// <param name="briefReason">A brief reason for the visit.</param>
        /// <param name="additionalNotes">Optional additional notes for the visit.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created visit.</returns>
        public async Task<int> CreateVisitAsync(int patientId, VisitStatus status, int checkedInByUserId, string briefReason, string? additionalNotes, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.visits (patient_id, brief_reason, additional_notes, status, checked_in_by_user_id, facility_id)
                VALUES (@PatientId, @BriefReason, @AdditionalNotes, @Status, @CheckedInByUserId, @FacilityId)
                RETURNING visit_id;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var visitId = await conn.QuerySingleOrDefaultAsync<int>(sql, new
                {
                    PatientId = patientId,
                    BriefReason = briefReason,
                    AdditionalNotes = additionalNotes,
                    Status = status.ToString(),
                    CheckedInByUserId = checkedInByUserId,
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans);
                return visitId;
            }, connection, transaction);
        }

        /// <summary>
        /// Overload for <see cref="CreateVisitAsync(int, VisitStatus, int, string, string?, IDbConnection?, IDbTransaction?)"/> without brief reason and additional notes.
        /// </summary>
        public Task<int> CreateVisitAsync(int patientId, VisitStatus initialStatus, int checkedInByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            return CreateVisitAsync(patientId, initialStatus, checkedInByUserId, null, null, connection, transaction);
        }

        /// <summary>
        /// Updates the status of an existing visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <param name="newStatus">The new status for the visit, using VisitStatus enum.</param>
        /// <param name="actionedByUserId">The ID of the user performing the status update.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        public async Task<bool> UpdateVisitStatusAsync(int visitId, VisitStatus newStatus, int actionedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.visits
                SET
                    status = @NewStatus,
                    updated_at = NOW()
                WHERE visit_id = @VisitId AND facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { VisitId = visitId, NewStatus = newStatus.ToString(), ActionedByUserId = actionedByUserId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <summary>
        /// Updates the visit status and the assigned officer for a visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <param name="newStatus">The new status for the visit, using VisitStatus enum.</param>
        /// <param name="assignedOfficerUserId">The ID of the user to assign as the officer.</param>
        /// <param name="actionedByUserId">The ID of the user performing the update.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        public async Task<bool> UpdateVisitStatusAndAssignedOfficerAsync(int visitId, VisitStatus newStatus, int? assignedOfficerUserId, int actionedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.visits
                SET
                    status = @NewStatus,
                    assigned_officer_user_id = @AssignedOfficerUserId,
                    updated_at = NOW()
                WHERE visit_id = @VisitId AND facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { VisitId = visitId, NewStatus = newStatus.ToString(), AssignedOfficerUserId = assignedOfficerUserId, ActionedByUserId = actionedByUserId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public Task<VitalsDashboardStatsDto> GetVitalsDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    SUM(CASE WHEN v.status = @StatusWaitingForVitals THEN 1 ELSE 0 END)::int AS WaitingForVitals,
                    SUM(CASE WHEN v.status = @StatusVitalsInProgress THEN 1 ELSE 0 END)::int AS VitalsInProgress,
                    SUM(CASE WHEN v.status = @StatusReadyForDoctor THEN 1 ELSE 0 END)::int AS ReadyForDoctor
                FROM app.visits v
                WHERE v.facility_id = @FacilityId;";

            return ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var stats = await conn.QuerySingleOrDefaultAsync<VitalsDashboardStatsDto>(sql, new
                {
                    StatusWaitingForVitals = VisitStatus.WaitingForVitals.ToString(),
                    StatusVitalsInProgress = VisitStatus.VitalsInProgress.ToString(),
                    StatusReadyForDoctor = VisitStatus.ReadyForDoctor.ToString(),
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans);
                return stats ?? new VitalsDashboardStatsDto();
            }, connection, transaction);
        }

        public Task<IEnumerable<VitalsQueueItemDto>> GetVitalsQueueAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT v.visit_id AS VisitId, v.patient_id AS PatientId, p.first_name || ' ' || p.last_name AS PatientName, p.rank AS Rank,
                       CASE WHEN p.date_of_birth IS NOT NULL THEN EXTRACT(YEAR FROM AGE(p.date_of_birth))::int ELSE NULL END AS Age,
                       v.visit_timestamp AS CheckinTimestamp, 'Normal' AS Priority
                FROM app.visits v JOIN app.patients p ON v.patient_id = p.patient_id
                WHERE (v.status = @StatusWaitingForVitals OR v.status = @StatusVitalsInProgress)
                  AND v.facility_id = @FacilityId
                ORDER BY v.visit_timestamp ASC;";

            return ExecuteWithConnectionAsync(
                (conn, trans) => conn.QueryAsync<VitalsQueueItemDto>(sql, new
                {
                    StatusWaitingForVitals = VisitStatus.WaitingForVitals.ToString(),
                    StatusVitalsInProgress = VisitStatus.VitalsInProgress.ToString(),
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans),
                connection, transaction);
        }

        public Task<DoctorDashboardStatsDto> GetDoctorDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT SUM(CASE WHEN v.status = @StatusReadyForDoctor OR v.status = @StatusConsultationInProgress THEN 1 ELSE 0 END)::int AS TotalWaitingForDoctor,
                       0 AS UrgentCasesCount, 0 AS HighPriorityCasesCount, 'N/A' AS AverageWaitTime
                FROM app.visits v
                WHERE v.facility_id = @FacilityId;";

            return ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var stats = await conn.QuerySingleOrDefaultAsync<DoctorDashboardStatsDto>(sql, new
                {
                    StatusReadyForDoctor = VisitStatus.ReadyForDoctor.ToString(),
                    StatusConsultationInProgress = VisitStatus.ConsultationInProgress.ToString(),
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans);
                return stats ?? new DoctorDashboardStatsDto { TotalWaitingForDoctor = 0, AverageWaitTime = "0m" };
            }, connection, transaction);
        }

        public Task<IEnumerable<DoctorQueueItemDto>> GetDoctorPatientQueueAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT v.visit_id AS VisitId, v.patient_id AS PatientId, p.first_name || ' ' || p.last_name AS PatientName, p.rank AS Rank,
                       CASE WHEN p.date_of_birth IS NOT NULL THEN EXTRACT(YEAR FROM AGE(p.date_of_birth))::int ELSE NULL END AS Age,
                       p.gender AS Gender, v.visit_timestamp AS ReadyForDoctorTimestamp,
                       COALESCE(v_priority.priority_level, 'Normal') AS Priority
                FROM app.visits v
                JOIN app.patients p ON v.patient_id = p.patient_id
                LEFT JOIN (SELECT visit_id, CASE WHEN mark_as_urgent THEN 'Urgent' ELSE 'Normal' END as priority_level FROM app.vital_signs) v_priority ON v.visit_id = v_priority.visit_id
                WHERE v.status = @StatusReadyForDoctor
                  AND v.facility_id = @FacilityId
                ORDER BY CASE COALESCE(v_priority.priority_level, 'Normal') WHEN 'Urgent' THEN 1 WHEN 'High' THEN 2 WHEN 'Normal' THEN 3 ELSE 4 END, v.visit_timestamp ASC;";

            return ExecuteWithConnectionAsync(
                (conn, trans) => conn.QueryAsync<DoctorQueueItemDto>(sql, new
                {
                    StatusReadyForDoctor = VisitStatus.ReadyForDoctor.ToString(),
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans),
                connection, transaction);
        }

        public async Task<BasicVisitInfoDto?> GetBasicVisitInfoByIdAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    visit_id AS VisitId,
                    patient_id AS PatientId,
                    brief_reason AS BriefReason,
                    visit_timestamp AS VisitTimestamp,
                    status as Status,
                    assigned_officer_user_id as AssignedOfficerUserId
                FROM app.visits
                WHERE visit_id = @VisitIdParam AND facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<BasicVisitInfoDto>(sql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        public Task<IEnumerable<DoctorQueueItemDto>> GetInProgressConsultationsForDoctorAsync(
            int doctorUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT DISTINCT
                    v.visit_id AS VisitId,
                    v.patient_id AS PatientId,
                    p.rank || '. ' || p.first_name || ' ' || p.last_name AS PatientName,
                    p.date_of_birth AS DateOfBirth,
                    p.gender AS Gender,
                    p.force_number AS ForceNumber,
                    v.visit_timestamp AS CheckinTimestamp,
                    v.status AS Status,
                    COALESCE(vs.mark_as_urgent, FALSE) AS IsUrgent,
                    CASE
                        WHEN COALESCE(vs.mark_as_urgent, FALSE) = TRUE THEN 'Urgent'
                        ELSE 'Normal'
                    END AS Priority,
                    v.visit_timestamp AS ReadyForDoctorTimestamp
                FROM app.visits v
                JOIN app.patients p ON v.patient_id = p.patient_id
                LEFT JOIN app.vital_signs vs ON v.visit_id = vs.visit_id
                WHERE v.status = @ConsultationInProgressStatus
                  AND v.assigned_officer_user_id = @DoctorUserId
                  AND v.facility_id = @FacilityId
                ORDER BY v.visit_timestamp ASC;";

            return ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var queueItems = await conn.QueryAsync<DoctorQueueItemDto>(sql, new
                {
                    DoctorUserId = doctorUserId,
                    ConsultationInProgressStatus = VisitStatus.ConsultationInProgress.ToString(),
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans);
                return queueItems ?? Enumerable.Empty<DoctorQueueItemDto>();
            }, connection, transaction);
        }

        public Task<string?> GetDoctorNotesAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = "SELECT doctor_consultation_notes FROM app.visits WHERE visit_id = @VisitIdParam AND facility_id = @FacilityId;";

            return ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<string?>(sql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        public Task<bool> UpdateDoctorNotesAsync(int visitId, string? notes, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.visits
                SET doctor_consultation_notes = @Notes,
                    updated_at = NOW()
                    -- updated_by_user_id = @ActionedByUserId -- Add this if you have it in schema
                WHERE visit_id = @VisitIdParam AND facility_id = @FacilityId;";

            return ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { VisitIdParam = visitId, Notes = notes, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<(IEnumerable<PatientQueueItemDto> Items, int TotalCount)> GetPatientAdminQueueAsync(
            FilterAndPaginationOptions options,
            IDbConnection? connection = null,
            IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.visits v
                JOIN app.patients p ON v.patient_id = p.patient_id
                LEFT JOIN app.users checked_in_user ON v.checked_in_by_user_id = checked_in_user.user_id
                LEFT JOIN app.users assigned_officer ON v.assigned_officer_user_id = assigned_officer.user_id
                LEFT JOIN app.vital_signs vs ON v.visit_id = vs.visit_id
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            whereClauses.Add("v.facility_id = @FacilityId");
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);

            whereClauses.Add("v.status = ANY(@RelevantStatuses)");
            parameters.Add("RelevantStatuses", new List<string> {
                VisitStatus.WaitingForVitals.ToString(),
                VisitStatus.VitalsInProgress.ToString(),
                VisitStatus.ReadyForDoctor.ToString(),
                VisitStatus.ConsultationInProgress.ToString(),
                VisitStatus.InTreatment.ToString(),
                VisitStatus.PendingPrescription.ToString()
            });

            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1))
            {
                whereClauses.Add("(p.first_name ILIKE @PatientSearch OR p.last_name ILIKE @PatientSearch OR p.force_number ILIKE @PatientSearch)");
                parameters.Add("PatientSearch", $"%{options.SearchTerm1}%");
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(DISTINCT v.visit_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    v.visit_id AS VisitId,
                    v.patient_id AS PatientId,
                    p.first_name || ' ' || p.last_name AS PatientName,
                    p.rank AS Rank,
                    p.force_number AS ForceNumber,
                    v.status AS Status,
                    v.visit_timestamp AS RelevantTimestamp,
                    COALESCE(CASE WHEN vs.mark_as_urgent = TRUE THEN 'Urgent' ELSE 'Normal' END, 'Normal') AS Priority,
                    CASE
                        WHEN v.status = @ConsultationInProgressStatus THEN assigned_officer.rank || '. ' || assigned_officer.last_name
                        WHEN v.status = @VitalsInProgressStatus OR v.status = @InTreatmentStatus THEN 'Vitals Room'
                        ELSE ''
                    END AS AssignedTo
                {baseSql.ToString()}
            ");

            itemsSql.Append(" ORDER BY CASE COALESCE(CASE WHEN vs.mark_as_urgent = TRUE THEN 'Urgent' ELSE 'Normal' END, 'Normal') WHEN 'Urgent' THEN 1 WHEN 'High' THEN 2 WHEN 'Normal' THEN 3 ELSE 4 END, v.visit_timestamp ASC");
            itemsSql.Append(" LIMIT @PageSize OFFSET @Offset;");

            parameters.Add("ConsultationInProgressStatus", VisitStatus.ConsultationInProgress.ToString());
            parameters.Add("VitalsInProgressStatus", VisitStatus.VitalsInProgress.ToString());
            parameters.Add("InTreatmentStatus", VisitStatus.InTreatment.ToString());


            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<PatientQueueItemDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<PatientQueueItemDto>(), totalCount);
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<DD50ReportDto?> GetDD50ReportDataAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // DD50 data combines information from multiple tables related to a visit.
            const string sql = @"
                SELECT
                    -- Patient Personal Details (from app.patients)
                    p.patient_id AS PatientId,
                    p.force_number AS PatientForceNumber,
                    p.rank AS PatientRank,
                    p.first_name AS PatientFirstName,
                    p.last_name AS PatientLastName,
                    p.date_of_birth AS PatientDateOfBirth,
                    p.gender AS PatientGender,
                    p.unit AS PatientUnit,
                    -- Patient Medical History (from app.patient_medical_history) - Concatenate for report purposes
                    (SELECT STRING_AGG(pmh.description, '; ') FROM app.patient_medical_history pmh WHERE pmh.patient_id = p.patient_id AND pmh.type = 'Allergy' AND pmh.is_active = TRUE) AS Allergies,
                    (SELECT STRING_AGG(pmh.description, '; ') FROM app.patient_medical_history pmh WHERE pmh.patient_id = p.patient_id AND pmh.type = 'Condition' AND pmh.is_active = TRUE) AS PreviousMedicalConditions,
                    -- Vital Signs (from app.vital_signs - latest for this visit)
                    vs.blood_pressure_systolic AS VitalsBloodPressureSystolic,
                    vs.blood_pressure_diastolic AS VitalsBloodPressureDiastolic,
                    vs.heart_rate AS VitalsHeartRate,
                    vs.temperature AS VitalsTemperature,
                    vs.respiratory_rate AS VitalsRespiratoryRate,
                    vs.oxygen_saturation AS VitalsOxygenSaturation,
                    vs.pain_level AS VitalsPainLevel,
                    -- Visit Assessment (from app.visit_assessments - latest for this visit)
                    va.physical_exam_findings AS AssessmentPhysicalExamFindings,
                    va.cardiovascular_notes AS AssessmentCardiovascularNotes,
                    va.respiratory_notes AS AssessmentRespiratoryNotes,
                    va.musculoskeletal_notes AS AssessmentMusculoskeletalNotes,
                    va.neurological_notes AS AssessmentNeurologicalNotes,
                    va.psychological_notes AS AssessmentPsychologicalNotes,
                    va.other_systems_notes AS AssessmentOtherSystemsNotes,
                    va.medical_classification AS AssessmentMedicalClassification,
                    va.deployment_status AS AssessmentDeploymentStatus,
                    va.validity_period_months AS AssessmentValidityPeriodMonths,
                    va.restrictions AS AssessmentRestrictions,
                    -- Diagnoses (from app.visit_diagnoses) - Concatenate for report purposes
                    (SELECT STRING_AGG(icd.code || ' - ' || icd.description, '; ') FROM app.visit_diagnoses vd JOIN app.icd10_codes icd ON vd.icd10_code_id = icd.icd10_code_id WHERE vd.visit_id = v.visit_id) AS Diagnoses,
                    -- Procedures (from app.visit_procedures) - Concatenate for report purposes
                    (SELECT STRING_AGG(proc.code || ' - ' || proc.name, '; ') FROM app.visit_procedures vp JOIN app.procedures proc ON vp.procedure_id = proc.procedure_id WHERE vp.visit_id = v.visit_id) AS Procedures,
                    -- Examining Officer Details (from app.users)
                    eo.first_name || ' ' || eo.last_name AS ExaminingOfficerName,
                    eo.rank AS ExaminingOfficerRank,
                    eo.department AS ExaminingOfficerDepartment,
                    NOW() AS ReportGeneratedDate -- Current date for report generation
                FROM app.visits v
                JOIN app.patients p ON v.patient_id = p.patient_id
                LEFT JOIN app.vital_signs vs ON v.visit_id = vs.visit_id
                LEFT JOIN app.visit_assessments va ON v.visit_id = va.visit_id
                LEFT JOIN app.users eo ON v.assigned_officer_user_id = eo.user_id
                WHERE v.visit_id = @VisitIdParam
                  AND v.facility_id = @FacilityId;
            ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var reportData = await conn.QueryFirstOrDefaultAsync<DD50ReportDto>(sql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);

                return reportData;
            }, connection, transaction);
        }

        /// <summary>
        /// Synchronizes ICD-10 diagnosis codes for a specific patient visit.
        /// Inserts new links, updates existing ones (recorded_by_user_id, recorded_at),
        /// and deletes any links that are no longer in the provided icd10CodeIds list for this visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="icd10CodeIds">A collection of ICD-10 code IDs to link.</param>
        /// <param name="recordedByUserId">The ID of the user who recorded the diagnosis.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the linking was successful, false otherwise.</returns>
        public async Task<bool> LinkVisitToDiagnosisAsync(int visitId, int patientId, IEnumerable<int> icd10CodeIds, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Even if icd10CodeIds is empty, we still need to execute the DELETE part
            // to remove all existing links for this visit.
            if (icd10CodeIds == null)
            {
                icd10CodeIds = Enumerable.Empty<int>(); // Ensure it's not null for UNNEST
            }

            const string sql = @"
                WITH input_diagnoses AS (
                    SELECT icd10_code_id
                    FROM UNNEST(@Icd10CodeIds) AS icd10_code_id
                ),
                inserted_or_updated AS (
                    INSERT INTO app.visit_diagnoses (visit_id, icd10_code_id, recorded_by_user_id, recorded_at)
                    SELECT @VisitId, id.icd10_code_id, @RecordedByUserId, NOW()
                    FROM input_diagnoses id
                    ON CONFLICT (visit_id, icd10_code_id) DO UPDATE SET
                        recorded_by_user_id = EXCLUDED.recorded_by_user_id,
                        recorded_at = NOW()
                    RETURNING visit_id, icd10_code_id
                )
                DELETE FROM app.visit_diagnoses
                WHERE visit_id = @VisitId
                  AND icd10_code_id NOT IN (SELECT icd10_code_id FROM input_diagnoses);
            ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                // Dapper's ExecuteAsync returns the number of rows affected by the LAST statement in the batch.
                // For a DELETE, it will be the count of deleted rows. For INSERT/UPDATE, it's those counts.
                // When combining, it often returns the count of the final statement (DELETE here).
                // A return of >= 0 still indicates success.
                var affectedRows = await conn.ExecuteAsync(sql, new
                {
                    VisitId = visitId,
                    PatientId = patientId, // Still not used in SQL but kept for consistency
                    Icd10CodeIds = icd10CodeIds,
                    RecordedByUserId = recordedByUserId
                }, transaction: trans);
                return affectedRows >= 0;
            }, connection, transaction);
        }

        /// <summary>
        /// Synchronizes medical procedures for a specific patient visit.
        /// Inserts new links, updates existing ones (performed_by_user_id, performed_at),
        /// and deletes any links that are no longer in the provided procedureIds list for this visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="procedureIds">A collection of procedure IDs to link.</param>
        /// <param name="performedByUserId">The ID of the user who performed/recorded the procedure.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the linking was successful, false otherwise.</returns>
        public async Task<bool> LinkVisitToProcedureAsync(int visitId, int patientId, IEnumerable<int> procedureIds, int performedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Even if procedureIds is empty, we still need to execute the DELETE part
            // to remove all existing links for this visit.
            if (procedureIds == null)
            {
                procedureIds = Enumerable.Empty<int>(); // Ensure it's not null for UNNEST
            }

            const string sql = @"
            WITH input_procedures AS (
                SELECT procedure_id
                FROM UNNEST(@ProcedureIds) AS procedure_id
            ),
            inserted_or_updated AS (
                INSERT INTO app.visit_procedures (visit_id, procedure_id, performed_by_user_id, performed_at)
                SELECT @VisitId, ip.procedure_id, @PerformedByUserId, NOW()
                FROM input_procedures ip
                ON CONFLICT (visit_id, procedure_id) DO UPDATE SET
                    performed_by_user_id = EXCLUDED.performed_by_user_id,
                    performed_at = NOW()
                RETURNING visit_id, procedure_id
            )
            DELETE FROM app.visit_procedures
            WHERE visit_id = @VisitId
              AND procedure_id NOT IN (SELECT procedure_id FROM input_procedures);
        ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new
                {
                    VisitId = visitId,
                    PatientId = patientId, // Still not used in SQL but kept for consistency
                    ProcedureIds = procedureIds,
                    PerformedByUserId = performedByUserId
                }, transaction: trans);
                return affectedRows >= 0;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Icd10CodeDto>> GetIcd10CodesForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    icd.icd10_code_id AS Icd10CodeId,
                    icd.code AS Code,
                    icd.description AS Description,
                    icd.category AS Category,
                    icd.is_active AS IsActive
                FROM app.visit_diagnoses vd
                JOIN app.icd10_codes icd ON vd.icd10_code_id = icd.icd10_code_id
                JOIN app.visits v ON vd.visit_id = v.visit_id
                WHERE vd.visit_id = @VisitIdParam
                  AND v.facility_id = @FacilityId
                ORDER BY icd.code; -- Order by code for consistent display
            ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var result = await conn.QueryAsync<Icd10CodeDto>(sql, new
                {
                    VisitIdParam = visitId,
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans);
                return result ?? Enumerable.Empty<Icd10CodeDto>();
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProcedureDto>> GetProceduresForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    p.procedure_id AS ProcedureId,
                    p.code AS Code,
                    p.name AS Name,
                    p.description AS Description,
                    p.category AS Category,
                    p.is_active AS IsActive
                FROM app.visit_procedures vp
                JOIN app.procedures p ON vp.procedure_id = p.procedure_id
                JOIN app.visits v ON vp.visit_id = v.visit_id
                WHERE vp.visit_id = @VisitIdParam
                  AND v.facility_id = @FacilityId
                ORDER BY p.code; -- Order by code for consistent display
            ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var result = await conn.QueryAsync<ProcedureDto>(sql, new
                {
                    VisitIdParam = visitId,
                    FacilityId = _facilityContext.CurrentFacilityId
                }, transaction: trans);
                return result ?? Enumerable.Empty<ProcedureDto>();
            }, connection, transaction);
        }
    }
}