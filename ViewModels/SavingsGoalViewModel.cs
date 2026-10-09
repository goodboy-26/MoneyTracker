using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.ViewModels
{
    public class SavingsGoalViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên mục tiêu")]
        [StringLength(150)]
        [Display(Name = "Tên mục tiêu")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền mục tiêu")]
        [Range(1, double.MaxValue,
            ErrorMessage = "Số tiền mục tiêu phải lớn hơn 0")]
        [Display(Name = "Số tiền mục tiêu")]
        public decimal TargetAmount { get; set; }

        [Display(Name = "Hạn hoàn thành")]
        public DateTime? TargetDate { get; set; }
    }


    public class AddSavingsAmountViewModel
    {
        public int Id { get; set; }

        public string GoalName { get; set; } = string.Empty;

        public decimal CurrentAmount { get; set; }

        public decimal TargetAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(1, double.MaxValue,
            ErrorMessage = "Số tiền phải lớn hơn 0")]
        [Display(Name = "Số tiền muốn thêm")]
        public decimal Amount { get; set; }
    }
}