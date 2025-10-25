using System.Threading.Tasks;
using carestream.core.dtos.consultation;

namespace carestream.core.interfaces.services
{
    public interface ISickNoteService
    {
        /// <summary>
        /// Gets the sick note data for a specific visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <returns>The sick note data if it exists, otherwise null.</returns>
        Task<SickNoteInputDto?> GetSickNoteForVisitAsync(int visitId);

        /// <summary>
        /// Saves (creates or updates) a sick note for a visit.
        /// </summary>
        /// <param name="sickNoteData">The sick note data to save.</param>
        /// <param name="performingUserId">The ID of the user saving the sick note.</param>
        /// <returns>The saved sick note data (including any generated ID or timestamps), or null if save failed.</returns>
        Task<SickNoteInputDto?> SaveSickNoteAsync(SickNoteInputDto sickNoteData, int performingUserId);
    }
}