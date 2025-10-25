using carestream.core.dtos.consultation; // For VisitAssessmentDto, CreateUpdateVisitAssessmentDto
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Linq; // For Enumerable.Empty()
using System.Threading.Tasks;

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing visit assessment data persistence.
    /// </summary>
    public class VisitAssessmentRepository : BaseRepository, IVisitAssessmentRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisitAssessmentRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public VisitAssessmentRepository(IConfiguration configuration, ILogger<VisitAssessmentRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<VisitAssessmentDto?> GetVisitAssessmentByIdAsync(int assessmentId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    va.visit_assessment_id AS VisitAssessmentId,
                    va.visit_id AS VisitId,
                    va.patient_id AS PatientId,
                    va.assessment_date AS AssessmentDate,
                    va.assessed_by_user_id AS AssessedByUserId,
                    u.first_name || ' ' || u.last_name AS AssessedByUserName,
                    va.physical_exam_findings AS PhysicalExamFindings,
                    va.cardiovascular_notes AS CardiovascularNotes,
                    va.respiratory_notes AS RespiratoryNotes,
                    va.musculoskeletal_notes AS MusculoskeletalNotes,
                    va.neurological_notes AS NeurologicalNotes,
                    va.psychological_notes AS PsychologicalNotes,
                    va.other_systems_notes AS OtherSystemsNotes,
                    va.medical_classification AS MedicalClassification,
                    va.deployment_status AS DeploymentStatus,
                    va.validity_period_months AS ValidityPeriodMonths,
                    va.restrictions AS Restrictions
                FROM app.visit_assessments va
                JOIN app.visits v ON va.visit_id = v.visit_id -- For facility filtering
                LEFT JOIN app.users u ON va.assessed_by_user_id = u.user_id
                WHERE va.visit_assessment_id = @AssessmentIdParam
                  AND v.facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<VisitAssessmentDto>(sql, new { AssessmentIdParam = assessmentId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<VisitAssessmentDto?> GetLatestAssessmentForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    va.visit_assessment_id AS VisitAssessmentId,
                    va.visit_id AS VisitId,
                    va.patient_id AS PatientId,
                    va.assessment_date AS AssessmentDate,
                    va.assessed_by_user_id AS AssessedByUserId,
                    u.first_name || ' ' || u.last_name AS AssessedByUserName,
                    va.physical_exam_findings AS PhysicalExamFindings,
                    va.cardiovascular_notes AS CardiovascularNotes,
                    va.respiratory_notes AS RespiratoryNotes,
                    va.musculoskeletal_notes AS MusculoskeletalNotes,
                    va.neurological_notes AS NeurologicalNotes,
                    va.psychological_notes AS PsychologicalNotes,
                    va.other_systems_notes AS OtherSystemsNotes,
                    va.medical_classification AS MedicalClassification,
                    va.deployment_status AS DeploymentStatus,
                    va.validity_period_months AS ValidityPeriodMonths,
                    va.restrictions AS Restrictions
                FROM app.visit_assessments va
                JOIN app.visits v ON va.visit_id = v.visit_id -- For facility filtering
                LEFT JOIN app.users u ON va.assessed_by_user_id = u.user_id
                WHERE va.visit_id = @VisitIdParam
                  AND v.facility_id = @FacilityId
                ORDER BY va.assessment_date DESC
                LIMIT 1;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<VisitAssessmentDto>(sql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreateVisitAssessmentAsync(CreateUpdateVisitAssessmentDto assessmentData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.visit_assessments (
                    visit_id, patient_id, assessment_date, assessed_by_user_id,
                    physical_exam_findings, cardiovascular_notes, respiratory_notes, musculoskeletal_notes,
                    neurological_notes, psychological_notes, other_systems_notes,
                    medical_classification, deployment_status, validity_period_months, restrictions
                ) VALUES (
                    @VisitId, @PatientId, NOW(), @AssessedByUserId,
                    @PhysicalExamFindings, @CardiovascularNotes, @RespiratoryNotes, @MusculoskeletalNotes,
                    @NeurologicalNotes, @PsychologicalNotes, @OtherSystemsNotes,
                    @MedicalClassification, @DeploymentStatus, @ValidityPeriodMonths, @Restrictions
                )
                RETURNING visit_assessment_id;";

            var parameters = new DynamicParameters(assessmentData);
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateVisitAssessmentAsync(CreateUpdateVisitAssessmentDto assessmentData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (assessmentData.VisitAssessmentId <= 0) return false;

            const string sql = @"
                UPDATE app.visit_assessments va
                SET
                    assessment_date = NOW(),
                    assessed_by_user_id = @AssessedByUserId,
                    physical_exam_findings = @PhysicalExamFindings,
                    cardiovascular_notes = @CardiovascularNotes,
                    respiratory_notes = @RespiratoryNotes,
                    musculoskeletal_notes = @MusculoskeletalNotes,
                    neurological_notes = @NeurologicalNotes,
                    psychological_notes = @PsychologicalNotes,
                    other_systems_notes = @OtherSystemsNotes,
                    medical_classification = @MedicalClassification,
                    deployment_status = @DeploymentStatus,
                    validity_period_months = @ValidityPeriodMonths,
                    restrictions = @Restrictions
                FROM app.visits v -- For facility filtering
                WHERE va.visit_id = v.visit_id
                  AND va.visit_assessment_id = @VisitAssessmentId
                  AND v.facility_id = @FacilityId;";

            var parameters = new DynamicParameters(assessmentData);
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }
    }
}