using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.medication;
using carestream.core.interfaces.repositories;
using carestream.core.infrastructure; // Added for ICurrentFacilityContext
using System.Linq; // For Enumerable.Empty() if needed
using System.Text; // For StringBuilder
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System; // For Exception, NOW()

namespace carestream.persistence.repositories
{
    public class MedicationRepository : BaseRepository, IMedicationRepository
    {
        public MedicationRepository(IConfiguration configuration, ILogger<MedicationRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        public async Task<IEnumerable<MedicationSearchResultDto>> SearchMedicationsAsync(string searchTerm, int limit = 10, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id filter as medications are system-wide.
            const string sql = @"
                SELECT
                    medication_id AS MedicationId,
                    name AS Name,
                    strength AS Strength,
                    form AS Form
                FROM app.medications
                WHERE (name ILIKE @SearchPattern OR category ILIKE @SearchPattern)
                   AND is_active = TRUE
                ORDER BY name, strength, form
                LIMIT @Limit;";

            var searchPattern = $"%{searchTerm}%";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<MedicationSearchResultDto>(sql, new { SearchPattern = searchPattern, Limit = limit }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<MedicationSearchResultDto>();
        }

        public async Task<MedicationSearchResultDto?> GetMedicationByIdAsync(int medicationId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id filter as medications are system-wide.
            const string sql = @"
                SELECT
                    medication_id AS MedicationId,
                    name AS Name,
                    strength AS Strength,
                    form AS Form
                FROM app.medications
                WHERE medication_id = @MedicationId
                  AND is_active = TRUE;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<MedicationSearchResultDto?>(sql, new { MedicationId = medicationId }, transaction: trans),
                connection, transaction);
        }

        public async Task<int?> GetStockOnHandAsync(int medicationId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT quantity_on_hand
                FROM app.medication_stock
                WHERE medication_id = @MedicationIdParam
                  AND facility_id = @FacilityId;"; // Correctly filters by facility_id

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<int?>(sql, new { MedicationIdParam = medicationId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        public async Task<bool> DecrementStockAsync(int medicationId, int quantityToDecrement, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (quantityToDecrement < 0)
            {
                return false;
            }
            if (quantityToDecrement == 0)
            {
                return true;
            }

            const string sql = @"
                UPDATE app.medication_stock
                SET quantity_on_hand = quantity_on_hand - @QuantityToDecrement
                WHERE medication_id = @MedicationId
                  AND quantity_on_hand >= @QuantityToDecrement
                  AND facility_id = @FacilityId;"; // Correctly filters by facility_id

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                try
                {
                    var affectedRows = await conn.ExecuteAsync(sql, new
                    {
                        MedicationId = medicationId,
                        QuantityToDecrement = quantityToDecrement,
                        FacilityId = _facilityContext.CurrentFacilityId
                    }, transaction: trans);

                    if (affectedRows == 1)
                    {
                        return true;
                    }
                    else
                    {
                        var currentStock = await conn.QuerySingleOrDefaultAsync<int?>(
                            "SELECT quantity_on_hand FROM app.medication_stock WHERE medication_id = @MedicationId AND facility_id = @FacilityId",
                            new { MedicationId = medicationId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);

                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrement medication stock for MedicationId: {MedicationId}, Quantity: {QuantityToDecrement}, FacilityId: {FacilityId}",
                        medicationId, quantityToDecrement, _facilityContext.CurrentFacilityId);
                    throw;
                }
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<MedicationStockDetailDto?> GetMedicationStockDetailAsync(int medicationId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Joins to app.medications to get medication details and app.medication_stock for stock.
            const string sql = @"
                SELECT
                    ms.medication_id AS MedicationId,
                    m.name AS Name,
                    m.strength AS Strength,
                    m.form AS Form,
                    m.category AS Category,
                    ms.quantity_on_hand AS QuantityOnHand,
                    ms.minimum_stock_level AS MinimumStockLevel,
                    ms.last_updated_at AS LastUpdatedAt
                FROM app.medication_stock ms
                JOIN app.medications m ON ms.medication_id = m.medication_id
                WHERE ms.medication_id = @MedicationIdParam
                  AND ms.facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<MedicationStockDetailDto>(sql, new { MedicationIdParam = medicationId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<MedicationStockDetailDto> Items, int TotalCount)> GetAllMedicationStockAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.medication_stock ms
                JOIN app.medications m ON ms.medication_id = m.medication_id
                WHERE ms.facility_id = @FacilityId
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1)) // Search by medication name or category
            {
                whereClauses.Add("(m.name ILIKE @SearchPattern OR m.category ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }
            if (options.IsActiveFilter.HasValue) // Filter by active status of medication itself (not stock status)
            {
                whereClauses.Add("m.is_active = @IsActiveFilter");
                parameters.Add("IsActiveFilter", options.IsActiveFilter.Value);
            }
            // For stock status (e.g., Low Stock), you might add a SearchTerm2 or specific flag
            // if (options.SearchTerm2 == "LowStock") { whereClauses.Add("ms.quantity_on_hand <= ms.minimum_stock_level"); }

            if (whereClauses.Any())
            {
                baseSql.Append(" AND ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(ms.medication_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    ms.medication_id AS MedicationId,
                    m.name AS Name,
                    m.strength AS Strength,
                    m.form AS Form,
                    m.category AS Category,
                    ms.quantity_on_hand AS QuantityOnHand,
                    ms.minimum_stock_level AS MinimumStockLevel,
                    ms.last_updated_at AS LastUpdatedAt
                {baseSql.ToString()}
                ORDER BY m.name
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<MedicationStockDetailDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<MedicationStockDetailDto>(), totalCount);
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> IncrementStockAsync(int medicationId, int quantityToIncrement, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (quantityToIncrement <= 0) return false;

            const string sql = @"
                INSERT INTO app.medication_stock (medication_id, facility_id, quantity_on_hand, last_updated_at)
                VALUES (@MedicationId, @FacilityId, @QuantityToIncrement, NOW())
                ON CONFLICT (medication_id, facility_id) DO UPDATE SET
                    quantity_on_hand = app.medication_stock.quantity_on_hand + EXCLUDED.quantity_on_hand,
                    last_updated_at = NOW();"; // No explicit updated_by_user_id on medication_stock table

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new
                {
                    MedicationId = medicationId,
                    FacilityId = _facilityContext.CurrentFacilityId,
                    QuantityToIncrement = quantityToIncrement // EXCLUDED.quantity_on_hand will use this value
                }, transaction: trans);
                return affectedRows >= 0; // ON CONFLICT DO UPDATE might return 0 if no change, or 1 if updated/inserted. >=0 is safer.
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> SetStockLevelAsync(int medicationId, int newQuantity, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (newQuantity < 0) return false; // Stock cannot be negative

            const string sql = @"
                INSERT INTO app.medication_stock (medication_id, facility_id, quantity_on_hand, last_updated_at)
                VALUES (@MedicationId, @FacilityId, @NewQuantity, NOW())
                ON CONFLICT (medication_id, facility_id) DO UPDATE SET
                    quantity_on_hand = EXCLUDED.quantity_on_hand,
                    last_updated_at = NOW();"; // No explicit updated_by_user_id on medication_stock table

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new
                {
                    MedicationId = medicationId,
                    FacilityId = _facilityContext.CurrentFacilityId,
                    NewQuantity = newQuantity // EXCLUDED.quantity_on_hand will use this value
                }, transaction: trans);
                return affectedRows >= 0; // ON CONFLICT DO UPDATE might return 0 if no change, or 1 if updated/inserted. >=0 is safer.
            }, connection, transaction);
        }
    }
}