using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;
using KanbanApp.Dtos;

namespace KanbanApp.Services;

public interface IBoardService
{
    Task<PagedResult<Board>> GetAllAsync(int skip = 0, int take = 10, string? search = null);
    Task<Board?> GetByIdAsync(int id);
    Task<Board> CreateAsync(Board board);
    Task<Board?> UpdateAsync(int id, Board board);
    Task<bool> DeleteAsync(int id);
}

public class BoardService : IBoardService
{
    private readonly KanbanDbContext _context;
    
    public BoardService(KanbanDbContext context)
    {
        _context = context;
    }
    
    public async Task<PagedResult<Board>> GetAllAsync(int skip = 0, int take = 10, string? search = null)
    {
        var query = _context.Boards
            .Where(b => !b.IsArchived)
            .OrderBy(b => b.Position)
            .AsQueryable();
            
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(b => 
                b.Name.ToLower().Contains(searchLower) || 
                (b.Description != null && b.Description.ToLower().Contains(searchLower)));
        }
        
        var totalRecords = await query.CountAsync();
        var data = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            
        return new PagedResult<Board>
        {
            Data = data,
            HasMore = (skip + take) < totalRecords,
            TotalRecords = totalRecords,
            CurrentPage = (skip / take) + 1
        };
    }
    
    public async Task<Board?> GetByIdAsync(int id)
    {
        return await _context.Boards
            .Include(b => b.Lists.Where(l => !l.IsArchived).OrderBy(l => l.Position))
                .ThenInclude(l => l.Cards.OrderBy(c => c.Position))
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsArchived);
    }
    
    public async Task<Board> CreateAsync(Board board)
    {
        var maxPosition = await _context.Boards.MaxAsync(b => (int?)b.Position) ?? -1;
        board.Position = maxPosition + 1;
        board.CreatedAt = DateTime.UtcNow;
        
        _context.Boards.Add(board);
        await _context.SaveChangesAsync();
        
        return board;
    }
    
    public async Task<Board?> UpdateAsync(int id, Board board)
    {
        var existing = await _context.Boards.FindAsync(id);
        if (existing == null) return null;
        
        existing.Name = board.Name;
        existing.Description = board.Description;
        
        await _context.SaveChangesAsync();
        return existing;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        var board = await _context.Boards.FindAsync(id);
        if (board == null) return false;
        
        _context.Boards.Remove(board);
        await _context.SaveChangesAsync();
        
        return true;
    }
}
