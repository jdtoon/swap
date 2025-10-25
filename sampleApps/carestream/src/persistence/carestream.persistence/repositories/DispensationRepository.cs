using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.pharmacy;
using System.Text;
using carestream.core.dtos.shared;
using carestream.core.infrastructure; // Added for ICurrentFacilityContext
using System.Linq; // For .Any()
using System; // For Exception, NOW()

namespace carestream.persistence.repositories
{
    public class DispensationRepository : BaseRepository, IDispensationRepository
    {
        public DispensationRepository(IConfiguration configuration, ILogger<DispensationRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        public async Task<int> LogDispenseActionAsync(DispenseLogEntryInputDto logEntry, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id from INSERT as it's not present in app.dispensation_log_items schema.
            const string sql = @"
                INSERT INTO app.dispensation_log_items (
                    prescription_item_id, visit_id, medication_id,
                    quantity_dispensed_transaction, dispensed_by_user_id, pharmacist_notes,
                    batch_number, expiry_date, dispensed_at
                ) VALUES (
                    @PrescriptionItemId, @VisitId, @MedicationId,
                    @QuantityDispensedInTransaction, @DispensedByUserId, @PharmacistNotes,
                    @BatchNumber, @ExpiryDate, NOW()
                )
                RETURNING dispensation_log_item_id;";

            var parameters = new DynamicParameters(logEntry);
            // parameters.Add("FacilityId", _facilityContext.CurrentFacilityId); // Removed this line

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        public async Task<(IEnumerable<DispensedHistoryItemDto> Items, int TotalCount)> GetDispensedHistoryAsync(
            FilterAndPaginationOptions options,
            IDbConnection? connection = null,
            IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.dispensation_log_items dli
                JOIN app.prescription_items pi ON dli.prescription_item_id = pi.prescription_item_id
                JOIN app.visits v ON dli.visit_id = v.visit_id
                JOIN app.patients p ON v.patient_id = p.patient_id
                JOIN app.medications m ON dli.medication_id = m.medication_id
                JOIN app.users pharmacist ON dli.dispensed_by_user_id = pharmacist.user_id
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            // Corrected: Filter by facility_id on the visits table, as dispensation_log_items does not have it directly.
            whereClauses.Add("v.facility_id = @FacilityId");
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);

            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            if (options.StartDate.HasValue)
            {
                whereClauses.Add("dli.dispensed_at >= @StartDate");
                parameters.Add("StartDate", options.StartDate.Value.Date);
            }
            if (options.EndDate.HasValue)
            {
                whereClauses.Add("dli.dispensed_at < @EndDate");
                parameters.Add("EndDate", options.EndDate.Value.Date.AddDays(1));
            }
            if (!string.IsNullOrWhiteSpace(options.SearchTerm1))
            {
                whereClauses.Add("(p.first_name ILIKE @PatientSearch OR p.last_name ILIKE @PatientSearch OR p.force_number ILIKE @PatientSearch)");
                parameters.Add("PatientSearch", $"%{options.SearchTerm1}%");
            }
            if (!string.IsNullOrWhiteSpace(options.SearchTerm2))
            {
                whereClauses.Add("(m.name ILIKE @MedicationSearch OR m.category ILIKE @MedicationSearch)");
                parameters.Add("MedicationSearch", $"%{options.SearchTerm2}%");
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(DISTINCT dli.dispensation_log_item_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    dli.dispensation_log_item_id AS DispensationLogItemId,
                    dli.visit_id AS VisitId,
                    dli.dispensed_at AS DispensedAt,
                    p.first_name || ' ' || p.last_name AS PatientName,
                    p.force_number AS PatientForceNumber,
                    m.name || COALESCE(' ' || m.strength, '') || COALESCE(' ' || m.form, '') AS MedicationName,
                    dli.quantity_dispensed_transaction AS QuantityDispensedInTransaction,
                    pharmacist.first_name || ' ' || pharmacist.last_name AS PharmacistName,
                    dli.pharmacist_notes AS PharmacistNotes,
                    dli.batch_number AS BatchNumber,
                    dli.expiry_date AS ExpiryDate,
                    dli.prescription_item_id as PrescriptionItemId
                {baseSql.ToString()}
            ");

            itemsSql.Append(" ORDER BY dli.dispensed_at DESC");
            itemsSql.Append(" LIMIT @PageSize OFFSET @Offset;");


            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<DispensedHistoryItemDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<DispensedHistoryItemDto>(), totalCount);
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<DispensedHistoryItemDto?> GetDispensedHistoryDetailAsync(int dispensationLogItemId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    dli.dispensation_log_item_id AS DispensationLogItemId,
                    dli.visit_id AS VisitId,
                    dli.dispensed_at AS DispensedAt,
                    p.first_name || ' ' || p.last_name AS PatientName,
                    p.force_number AS PatientForceNumber,
                    m.name || COALESCE(' ' || m.strength, '') || COALESCE(' ' || m.form, '') AS MedicationName,
                    dli.quantity_dispensed_transaction AS QuantityDispensedInTransaction,
                    pharmacist.first_name || ' ' || pharmacist.last_name AS PharmacistName,
                    dli.pharmacist_notes AS PharmacistNotes,
                    dli.batch_number AS BatchNumber,
                    dli.expiry_date AS ExpiryDate,
                    dli.prescription_item_id as PrescriptionItemId,
                    pi.quantity_prescribed AS QuantityPrescribedOriginal,
                    pi.dosage AS OriginalDosage,
                    pi.frequency AS OriginalFrequency,
                    pi.duration AS OriginalDuration,
                    pi.special_instructions AS OriginalSpecialInstructions
                FROM app.dispensation_log_items dli
                JOIN app.prescription_items pi ON dli.prescription_item_id = pi.prescription_item_id
                JOIN app.visits v ON dli.visit_id = v.visit_id
                JOIN app.patients p ON v.patient_id = p.patient_id
                JOIN app.medications m ON dli.medication_id = m.medication_id
                JOIN app.users pharmacist ON dli.dispensed_by_user_id = pharmacist.user_id
                WHERE dli.dispensation_log_item_id = @DispensationLogItemIdParam
                  AND v.facility_id = @FacilityId;"; // Filter by facility_id on the visits table

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<DispensedHistoryItemDto>(sql, new { DispensationLogItemIdParam = dispensationLogItemId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }
    }
}