using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.consultation;
using carestream.core.interfaces.repositories;
using carestream.core.infrastructure; // Added for ICurrentFacilityContext

namespace carestream.persistence.repositories
{
    public class SickNoteRepository : BaseRepository, ISickNoteRepository
    {
        public SickNoteRepository(IConfiguration configuration, ILogger<SickNoteRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        public async Task<SickNoteInputDto?> GetSickNoteByVisitIdAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Joined with visits for facility_id filter.
            const string sql = @"
                SELECT
                    sn.sick_note_id AS SickNoteId,
                    sn.visit_id AS VisitId,
                    sn.start_date AS StartDate,
                    sn.end_date AS EndDate,
                    sn.diagnosis AS Diagnosis,
                    sn.recommendations AS Recommendations,
                    sn.issued_at AS IssuedAt,
                    u.first_name || ' ' || u.last_name AS IssuedByUserName
                FROM app.sick_notes sn
                JOIN app.visits v ON sn.visit_id = v.visit_id -- ADDED JOIN
                LEFT JOIN app.users u ON sn.issued_by_user_id = u.user_id
                WHERE sn.visit_id = @VisitIdParam AND v.facility_id = @FacilityId -- FILTERED ON V.facility_id
                ORDER BY sn.issued_at DESC
                LIMIT 1;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<SickNoteInputDto?>(sql, new { VisitIdParam = visitId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        public async Task<SickNoteInputDto?> CreateSickNoteAsync(SickNoteInputDto data, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id from INSERT as it's not present in app.sick_notes schema.
            const string sql = @"
                INSERT INTO app.sick_notes (
                    visit_id, start_date, end_date, diagnosis, recommendations,
                    issued_by_user_id, issued_at
                ) VALUES (
                    @VisitId, @StartDate, @EndDate, @Diagnosis, @Recommendations,
                    @IssuedByUserId, NOW()
                )
                RETURNING sick_note_id AS SickNoteId, visit_id AS VisitId, start_date AS StartDate,
                          end_date AS EndDate, diagnosis AS Diagnosis, recommendations AS Recommendations,
                          issued_at AS IssuedAt;";

            var parameters = new DynamicParameters(data);
            parameters.Add("IssuedByUserId", createdByUserId);
            // parameters.Add("FacilityId", _facilityContext.CurrentFacilityId); // Removed this line

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var createdNote = await conn.QuerySingleOrDefaultAsync<SickNoteInputDto>(sql, parameters, transaction: trans);

                if (createdNote != null && createdByUserId > 0)
                {
                    const string userSql = "SELECT first_name || ' ' || last_name FROM app.users WHERE user_id = @UserId;";
                    createdNote.IssuedByUserName = await conn.QuerySingleOrDefaultAsync<string>(userSql, new { UserId = createdByUserId }, transaction: trans);
                }
                return createdNote;
            }, connection, transaction);
        }

        public async Task<SickNoteInputDto?> UpdateSickNoteAsync(SickNoteInputDto data, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Joined with visits for facility_id filter.
            const string sql = @"
                UPDATE app.sick_notes sn
                SET
                    start_date = @StartDate,
                    end_date = @EndDate,
                    diagnosis = @Diagnosis,
                    recommendations = @Recommendations,
                    issued_at = NOW(),
                    issued_by_user_id = @IssuedByUserId
                FROM app.visits v -- ADDED JOIN SOURCE
                WHERE sn.visit_id = v.visit_id -- JOIN CONDITION
                  AND sn.sick_note_id = @SickNoteId
                  AND sn.visit_id = @VisitId
                  AND v.facility_id = @FacilityId -- FILTERED ON V.facility_id
                RETURNING sn.sick_note_id AS SickNoteId, sn.visit_id AS VisitId, sn.start_date AS StartDate,
                          sn.end_date AS EndDate, sn.diagnosis AS Diagnosis, sn.recommendations AS Recommendations,
                          sn.issued_at AS IssuedAt;";

            if (!data.SickNoteId.HasValue || data.SickNoteId.Value <= 0)
            {
                return null;
            }

            var parameters = new DynamicParameters(data);
            parameters.Add("IssuedByUserId", updatedByUserId);
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId);


            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var updatedNote = await conn.QuerySingleOrDefaultAsync<SickNoteInputDto>(sql, parameters, transaction: trans);

                if (updatedNote != null && updatedByUserId > 0)
                {
                    const string userSql = "SELECT first_name || ' ' || last_name FROM app.users WHERE user_id = @UserId;";
                    updatedNote.IssuedByUserName = await conn.QuerySingleOrDefaultAsync<string>(userSql, new { UserId = updatedByUserId }, transaction: trans);
                }
                return updatedNote;
            }, connection, transaction);
        }
    }
}