using System.Collections.Generic;

namespace carestream.core.dtos.admin
{
    public class AdminUserManagementViewModel
    {
        public List<AdminUserListItemDto> Users { get; set; } = new List<AdminUserListItemDto>();
        // Add properties for pagination and filtering later
        public string? SearchTerm { get; set; }
    }
}