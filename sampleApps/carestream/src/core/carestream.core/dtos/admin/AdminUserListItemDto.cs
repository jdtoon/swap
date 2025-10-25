// carestream.core.dtos.admin.AdminUserListItemDto.cs
namespace carestream.core.dtos.admin
{
    public class AdminUserListItemDto
    {
        public int UserId { get; set; }
        public string ForceNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Rank { get; set; }
        public string? Department { get; set; }
        public string? LogtoSub { get; set; } // The Logto Subject ID
        public bool IsLinkedToLogto => !string.IsNullOrWhiteSpace(LogtoSub);
        public string Roles { get; set; } = string.Empty; // Display string of Carestream roles
        public bool IsActive { get; set; }
        public string? FacilityName { get; set; } // ADDED to display default facility in list
    }
}