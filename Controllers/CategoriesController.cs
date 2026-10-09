using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Data;
using MoneyTracker.Models;
using MoneyTracker.ViewModels;

namespace MoneyTracker.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CategoriesController(
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var categories = await _context.Categories
                .Where(c => c.IsDefault || c.UserId == userId)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _context.Categories.AnyAsync(c =>
                c.Name == model.Name &&
                c.Type == model.Type &&
                (c.IsDefault || c.UserId == userId));

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Danh mục này đã tồn tại.");

                return View(model);
            }

            var category = new Category
            {
                Name = model.Name.Trim(),
                Type = model.Type,
                UserId = userId,
                IsDefault = false,
                CreatedAt = DateTime.Now
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UserId == userId &&
                    !c.IsDefault);

            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Type = category.Type
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == model.Id &&
                    c.UserId == userId &&
                    !c.IsDefault);

            if (category == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _context.Categories.AnyAsync(c =>
                c.Id != model.Id &&
                c.Name == model.Name &&
                c.Type == model.Type &&
                (c.IsDefault || c.UserId == userId));

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Danh mục này đã tồn tại.");

                return View(model);
            }

            category.Name = model.Name.Trim();
            category.Type = model.Type;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UserId == userId &&
                    !c.IsDefault);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UserId == userId &&
                    !c.IsDefault);

            if (category == null)
            {
                return NotFound();
            }

            var isUsed = await _context.Transactions
                .AnyAsync(t =>
                    t.CategoryId == id &&
                    t.UserId == userId);

            if (isUsed)
            {
                TempData["Error"] =
                    "Không thể xóa vì danh mục đang được sử dụng trong giao dịch.";

                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}