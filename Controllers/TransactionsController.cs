using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Data;
using MoneyTracker.Models;
using MoneyTracker.Services;
using MoneyTracker.ViewModels;

namespace MoneyTracker.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SpendingPlanNotificationService _spendingPlanNotificationService;

        public TransactionsController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            SpendingPlanNotificationService spendingPlanNotificationService)
        {
            _context = context;
            _userManager = userManager;
            _spendingPlanNotificationService = spendingPlanNotificationService;
        }

        // =====================================================
        // DANH SÁCH + TÌM KIẾM + BỘ LỌC
        // =====================================================
        public async Task<IActionResult> Index(
            string? type,
            int? categoryId,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId)
                .AsQueryable();

            // Lọc Thu / Chi
            if (type == "Income" || type == "Expense")
            {
                query = query.Where(t => t.Type == type);
            }

            // Lọc danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(t =>
                    t.CategoryId == categoryId.Value);
            }

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(t =>
                    t.Title.Contains(search) ||
                    (t.Note != null &&
                     t.Note.Contains(search)));
            }

            // Từ ngày
            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                query = query.Where(t =>
                    t.TransactionDate >= startDate);
            }

            // Đến ngày
            if (toDate.HasValue)
            {
                var endDate =
                    toDate.Value.Date.AddDays(1);

                query = query.Where(t =>
                    t.TransactionDate < endDate);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.Type = type;
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");

            ViewBag.Categories =
                await GetCategories(userId);

            return View(transactions);
        }

        // =====================================================
        // THÊM GIAO DỊCH - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create(
            string? type = null)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // Chỉ chấp nhận 2 loại hợp lệ.
            // Dashboard truyền Income hoặc Expense vào đây.
            var selectedType =
                type == "Income"
                    ? "Income"
                    : "Expense";

            ViewBag.Categories =
                await GetCategories(userId);

            return View(
                new TransactionViewModel
                {
                    Type = selectedType,
                    TransactionDate = DateTime.Now
                });
        }

        // =====================================================
        // THÊM GIAO DỊCH - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TransactionViewModel model)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // Chặn giá trị Type giả
            if (model.Type != "Income" &&
                model.Type != "Expense")
            {
                ModelState.AddModelError(
                    nameof(model.Type),
                    "Loại giao dịch không hợp lệ.");
            }

            var category =
                await _context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.Id == model.CategoryId &&
                        (c.IsDefault ||
                         c.UserId == userId));

            if (category == null)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục không hợp lệ.");
            }
            else if (category.Type != model.Type)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục không phù hợp với loại giao dịch.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories =
                    await GetCategories(userId);

                return View(model);
            }

            var transaction =
                new Transaction
                {
                    Title = model.Title.Trim(),
                    Amount = model.Amount,
                    Type = model.Type,
                    CategoryId = model.CategoryId,
                    UserId = userId,
                    Note = model.Note?.Trim(),
                    TransactionDate = model.TransactionDate,
                    CreatedAt = DateTime.Now
                };

            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            // Expense KHÔNG bị chặn khi vượt SpendingPlan.
            // Chỉ kiểm tra để tạo cảnh báo.
            if (transaction.Type == "Expense")
            {
                await _spendingPlanNotificationService
                    .CheckSpendingPlanAsync(
                        userId,
                        transaction.CategoryId,
                        transaction.TransactionDate);
            }

            TempData["Success"] =
                transaction.Type == "Income"
                    ? "Thêm khoản thu thành công."
                    : "Thêm khoản chi thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CHỈNH SỬA - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var transaction =
                await _context.Transactions
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.UserId == userId);

            if (transaction == null)
                return NotFound();

            ViewBag.Categories =
                await GetCategories(userId);

            var model =
                new TransactionViewModel
                {
                    Id = transaction.Id,
                    Title = transaction.Title,
                    Amount = transaction.Amount,
                    Type = transaction.Type,
                    CategoryId = transaction.CategoryId,
                    TransactionDate =
                        transaction.TransactionDate,
                    Note = transaction.Note
                };

            return View(model);
        }

        // =====================================================
        // CHỈNH SỬA - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            TransactionViewModel model)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var transaction =
                await _context.Transactions
                    .FirstOrDefaultAsync(t =>
                        t.Id == model.Id &&
                        t.UserId == userId);

            if (transaction == null)
                return NotFound();

            if (model.Type != "Income" &&
                model.Type != "Expense")
            {
                ModelState.AddModelError(
                    nameof(model.Type),
                    "Loại giao dịch không hợp lệ.");
            }

            var category =
                await _context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.Id == model.CategoryId &&
                        (c.IsDefault ||
                         c.UserId == userId));

            if (category == null)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục không hợp lệ.");
            }
            else if (category.Type != model.Type)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục không phù hợp với loại giao dịch.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories =
                    await GetCategories(userId);

                return View(model);
            }

            transaction.Title =
                model.Title.Trim();

            transaction.Amount =
                model.Amount;

            transaction.Type =
                model.Type;

            transaction.CategoryId =
                model.CategoryId;

            transaction.TransactionDate =
                model.TransactionDate;

            transaction.Note =
                model.Note?.Trim();

            transaction.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            // Chỉ cảnh báo SpendingPlan, KHÔNG chặn Expense.
            if (transaction.Type == "Expense")
            {
                await _spendingPlanNotificationService
                    .CheckSpendingPlanAsync(
                        userId,
                        transaction.CategoryId,
                        transaction.TransactionDate);
            }

            TempData["Success"] =
                "Cập nhật giao dịch thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // XÓA - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var transaction =
                await _context.Transactions
                    .Include(t => t.Category)
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.UserId == userId);

            if (transaction == null)
                return NotFound();

            return View(transaction);
        }

        // =====================================================
        // XÓA - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var transaction =
                await _context.Transactions
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.UserId == userId);

            if (transaction == null)
                return NotFound();

            _context.Transactions.Remove(transaction);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Xóa giao dịch thành công.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // HELPER - DANH MỤC CỦA USER
        // =====================================================
        private async Task<List<Category>>
            GetCategories(string userId)
        {
            return await _context.Categories
                .Where(c =>
                    c.IsDefault ||
                    c.UserId == userId)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }
    }
}