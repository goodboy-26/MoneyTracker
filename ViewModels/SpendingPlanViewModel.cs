using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.ViewModels
{
    public class SpendingPlanViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền kế hoạch")]
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền kế hoạch phải lớn hơn 0")]
        [Display(Name = "Số tiền kế hoạch")]
        public decimal PlannedAmount { get; set; }

        [Range(1, 12)]
        [Display(Name = "Tháng")]
        public int Month { get; set; } = DateTime.Now.Month;

        [Display(Name = "Năm")]
        public int Year { get; set; } = DateTime.Now.Year;
    }
}