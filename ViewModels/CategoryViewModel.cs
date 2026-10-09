using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Loại")]
        public string Type { get; set; } = "Expense";
    }
}