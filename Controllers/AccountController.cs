using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyTracker.Models;
using MoneyTracker.ViewModels;

namespace MoneyTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }


        // ================================
        // REGISTER
        // ================================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email này đã được sử dụng.");

                return View(model);
            }

            var user = new AppUser
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = model.Email,
                CreatedAt = DateTime.Now
            };

            var result =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    user,
                    "User");

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            return View(model);
        }


        // ================================
        // LOGIN
        // ================================



        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            ViewBag.ReturnUrl = returnUrl;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không đúng.");

                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "Admin");
                }

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản của bạn đã bị khóa.");

                return View(model);
            }

            ModelState.AddModelError(
                string.Empty,
                "Email hoặc mật khẩu không đúng.");

            return View(model);
        }


        // ================================
        // LOGOUT
        // ================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }


        // ================================
        // ACCESS DENIED
        // ================================

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
    ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByEmailAsync(model.Email);

            // Không tiết lộ email có tồn tại hay không
            if (user == null)
            {
                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    email = user.Email,
                    token = token
                },
                Request.Scheme);

            // Tạm thời dùng để test LOCAL
            TempData["ResetLink"] = resetLink;

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(
            string? email,
            string? token)
        {
            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(token))
            {
                return BadRequest();
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}