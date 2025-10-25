using Microsoft.EntityFrameworkCore;
using habits.Data;

public class GlobalSearchService : IGlobalSearchService
{
    private readonly ApplicationDbContext _context;

    public GlobalSearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GlobalSearchResultDto> Search(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return new GlobalSearchResultDto { SearchTerm = term };

        term = term.ToLower();
        var results = new List<SearchResultItem>();

        // Search Calendar Events
        var events = await _context.CalendarEvent
            .Include(e => e.EventType)
            .Where(e => e.Title.ToLower().Contains(term))
            .Where(events => events.StartDate >= DateTime.Today)
            .Take(5)
            .Select(e => new SearchResultItem
            {
                Type = "Event",
                Title = e.Title,
                NavigateUrl = $"/Calendar?date={e.StartDate:yyyy-MM-dd}&eventId={e.Id}",
                Priority = 1
            })
            .ToListAsync();

        UpdatePriority(events);
        results.AddRange(events);

        // Search Task Lists
        var lists = await _context.TaskList
            .Where(l => l.Name.ToLower().Contains(term))
            .Take(5)
            .Select(l => new SearchResultItem
            {
                Type = "List",
                Title = l.Name,
                NavigateUrl = $"/Item?taskListId={l.Id}",
                Priority = 2
            })
            .ToListAsync();

        UpdatePriority(lists);
        results.AddRange(lists);

        // Search Items in Lists
        var items = await _context.TaskListItem
            .Include(i => i.TaskList)
            .Where(i => i.Task.Contains(term.ToLower()))
            .Take(5)
            .Select(i => new SearchResultItem
            {
                Type = "To Do",
                Title = i.Task,
                NavigateUrl = $"/Item?taskListId={i.TaskList.Id}",
                Priority = 3
            })
            .ToListAsync();
        UpdatePriority(items);
        results.AddRange(items);

        // Search Documents
        var documents = await _context.Document
            .Where(d => d.Name.ToLower().Contains(term))
            .Take(5)
            .Select(d => new SearchResultItem
            {
                Type = "File",
                Title = d.Name,
                NavigateUrl = $"/Documents?search={Uri.EscapeDataString(d.Name)}",
                Priority = 4
            })
            .ToListAsync();
        UpdatePriority(documents);
        results.AddRange(documents);

        // Search Members
        var members = await _context.Users
            .Where(u => (u.Name + " " + u.Surname).ToLower().Contains(term) ||
                         u.Email!.ToLower().Contains(term))
            .Take(5)
            .Select(u => new SearchResultItem
            {
                Type = "Member",
                Title = $"{u.Name} {u.Surname}",
                NavigateUrl = $"/Members?search={Uri.EscapeDataString(u.Name + " " + u.Surname)}",
                Priority = 5
            })
            .ToListAsync();
        UpdatePriority(members);
        results.AddRange(members);

        // Order by priority and take top 5 overall results
        return new GlobalSearchResultDto
        {
            SearchTerm = term,
            Results = results
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Title)
                .Take(5)
                .ToList()
        };
    }

    private void UpdatePriority(List<SearchResultItem> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Priority = i + 1;
        }
    }
}