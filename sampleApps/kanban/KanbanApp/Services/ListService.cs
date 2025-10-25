using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;
using KanbanApp.Dtos;

namespace KanbanApp.Services;

public interface IListService
{
    Task<PagedResult<KanbanList>> GetByBoardIdAsync(int boardId, int skip = 0, int take = 20);
    Task<KanbanList?> GetByIdAsync(int id);
    Task<KanbanList> CreateAsync(KanbanList list);
    Task<KanbanList?> UpdateAsync(int id, KanbanList list);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdatePositionAsync(int id, int newPosition);
}

public class ListService : IListService
{
    private readonly KanbanDbContext _context;
    
    public ListService(KanbanDbContext context)
    {
        _context = context;
    }
    
    public async Task<PagedResult<KanbanList>> GetByBoardIdAsync(int boardId, int skip = 0, int take = 20)
    {
        var query = _context.Lists
            .Where(l => l.BoardId == boardId && !l.IsArchived)
            .OrderBy(l => l.Position)
            .AsQueryable();
            
        var totalRecords = await query.CountAsync();
        var data = await query
            .Skip(skip)
            .Take(take)
            .Include(l => l.Cards.OrderBy(c => c.Position))
            .ToListAsync();
            
        return new PagedResult<KanbanList>
        {
            Data = data,
            HasMore = (skip + take) < totalRecords,
            TotalRecords = totalRecords,
            CurrentPage = (skip / take) + 1
        };
    }
    
    public async Task<KanbanList?> GetByIdAsync(int id)
    {
        return await _context.Lists
            .Include(l => l.Board)
            .Include(l => l.Cards.OrderBy(c => c.Position))
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsArchived);
    }
    
    public async Task<KanbanList> CreateAsync(KanbanList list)
    {
        var maxPosition = await _context.Lists
            .Where(l => l.BoardId == list.BoardId)
            .MaxAsync(l => (int?)l.Position) ?? -1;
            
        list.Position = maxPosition + 1;
        
        // TTW pattern: MapBoardId → BoardId
        if (list.MapBoardId > 0)
        {
            list.BoardId = list.MapBoardId;
        }
        
        _context.Lists.Add(list);
        await _context.SaveChangesAsync();
        
        return list;
    }
    
    public async Task<KanbanList?> UpdateAsync(int id, KanbanList list)
    {
        var existing = await _context.Lists.FindAsync(id);
        if (existing == null) return null;
        
        existing.Name = list.Name;
        
        await _context.SaveChangesAsync();
        return existing;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        var list = await _context.Lists.FindAsync(id);
        if (list == null) return false;
        
        _context.Lists.Remove(list);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> UpdatePositionAsync(int id, int newPosition)
    {
        var list = await _context.Lists.FindAsync(id);
        if (list == null) return false;
        
        var oldPosition = list.Position;
        var boardId = list.BoardId;
        
        // Reorder other lists
        if (newPosition < oldPosition)
        {
            // Moving left - shift lists right
            await _context.Lists
                .Where(l => l.BoardId == boardId && l.Position >= newPosition && l.Position < oldPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Position, l => l.Position + 1));
        }
        else if (newPosition > oldPosition)
        {
            // Moving right - shift lists left
            await _context.Lists
                .Where(l => l.BoardId == boardId && l.Position > oldPosition && l.Position <= newPosition)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Position, l => l.Position - 1));
        }
        
        list.Position = newPosition;
        await _context.SaveChangesAsync();
        
        return true;
    }
}
