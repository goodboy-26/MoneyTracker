using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [DataType(DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}