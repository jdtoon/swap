using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.prescription;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.pharmacy;
using carestream.core.infrastructure;
using System.Linq;
using carestream.core.dtos.shared;
using System.Text;
using System;

namespace carestream.persistence.repositories
{
    public class PrescriptionRepository : BaseRepository, IPrescriptionRepository
    {
        public PrescriptionRepository(IConfiguration configuration, ILogger<PrescriptionRepository> logger, ICurrentFacilityContext facilityContext)
             : base(configuration, logger, facilityContext)
        {
        }

        public async Task<IEnumerable<PrescriptionItemDto>> GetPrescriptionItemsForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    pi.prescription_item_id AS PrescriptionItemId,
                    pi.medication_id AS MedicationId,
                    m.name || COALESCE(' ' || m.strength, '') || COALESCE(' ' || m.form, '') AS MedicationName,
                    pi.dosage AS Dosage,
                    pi.frequency AS Frequency,
                    pi.duration AS Duration,
                    pi.quantity_prescribed AS QuantityPrescribed,
                    pi.special_instructions AS SpecialInstructions,
                    pi.is_sent_to_pharmacy AS IsSentToPharmacy
                FROM app.prescription_items pi
                JOIN app.medications m ON pi.medication_id = m.medication_id
                JOIN app.visits v ON pi.visit_id = v.visit_id
                WHERE pi.visit_id = @VisitId
                  AND pi.is_sent_to_pharmacy = FALSE
                  AND v.facility_id = @FacilityId
                ORDER BY pi.created_at ASC;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<PrescriptionItemDto>(sql, new { VisitId = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<PrescriptionItemDto>();
        }

        public async Task<PrescriptionItemDto?> AddPrescriptionItemAsync(AddPrescriptionItemInputDto item, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.prescription_items (
                    visit_id, medication_id, dosage, frequency, duration,
                    quantity_prescribed, special_instructions, created_by_user_id
                ) VALUES (
                    @VisitId, @MedicationId, @Dosage, @Frequency, @Duration,
                    @QuantityPrescribed, @SpecialInstructions, @CreatedByUserId
                )
                RETURNING prescription_item_id AS PrescriptionItemId,
                          medication_id AS MedicationId,
                          dosage AS Dosage,
                          frequency AS Frequency,
                          duration AS Duration,
                          quantity_prescribed AS QuantityPrescribed,
                          special_instructions AS SpecialInstructions;";

            var parameters = new DynamicParameters(item);
            parameters.Add("CreatedByUserId", createdByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var newItemData = await conn.QuerySingleOrDefaultAsync<PrescriptionItemDto>(sql, parameters, transaction: trans);

                if (newItemData != null)
                {
                    const string medNameSql = "SELECT name || COALESCE(' ' || strength, '') || COALESCE(' ' || form, '') FROM app.medications WHERE medication_id = @MedicationId;";
                    newItemData.MedicationName = await conn.QuerySingleOrDefaultAsync<string>(medNameSql, new { newItemData.MedicationId }, transaction: trans) ?? "Unknown Medication";
                }
                return newItemData;
            }, connection, transaction);
        }

