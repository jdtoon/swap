using habits.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace habits.Dtos
{
    public class MemberDto
    {
        public string Id { get; set; } = null!;

        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Surname { get; set; }

        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Color { get; set; }

        public bool IsActive { get; set; }

        public string Role { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public static MemberDto FromModel(AppUser user, AppRole role = null!)
        {
            return new MemberDto
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.UserName!,
                PhoneNumber = user.PhoneNumber!,
                IsActive = user.IsActive,
                Color = user.Color,
                Role = role != null ? role.Name! : ""
            };
        }
    }

    public class UserDisplayDto
    {
        public string Id { get; set; } = null!;
        public string Initial { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Color { get; set; } = null!;

        public static UserDisplayDto FromModel(AppUser user)
        {
            return new UserDisplayDto
            {
                Id = user.Id,
                Name = user.Name!,
                Initial = GetInitials(user.Name!),
                Color = user.Color!
            };
        }

        public static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return string.Join("", name
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part[0].ToString().ToUpper()));
        }
    }

    public class AssignUsersDto
    {
        public int ItemId { get; set; }
        public List<UserDisplayDto> Users { get; set; } = new();
        public List<string> AssignedUserIds { get; set; } = new();
    }
}