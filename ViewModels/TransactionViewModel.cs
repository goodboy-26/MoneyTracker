using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.ViewModels
{
    public class TransactionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên giao dịch")]
        [Display(Name = "Tên giao dịch")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Loại giao dịch")]
        public string Type { get; set; } = "Expense";

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Ngày giao dịch")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}