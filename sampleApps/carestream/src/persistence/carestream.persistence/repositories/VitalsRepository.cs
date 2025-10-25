using carestream.core.interfaces.repositories;
using carestream.core.dtos.vitals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Data;
using carestream.core.infrastructure; // Added for ICurrentFacilityContext
using System; // For Exception

namespace carestream.persistence.repositories
{
    public class VitalsRepository : BaseRepository, IVitalsRepository
    {
        public VitalsRepository(IConfiguration configuration, ILogger<VitalsRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        public Task<int> CreateVitalsRecordAsync(VitalsCaptureInputDto vitalsData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id from INSERT as it's not present in app.vital_signs schema.
            const string sql = @"
                INSERT INTO app.vital_signs (
                    visit_id, patient_id,
                    blood_pressure_systolic, blood_pressure_diastolic, heart_rate, temperature,
                    respiratory_rate, oxygen_saturation, pain_level,
                    urinalysis_color, urinalysis_clarity, urinalysis_specific_gravity, urinalysis_ph,
                    urinalysis_protein, urinalysis_glucose,
                    clinical_notes, requires_follow_up, mark_as_urgent,
                    recorded_at, recorded_by_user_id
                ) VALUES (
                    @VisitId, @PatientId,
                    @BloodPressureSystolic, @BloodPressureDiastolic, @HeartRate, @Temperature,
                    @RespiratoryRate, @OxygenSaturation, @PainLevel,
                    @UrinalysisColor, @UrinalysisClarity, @UrinalysisSpecificGravity, @UrinalysisPh,
                    @UrinalysisProtein, @UrinalysisGlucose,
                    @ClinicalNotes, @RequiresFollowUp, @MarkAsUrgent,
                    @RecordedAt, @RecordedByUserId
                )
                RETURNING vital_signs_id;";

            var parameters = new DynamicParameters(vitalsData);
            // parameters.Add("FacilityId", _facilityContext.CurrentFacilityId); // Removed this line

            return ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                try
                {
                    var newId = await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans);
                    return newId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating vitals record for VisitId: {VisitId}, PatientId: {PatientId}", vitalsData.VisitId, vitalsData.PatientId);
                    throw;
                }
            }, connection, transaction);
        }

        public Task<VitalsCaptureInputDto?> GetVitalsForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Joined with visits for facility_id filter.
            const string sql = @"
                SELECT
                    vs.visit_id AS VisitId,
                    vs.patient_id AS PatientId,
                    vs.blood_pressure_systolic AS BloodPressureSystolic,
                    vs.blood_pressure_diastolic AS BloodPressureDiastolic,
                    vs.heart_rate AS HeartRate,
                    vs.temperature AS Temperature,
                    vs.respiratory_rate AS RespiratoryRate,
                    vs.oxygen_saturation AS OxygenSaturation,
                    vs.pain_level AS PainLevel,
                    vs.urinalysis_color AS UrinalysisColor,
                    vs.urinalysis_clarity AS UrinalysisClarity,
                    vs.urinalysis_specific_gravity AS UrinalysisSpecificGravity,
                    vs.urinalysis_ph AS UrinalysisPh,
                    vs.urinalysis_protein AS UrinalysisProtein,
                    vs.urinalysis_glucose AS UrinalysisGlucose,
                    vs.clinical_notes AS ClinicalNotes,
                    vs.requires_follow_up AS RequiresFollowUp,
                    vs.mark_as_urgent AS MarkAsUrgent,
                    vs.recorded_at AS RecordedAt,
                    vs.recorded_by_user_id AS RecordedByUserId,
                    u.rank || '. ' || u.first_name || ' ' || u.last_name AS RecordedByUserName
                FROM app.vital_signs vs
                JOIN app.visits v ON vs.visit_id = v.visit_id -- ADDED JOIN to visits
                LEFT JOIN app.users u ON vs.recorded_by_user_id = u.user_id
                WHERE vs.visit_id = @VisitIdParam AND v.facility_id = @FacilityId -- FILTERED ON V.facility_id
                ORDER BY vs.recorded_at DESC
                LIMIT 1;";

            return ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                try
                {
                    var vitals = await conn.QueryFirstOrDefaultAsync<VitalsCaptureInputDto>(sql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                    return vitals;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching vitals record for VisitId: {VisitId}", visitId);
                    throw;
                }
            }, connection, transaction);
        }
    }
}