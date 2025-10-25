using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace habits.Data.Models
{
    [Table("AspNetUsers")]
    public class AppUser : IdentityUser
    {
        [StringLength(100)]
        public string? Name { get; set; } = null!;

        [StringLength(100)]
        public string? Surname { get; set; } = null!;

        [StringLength(20)]
        public string? Color { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public bool ReceiveNotifications { get; set; } = false;

        public bool ReceivePushNotifications { get; set; } = false;

        [StringLength(4096)]
        public string? FcmToken { get; set; }
    }
}