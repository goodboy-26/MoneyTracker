using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MoneyTracker.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        // Ảnh hiện tại
        public string? Avatar { get; set; }

        // File ảnh mới người dùng chọn
        [Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }
    }
}