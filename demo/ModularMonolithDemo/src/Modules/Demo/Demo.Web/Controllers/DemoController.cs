using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using ModularMonolithDemo.Modules.Demo.Module;
using ModularMonolithDemo.Modules.Demo.Contracts;
using Swap.Htmx.Events;

namespace ModularMonolithDemo.Modules.Demo.Web.Controllers;

[Route("Demo")] 
public class DemoController : SwapController
{
    private readonly IStatsService _stats;
    private readonly IDemoQueries _queries;
    private readonly INotesService _notes;
    private readonly IBulkService _bulk;
    private readonly ISwapEventBus _bus;

    public DemoController(IStatsService stats, IDemoQueries queries, INotesService notes, IBulkService bulk, ISwapEventBus bus)
    {
        _stats = stats; _queries = queries; _notes = notes; _bulk = bulk; _bus = bus;
    }

    [HttpGet("")]
    public IActionResult Index() => SwapView("~/Views/Demo/Index.cshtml");

    [HttpGet("Stats")]
    public async Task<IActionResult> Stats()
        => PartialView("~/Views/Demo/_Stats.cshtml", await _stats.GetStatsAsync());

    [HttpGet("Toasts")]
    public async Task<IActionResult> Toasts()
        => PartialView("~/Views/Demo/_Toasts.cshtml", await _queries.GetLatestMessageAsync());

    [HttpGet("ActivityLog")]
    public async Task<IActionResult> ActivityLog()
        => PartialView("~/Views/Demo/_ActivityLog.cshtml", await _queries.GetActivityLogAsync());

    // --- Dynamic components demo ---
    [HttpGet("Dynamic")]
    public IActionResult Dynamic() => SwapView("~/Views/Demo/Dynamic.cshtml");

    [HttpGet("Summary")]
    public async Task<IActionResult> Summary()
        => PartialView("~/Views/Demo/_Summary.cshtml", await _notes.GetCountAsync());

    [HttpGet("Details")]
    public async Task<IActionResult> Details()
        => PartialView("~/Views/Demo/_Details.cshtml", await _notes.GetAllAsync());

    [HttpPost("AddNote")]
    public async Task<IActionResult> AddNote(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return BadRequest();
        await _notes.AddAsync(text);
        _bus.Emit(EventNames.Ui.DetailsRefresh);
        _bus.Emit(EventNames.Ui.SummaryRefresh);
        Response.ShowSuccessToast($"Note added: {text}");
        return NoContent();
    }

    // --- Bulk operations demo ---
    [HttpGet("Bulk")]
    public IActionResult Bulk() => SwapView("~/Views/Demo/Bulk.cshtml");

    [HttpGet("BulkTodos")]
    public async Task<IActionResult> BulkTodos([FromServices] ModularMonolithDemo.Modules.Todos.Contracts.ITodoService todos)
        => PartialView("~/Views/Demo/_BulkTodos.cshtml", await Task.FromResult(todos.GetAll().ToList()));

    [HttpPost("BulkComplete")]
    public async Task<IActionResult> BulkComplete([FromForm] int[] ids)
    {
        var count = await _bulk.CompleteAsync(ids);
        if (count == 0) { Response.ShowSuccessToast("No items selected"); return NoContent(); }
        _bus.Emit(ModularMonolithDemo.Modules.Todos.Contracts.TodoEvents.Ui.RefreshList);
        _bus.Emit(EventNames.Ui.StatsRefresh);
        Response.ShowSuccessToast($"Completed {count} todos");
        return NoContent();
    }
}
