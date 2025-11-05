using System.Collections.Generic;

namespace ModularMonolithDemo.Modules.Todos.Contracts;

public interface ITodoService
{
    IReadOnlyList<TodoItemDto> GetAll();
    TodoItemDto Add(string title);
    TodoItemDto? Toggle(int id);
    bool Delete(int id);
}
