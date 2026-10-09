using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyTracker.Models;
using MoneyTracker.ViewModels;

namespace MoneyTracker.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Avatar = user.Avatar
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            // Giữ avatar hiện tại nếu form lỗi
            model.Avatar = user.Avatar;

            if (!ModelState.IsValid)
                return View(model);


            // KIỂM TRA EMAIL

            var emailUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (emailUser != null &&
                emailUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email này đã được sử dụng.");

                return View(model);
            }

            // XỬ LÝ AVATAR
   

            if (model.AvatarFile != null &&
                model.AvatarFile.Length > 0)
            {
                // Tối đa 5 MB
                if (model.AvatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "AvatarFile",
                        "Ảnh đại diện không được lớn hơn 5 MB.");

                    return View(model);
                }


                var extension =
                    Path.GetExtension(
                        model.AvatarFile.FileName)
                    .ToLowerInvariant();


                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };


                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "AvatarFile",
                        "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");

                    return View(model);
                }


                // Tạo thư mục nếu chưa tồn tại
                var avatarFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "avatars");


                Directory.CreateDirectory(avatarFolder);


                // Tạo tên file ngẫu nhiên
                var fileName =
                    Guid.NewGuid().ToString("N")
                    + extension;


                var filePath =
                    Path.Combine(
                        avatarFolder,
                        fileName);


                // Lưu file
                using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await model.AvatarFile
                        .CopyToAsync(stream);
                }


                // XÓA AVATAR CŨ

                if (!string.IsNullOrWhiteSpace(user.Avatar))
                {
                    var oldAvatar =
                        user.Avatar.TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar);


                    var oldAvatarPath =
                        Path.Combine(
                            _environment.WebRootPath,
                            oldAvatar);


                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(
                            oldAvatarPath);
                    }
                }


                // Lưu đường dẫn mới
                user.Avatar =
                    "/uploads/avatars/" + fileName;
            }



            // CẬP NHẬT THÔNG TIN

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.Now;


            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                user.UserName = model.Email;

                // Đồng bộ normalized fields
                user.NormalizedEmail =
                    _userManager.NormalizeEmail(model.Email);

                user.NormalizedUserName =
                    _userManager.NormalizeName(model.Email);
            }


            var result =
                await _userManager.UpdateAsync(user);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                model.Avatar = user.Avatar;

                return View(model);
            }


            await _signInManager
                .RefreshSignInAsync(user);


            TempData["Success"] =
                "Cập nhật hồ sơ thành công.";


            return RedirectToAction(
                nameof(Index));
        }


        // ĐỔI MẬT KHẨU

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);


            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
                return NotFound();


            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }


            await _signInManager
                .RefreshSignInAsync(user);


            TempData["Success"] =
                "Đổi mật khẩu thành công.";


            return RedirectToAction(
                nameof(ChangePassword));
        }
    }
}