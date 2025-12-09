using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Events;
using SwapModularMonolith.Modules.Notes.Entities;
using SwapModularMonolith.Modules.Notes.Events;
using SwapModularMonolith.Modules.Notes.Services;

namespace SwapModularMonolith.Modules.Notes.Controllers;

public class NotesController : SwapController
{
    private readonly INotesService _service;

    public NotesController(INotesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var notes = await _service.GetAllAsync();
        return SwapView(notes);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var notes = await _service.GetAllAsync();
        return SwapView("_NotesList", notes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return SwapView("_CreateModal");
    }

    [HttpPost]
    public async Task<IActionResult> Create(Note note)
    {
        if (!ModelState.IsValid)
        {
            return SwapView("_CreateModal", note);
        }

        await _service.CreateAsync(note);

        return SwapResponse()
            .WithSuccessToast("Note created!")
            .WithTrigger(NotesEvents.Notes.ListChanged)  // Triggers client refresh
            .WithView("_ModalClose")
            .Build();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var note = await _service.GetByIdAsync(id);
        if (note == null) return NotFound();

        return SwapView("_EditModal", note);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Note note)
    {
        if (!ModelState.IsValid)
        {
            return SwapView("_EditModal", note);
        }

        try
        {
            await _service.UpdateAsync(id, note);
            return SwapResponse()
                .WithSuccessToast("Note updated!")
                .WithTrigger(NotesEvents.Notes.ListChanged)  // Triggers client refresh
                .WithView("_ModalClose")
                .Build();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> DeleteConfirm(int id)
    {
        var note = await _service.GetByIdAsync(id);
        if (note == null) return NotFound();

        return SwapView("_DeleteConfirmModal", note);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return SwapResponse()
                .WithSuccessToast("Note deleted!")
                .WithTrigger(NotesEvents.Notes.ListChanged)  // Triggers client refresh
                .WithView("_ModalClose")
                .Build();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> TogglePin(int id)
    {
        try
        {
            await _service.TogglePinAsync(id);
            return SwapResponse()
                .WithTrigger(NotesEvents.Notes.ListChanged)  // Triggers client refresh
                .Build();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}