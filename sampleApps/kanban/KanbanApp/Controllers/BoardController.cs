using Microsoft.AspNetCore.Mvc;
using KanbanApp.Data;
using KanbanApp.Services;

namespace KanbanApp.Controllers;

public class BoardController : Controller
{
    private readonly IBoardService _boardService;
    private readonly IListService _listService;
    
    public BoardController(IBoardService boardService, IListService listService)
    {
        _boardService = boardService;
        _listService = listService;
    }
    
    // TTW pattern: Shell view with hx-trigger="load"
    public IActionResult Index()
    {
        return View();
    }
    
    // TTW pattern: Check HX-Request header, return PartialView
    public async Task<IActionResult> GetBoardList(int skip = 0, int take = 10, string? search = null)
    {
        var result = await _boardService.GetAllAsync(skip, take, search);
        ViewBag.Search = search;
        ViewBag.Skip = skip;
        ViewBag.Take = take;
        
        if (Request.Headers["HX-Request"] == "true")
        {
            return PartialView("_BoardList", result);
        }
        
        return View("Index");
    }
    
    // View board details with lists and cards
    public async Task<IActionResult> Details(int id)
    {
        var board = await _boardService.GetByIdAsync(id);
        if (board == null) return NotFound();
        
        return View(board);
    }
    
    // TTW pattern: Load modal into #modalContainer
    public IActionResult CreateModal()
    {
        return PartialView("_CreateModal");
    }
    
    // TTW pattern: Load edit modal with data
    public async Task<IActionResult> EditModal(int id)
    {
        var board = await _boardService.GetByIdAsync(id);
        if (board == null) return NotFound();
        
        return PartialView("_EditModal", board);
    }
    
    // Create board
    [HttpPost]
    public async Task<IActionResult> Create(Board board)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_CreateModal", board);
        }
        
        await _boardService.CreateAsync(board);
        
        // Refresh the board list
        var result = await _boardService.GetAllAsync();
        return PartialView("_BoardList", result);
    }
    
    // Update board
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Board board)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_EditModal", board);
        }
        
        await _boardService.UpdateAsync(id, board);
        
        // Refresh the board list
        var result = await _boardService.GetAllAsync();
        return PartialView("_BoardList", result);
    }
    
    // TTW pattern: Delete returns EmptyResult() for inline removal
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        await _boardService.DeleteAsync(id);
        return new EmptyResult();
    }
}
