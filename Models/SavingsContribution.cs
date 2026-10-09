using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Models
{
    public class SavingsContribution
    {
        public int Id { get; set; }

        [Required]
        public int SavingsGoalId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(1, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime ContributionDate { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public SavingsGoal? SavingsGoal { get; set; }

        public AppUser? User { get; set; }
    }
}