using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Models
{
    public class SpendingPlan
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(1, double.MaxValue)]
        public decimal PlannedAmount { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public AppUser? User { get; set; }

        public Category? Category { get; set; }
    }
}