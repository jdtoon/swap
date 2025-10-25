using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;
using KanbanApp.Dtos;

namespace KanbanApp.Services;

public interface ICardService
{
    Task<PagedResult<Card>> GetByListIdAsync(int listId, int skip = 0, int take = 50);
    Task<Card?> GetByIdAsync(int id);
    Task<Card> CreateAsync(Card card);
    Task<Card?> UpdateAsync(int id, Card card);
    Task<bool> DeleteAsync(int id);
    Task<bool> MoveCardAsync(int id, int newListId, int newPosition);
}

public class CardService : ICardService
{
    private readonly KanbanDbContext _context;
    
    public CardService(KanbanDbContext context)
    {
        _context = context;
    }
    
    public async Task<PagedResult<Card>> GetByListIdAsync(int listId, int skip = 0, int take = 50)
    {
        var query = _context.Cards
            .Where(c => c.ListId == listId)
            .OrderBy(c => c.Position)
            .AsQueryable();
            
        var totalRecords = await query.CountAsync();
        var data = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            
        return new PagedResult<Card>
        {
            Data = data,
            HasMore = (skip + take) < totalRecords,
            TotalRecords = totalRecords,
            CurrentPage = (skip / take) + 1
        };
    }
    
    public async Task<Card?> GetByIdAsync(int id)
    {
        return await _context.Cards
            .Include(c => c.List)
                .ThenInclude(l => l.Board)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<Card> CreateAsync(Card card)
    {
        var maxPosition = await _context.Cards
            .Where(c => c.ListId == card.ListId)
            .MaxAsync(c => (int?)c.Position) ?? -1;
            
        card.Position = maxPosition + 1;
        card.CreatedAt = DateTime.UtcNow;
        
        // TTW pattern: MapListId → ListId
        if (card.MapListId > 0)
        {
            card.ListId = card.MapListId;
        }
        
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        
        return card;
    }
    
    public async Task<Card?> UpdateAsync(int id, Card card)
    {
        var existing = await _context.Cards.FindAsync(id);
        if (existing == null) return null;
        
        existing.Title = card.Title;
        existing.Description = card.Description;
        existing.Priority = card.Priority;
        existing.DueDate = card.DueDate;
        
        await _context.SaveChangesAsync();
        return existing;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        var card = await _context.Cards.FindAsync(id);
        if (card == null) return false;
        
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> MoveCardAsync(int id, int newListId, int newPosition)
    {
        var card = await _context.Cards.FindAsync(id);
        if (card == null) return false;
        
        var oldListId = card.ListId;
        var oldPosition = card.Position;
        
        // Same list - just reorder
        if (oldListId == newListId)
        {
            if (newPosition < oldPosition)
            {
                // Moving up - shift cards down
                await _context.Cards
                    .Where(c => c.ListId == oldListId && c.Position >= newPosition && c.Position < oldPosition)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position + 1));
            }
            else if (newPosition > oldPosition)
            {
                // Moving down - shift cards up
                await _context.Cards
                    .Where(c => c.ListId == oldListId && c.Position > oldPosition && c.Position <= newPosition)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position - 1));
            }
        }
        else
        {
            // Different list - update both lists
            
            // Shift cards in old list
            await _context.Cards
                .Where(c => c.ListId == oldListId && c.Position > oldPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position - 1));
            
            // Shift cards in new list
            await _context.Cards
                .Where(c => c.ListId == newListId && c.Position >= newPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Position, c => c.Position + 1));
            
            card.ListId = newListId;
        }
        
        card.Position = newPosition;
        await _context.SaveChangesAsync();
        
        return true;
    }
}
