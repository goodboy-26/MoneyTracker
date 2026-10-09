using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Content { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = "Info";

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public AppUser? User { get; set; }
    }
}