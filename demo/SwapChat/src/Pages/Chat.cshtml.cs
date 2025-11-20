using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SwapChat.Pages
{
    [Authorize]
    public class ChatModel : PageModel
    {
        public string Username { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string Room { get; set; } = "general";

        public void OnGet()
        {
            Username = User.Identity?.Name ?? "Unknown";
            IsAdmin = User.IsInRole("Admin");
        }
    }
}
