using habits.Dtos;

namespace habits.Services.Tasks
{
    public interface IItemService
    {
        List<ItemDto> GetItems(int taskListId);
        ItemDto GetItem(int id);
        ItemDto CreateItem(ItemDto dto);
        ItemDto UpdateItem(ItemDto dto);
        ItemDto SetCompleted(int id, bool isComplete);
        ItemDto SetAsHeader(int id, bool isHeader);
        bool DeleteItem(int id);
        bool DeleteItems(List<int> ids);
        Task<bool> UpdateItemsOrderAsync(int id, int taskListId, int newPosition);
        List<UserDisplayDto> GetAllUsers();
        ItemDto AssignUsersToItem(int itemId, List<string> userIds);
    }
}
