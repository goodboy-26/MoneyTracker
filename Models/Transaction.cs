using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyTracker.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên giao dịch")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty;
        // Income / Expense

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Today;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public Category? Category { get; set; }

        public AppUser? User { get; set; }
    }
}