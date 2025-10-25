using System.Data;
using System.Threading.Tasks;
using carestream.core.dtos.consultation;

namespace carestream.core.interfaces.repositories
{
    public interface ISickNoteRepository
    {
        /// <summary>
        /// Gets the most recent sick note for a given visit.
        /// </summary>
        Task<SickNoteInputDto?> GetSickNoteByVisitIdAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new sick note.
        /// </summary>
        Task<SickNoteInputDto?> CreateSickNoteAsync(SickNoteInputDto data, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing sick note.
        /// </summary>
        Task<SickNoteInputDto?> UpdateSickNoteAsync(SickNoteInputDto data, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}