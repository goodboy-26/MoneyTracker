using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Models
{
    public class SavingsGoal
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(1, double.MaxValue)]
        public decimal TargetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal CurrentAmount { get; set; }

        public DateTime? TargetDate { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public AppUser? User { get; set; }
    }
}