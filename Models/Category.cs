using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty;
        // Income hoặc Expense

        public string? UserId { get; set; }

        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public AppUser? User { get; set; }

        public ICollection<Transaction> Transactions { get; set; }
            = new List<Transaction>();

        public ICollection<SpendingPlan> SpendingPlans { get; set; }
            = new List<SpendingPlan>();
    }
}