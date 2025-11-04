using Microsoft.AspNetCore.Mvc;
using ModularMonolithDemo.Modules.Todos.Contracts;
using ModularMonolithDemo.Modules.Todos.Module;

namespace ModularMonolithDemo.Modules.Todos.Web.ViewComponents;

public class TodosListViewComponent : ViewComponent
{
    private readonly ITodoService _service;
    public TodosListViewComponent(ITodoService service) { _service = service; }

    public IViewComponentResult Invoke()
    {
        var items = _service.GetAll().ToList();
        return View(items);
    }
}