        public async Task<bool> RemovePrescriptionItemAsync(int prescriptionItemId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                DELETE FROM app.prescription_items pi
                USING app.visits v
                WHERE pi.visit_id = v.visit_id
                  AND pi.prescription_item_id = @PrescriptionItemId
                  AND pi.is_sent_to_pharmacy = FALSE
                  AND v.facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { PrescriptionItemId = prescriptionItemId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<bool> SendPrescriptionToPharmacyAsync(int visitId, int sentByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.prescription_items pi
                SET is_sent_to_pharmacy = TRUE,
                    pharmacy_sent_at = NOW()
                FROM app.visits v
                WHERE pi.visit_id = v.visit_id
                  AND pi.visit_id = @VisitId
                  AND pi.is_sent_to_pharmacy = FALSE
                  AND v.facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { VisitId = visitId, SentByUserId = sentByUserId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows >= 1;
            }, connection, transaction);
        }

        public async Task<IEnumerable<PendingPrescriptionSummaryDto>> GetPendingPrescriptionsSummaryAsync(int limit = 25, int offset = 0, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    v.visit_id AS VisitId,
                    p.patient_id AS PatientId,
                    p.first_name || ' ' || p.last_name AS PatientName,
                    p.rank AS PatientRank,
                    p.force_number AS PatientForceNumber,
                    MAX(pi.pharmacy_sent_at) AS PrescribedAt,
                    prescribing_doc.first_name || ' ' || prescribing_doc.last_name AS PrescribingDoctorName,
                    COUNT(pi.prescription_item_id) AS NumberOfMedications,
                    'Pending Dispense' AS Status
                FROM app.visits v
                JOIN app.patients p ON v.patient_id = p.patient_id
                JOIN app.prescription_items pi ON v.visit_id = pi.visit_id
                JOIN app.users prescribing_doc ON pi.created_by_user_id = prescribing_doc.user_id
                WHERE v.facility_id = @FacilityId
                  AND pi.is_sent_to_pharmacy = TRUE
                  AND pi.is_fully_dispensed = FALSE
                GROUP BY v.visit_id, p.patient_id, p.first_name, p.last_name, p.rank, p.force_number, prescribing_doc.first_name, prescribing_doc.last_name
                ORDER BY MAX(pi.pharmacy_sent_at) ASC
                LIMIT @Limit OFFSET @Offset;
            ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<PendingPrescriptionSummaryDto>(sql, new { Limit = limit, Offset = offset, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<PendingPrescriptionSummaryDto>();
        }

        public async Task<PharmacistDashboardStatsDto> GetPharmacistDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    COUNT(DISTINCT v.visit_id)::int AS PendingPrescriptionsCount
                FROM app.visits v
                WHERE v.facility_id = @FacilityId
                  AND EXISTS (
                    SELECT 1
                    FROM app.prescription_items pi
                    WHERE pi.visit_id = v.visit_id
                      AND pi.is_sent_to_pharmacy = TRUE
                      AND pi.is_fully_dispensed = FALSE
                );";

            var stats = await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<PharmacistDashboardStatsDto>(sql, new { FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);

            if (stats == null) stats = new PharmacistDashboardStatsDto();
            stats.PatientsWaitingCollection = 0;
            stats.DispensedTodayCount = 0;
            stats.AveragePreparationTime = "12m";

            return stats;
        }

        public async Task<IEnumerable<PrescriptionDetailItemDto>> GetPrescriptionDetailItemsAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    pi.prescription_item_id AS PrescriptionItemId,
                    pi.medication_id AS MedicationId,
                    m.name || COALESCE(' ' || m.strength, '') || COALESCE(' ' || m.form, '') AS MedicationName,
                    pi.dosage AS Dosage,
                    pi.frequency AS Frequency,
                    pi.duration AS Duration,
                    pi.quantity_prescribed AS QuantityPrescribed,
                    pi.special_instructions AS SpecialInstructions
                FROM app.prescription_items pi
                JOIN app.medications m ON pi.medication_id = m.medication_id
                JOIN app.visits v ON pi.visit_id = v.visit_id
                WHERE pi.visit_id = @VisitId
                  AND pi.is_sent_to_pharmacy = TRUE
                  AND v.facility_id = @FacilityId
                  AND pi.is_fully_dispensed = FALSE
                ORDER BY pi.prescription_item_id ASC;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<PrescriptionDetailItemDto>(sql, new { VisitId = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<PrescriptionDetailItemDto>();
        }

        public async Task<PrescriptionDetailHeaderDto?> GetPrescriptionDetailHeaderAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string refinedSql = @"
                SELECT
                    v.visit_id AS VisitId,
                    p.patient_id AS PatientId,
                    p.first_name || ' ' || p.last_name AS PatientName,
                    p.rank AS PatientRank,
                    p.force_number AS PatientForceNumber,
                    CASE WHEN p.date_of_birth IS NOT NULL THEN EXTRACT(YEAR FROM AGE(p.date_of_birth))::int ELSE NULL END AS PatientAge,
                    prescriber.first_name || ' ' || prescriber.last_name AS PrescriberName,
                    prescriber.rank AS PrescriberRank,
                    prescriber.department AS PrescriberDepartment,
                    MIN(pi.pharmacy_sent_at) AS PrescriptionDate,
                    'Rx-' || v.visit_id::text AS PrescriptionIdentifier
                FROM app.visits v
                JOIN app.patients p ON v.patient_id = p.patient_id
                JOIN app.prescription_items pi ON v.visit_id = pi.visit_id AND pi.is_sent_to_pharmacy = TRUE
                LEFT JOIN app.users prescriber ON pi.created_by_user_id = prescriber.user_id
                WHERE v.visit_id = @VisitIdParam AND v.facility_id = @FacilityId
                GROUP BY v.visit_id, p.patient_id, p.first_name, p.last_name, p.rank, p.force_number, p.date_of_birth,
                         prescriber.first_name, prescriber.last_name, prescriber.rank, prescriber.department
                LIMIT 1;
            ";


            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<PrescriptionDetailHeaderDto?>(refinedSql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        public async Task<IEnumerable<DispenseItemDto>> GetItemsForDispensingAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    pi.prescription_item_id AS PrescriptionItemId,
                    pi.medication_id AS MedicationId,
                    m.name || COALESCE(' ' || m.strength, '') || COALESCE(' ' || m.form, '') AS MedicationName,
                    pi.quantity_prescribed AS QuantityPrescribed,
                    pi.dosage AS OriginalDosage,
                    pi.frequency AS OriginalFrequency,
                    pi.duration AS OriginalDuration,
                    pi.special_instructions AS SpecialInstructions,
                    0 AS StockOnHand -- This will be filled later, or joined with medication_stock if needed.
                FROM app.prescription_items pi
                JOIN app.medications m ON pi.medication_id = m.medication_id
                JOIN app.visits v ON pi.visit_id = v.visit_id
                WHERE pi.visit_id = @VisitId
                  AND pi.is_sent_to_pharmacy = TRUE
                  AND v.facility_id = @FacilityId
                  AND pi.is_fully_dispensed = FALSE
                ORDER BY pi.prescription_item_id ASC;
            ";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<DispenseItemDto>(sql, new { VisitId = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<DispenseItemDto>();
        }

        public async Task<bool> UpdatePrescriptionItemDispenseStatusAsync(
            int prescriptionItemId,
            string newTotalQuantityDispensed,
            bool isFullyDispensed,
            int dispensedByUserId,
            IDbConnection? connection = null,
            IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.prescription_items pi
                SET
                    quantity_dispensed = @NewTotalQuantityDispensed,
                    is_fully_dispensed = @IsFullyDispensed,
                    last_dispensed_at = NOW(),
                    last_dispensed_by_user_id = @DispensedByUserId
                FROM app.visits v
                WHERE pi.visit_id = v.visit_id
                  AND pi.prescription_item_id = @PrescriptionItemId
                  AND v.facility_id = @FacilityId;";

            var parameters = new DynamicParameters();
            parameters.Add("PrescriptionItemId", prescriptionItemId);
            parameters.Add("NewTotalQuantityDispensed", newTotalQuantityDispensed);
            parameters.Add("IsFullyDispensed", isFullyDispensed);
            parameters.Add("DispensedByUserId", dispensedByUserId);
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<PrescriptionItemDispenseInfoDto?> GetPrescriptionItemCurrentDispenseInfoAsync(int prescriptionItemId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    pi.quantity_prescribed AS QuantityPrescribed,
                    pi.quantity_dispensed AS QuantityDispensedSoFar,
                    pi.is_fully_dispensed AS IsAlreadyFullyDispensed
                FROM app.prescription_items pi
                JOIN app.visits v ON pi.visit_id = v.visit_id
                WHERE pi.prescription_item_id = @PrescriptionItemIdParam
                  AND v.facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<PrescriptionItemDispenseInfoDto?>(sql, new { PrescriptionItemIdParam = prescriptionItemId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<PatientPrescriptionHistoryItemDto> Items, int TotalCount)> GetPatientPrescriptionHistoryAsync(int patientId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.prescription_items pi
                JOIN app.visits v ON pi.visit_id = v.visit_id
                JOIN app.patients p ON v.patient_id = p.patient_id
                JOIN app.medications m ON pi.medication_id = m.medication_id
                LEFT JOIN app.users prescriber ON pi.created_by_user_id = prescriber.user_id
                LEFT JOIN app.users dispenser ON pi.last_dispensed_by_user_id = dispenser.user_id
                WHERE pi.patient_id = @PatientIdParam -- Assuming patient_id exists on prescription_items for direct join or add to FROM clause
                  AND v.facility_id = @FacilityId -- Filter by current facility's context
            ");

            // Corrected: The prescription_items table does not have patient_id directly.
            // It links to visits.visits.patient_id. So, the baseSql needs patient_id from the join to visits.
            // Removed pi.patient_id = @PatientIdParam from WHERE.

            // Corrected baseSql for accurate joins
            baseSql = new StringBuilder(@"
                FROM app.prescription_items pi
                JOIN app.visits v ON pi.visit_id = v.visit_id
                JOIN app.patients p ON v.patient_id = p.patient_id -- Patient is now part of the base join
                JOIN app.medications m ON pi.medication_id = m.medication_id
                LEFT JOIN app.users prescriber ON pi.created_by_user_id = prescriber.user_id
                LEFT JOIN app.users dispenser ON pi.last_dispensed_by_user_id = dispenser.user_id
                WHERE p.patient_id = @PatientIdParam -- Filter by patient_id from the joined patients table
                  AND v.facility_id = @FacilityId -- Filter by current facility's context
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("PatientIdParam", patientId);
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1)) // Search by medication name
            {
                whereClauses.Add("(m.name ILIKE @MedicationSearch)");
                parameters.Add("MedicationSearch", $"%{options.SearchTerm1}%");
            }
            if (options.StartDate.HasValue) // Filter by prescription date
            {
                whereClauses.Add("pi.created_at >= @StartDate");
                parameters.Add("StartDate", options.StartDate.Value.Date);
            }
            if (options.EndDate.HasValue) // Filter by prescription date
            {
                whereClauses.Add("pi.created_at < @EndDate");
                parameters.Add("EndDate", options.EndDate.Value.Date.AddDays(1));
            }
            // For specific dispense status filtering (e.g., IsFullyDispensed) might use options.SearchTerm2 or custom enum

            if (whereClauses.Any())
            {
                baseSql.Append(" AND ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(pi.prescription_item_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    pi.prescription_item_id AS PrescriptionItemId,
                    pi.visit_id AS VisitId,
                    pi.patient_id AS PatientId, -- Add patient_id to SELECT for DTO mapping
                    pi.created_at AS PrescribedAt,
                    prescriber.first_name || ' ' || prescriber.last_name AS PrescribedByDoctorName,
                    pi.medication_id AS MedicationId,
                    m.name || COALESCE(' ' || m.strength, '') || COALESCE(' ' || m.form, '') AS MedicationName,
                    pi.dosage AS Dosage,
                    pi.frequency AS Frequency,
                    pi.duration AS Duration,
                    pi.quantity_prescribed AS QuantityPrescribed,
                    pi.special_instructions AS SpecialInstructions,
                    pi.quantity_dispensed AS QuantityDispensed,
                    pi.is_fully_dispensed AS IsFullyDispensed,
                    pi.last_dispensed_at AS LastDispensedAt,
                    dispenser.first_name || ' ' || dispenser.last_name AS LastDispensedByPharmacistName
                {baseSql.ToString()}
                ORDER BY pi.created_at DESC
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<PatientPrescriptionHistoryItemDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<PatientPrescriptionHistoryItemDto>(), totalCount);
            }, connection, transaction);
        }
    }
}