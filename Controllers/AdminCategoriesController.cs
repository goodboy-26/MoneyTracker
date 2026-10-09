using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Data;
using MoneyTracker.Models;
using MoneyTracker.ViewModels;

namespace MoneyTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public AdminCategoriesController(AppDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Where(c => c.IsDefault)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryViewModel
            {
                Type = "Expense"
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var exists = await _context.Categories
                .AnyAsync(c =>
                    c.IsDefault &&
                    c.Type == model.Type &&
                    c.Name == model.Name);

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Danh mục hệ thống này đã tồn tại.");

                return View(model);
            }

            var category = new Category
            {
                Name = model.Name.Trim(),
                Type = model.Type,
                IsDefault = true,
                UserId = null,
                CreatedAt = DateTime.Now
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Thêm danh mục hệ thống thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.IsDefault);

            if (category == null)
                return NotFound();

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
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == model.Id &&
                    c.IsDefault);

            if (category == null)
                return NotFound();

            var exists = await _context.Categories
                .AnyAsync(c =>
                    c.Id != model.Id &&
                    c.IsDefault &&
                    c.Type == model.Type &&
                    c.Name == model.Name);

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Danh mục hệ thống này đã tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(model);

            category.Name = model.Name.Trim();
            category.Type = model.Type;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật danh mục hệ thống thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.IsDefault);

            if (category == null)
                return NotFound();

            return View(category);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.IsDefault);

            if (category == null)
                return NotFound();

            var usedInTransactions =
                await _context.Transactions
                    .AnyAsync(t =>
                        t.CategoryId == id);

            var usedInSpendingPlans =
                await _context.SpendingPlans
                    .AnyAsync(b =>
                        b.CategoryId == id);

            if (usedInTransactions || usedInSpendingPlans)
            {
                TempData["Error"] =
                    "Không thể xóa vì danh mục này đang được sử dụng.";

                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Xóa danh mục hệ thống thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}