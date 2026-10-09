using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Models
{
    public class AppUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string? Avatar { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}