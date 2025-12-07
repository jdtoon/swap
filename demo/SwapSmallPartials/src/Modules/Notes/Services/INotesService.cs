using SwapSmallPartials.Modules.Notes.Entities;

namespace SwapSmallPartials.Modules.Notes.Services;

public interface INotesService
{
    Task<List<Note>> GetAllAsync();
    Task<Note?> GetByIdAsync(int id);
    Task<Note> CreateAsync(Note note);
    Task UpdateAsync(int id, Note note);
    Task DeleteAsync(int id);
    Task TogglePinAsync(int id);
}
