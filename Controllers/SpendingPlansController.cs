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
    public class SpendingPlansController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public SpendingPlansController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<List<Category>> GetExpenseCategoriesAsync(string userId)
        {
            return await _context.Categories
                .Where(c => c.Type == "Expense" && (c.IsDefault || c.UserId == userId))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;
            var monthStart = new DateTime(selectedYear, selectedMonth, 1);
            var monthEnd = monthStart.AddMonths(1);

            var plans = await _context.SpendingPlans
                .Include(p => p.Category)
                .Where(p => p.UserId == userId && p.Month == selectedMonth && p.Year == selectedYear)
                .OrderBy(p => p.Category!.Name)
                .ToListAsync();

            var monthlyIncome = await _context.Transactions
                .Where(t => t.UserId == userId && t.Type == "Income" &&
                            t.TransactionDate >= monthStart && t.TransactionDate < monthEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var monthlyExpense = await _context.Transactions
                .Where(t => t.UserId == userId && t.Type == "Expense" &&
                            t.TransactionDate >= monthStart && t.TransactionDate < monthEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var monthlySavings = await _context.SavingsContributions
                .Where(s => s.UserId == userId &&
                            s.ContributionDate >= monthStart && s.ContributionDate < monthEnd)
                .SumAsync(s => (decimal?)s.Amount) ?? 0;

            var expensesByCategory = await _context.Transactions
                .Where(t => t.UserId == userId && t.Type == "Expense" &&
                            t.TransactionDate >= monthStart && t.TransactionDate < monthEnd)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => t.Amount) })
                .ToListAsync();

            ViewBag.ActualExpenses = expensesByCategory.ToDictionary(x => x.CategoryId, x => x.Amount);
            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;
            ViewBag.MonthlyIncome = monthlyIncome;       // Ngân sách = tổng thu
            ViewBag.TotalSpendingPlan = plans.Sum(x => x.PlannedAmount); // Tổng kế hoạch
            ViewBag.MonthlySavings = monthlySavings;
            ViewBag.MonthlyExpense = monthlyExpense;
            ViewBag.RemainingBalance = monthlyIncome - monthlyExpense - monthlySavings; // Còn lại thực tế

            return View(plans);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            ViewBag.Categories = await GetExpenseCategoriesAsync(userId);
            return View(new SpendingPlanViewModel { Month = DateTime.Now.Month, Year = DateTime.Now.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpendingPlanViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var categoryValid = await _context.Categories.AnyAsync(c =>
                c.Id == model.CategoryId && c.Type == "Expense" && (c.IsDefault || c.UserId == userId));
            if (!categoryValid)
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục chi tiêu không hợp lệ.");

            var duplicate = await _context.SpendingPlans.AnyAsync(p =>
                p.UserId == userId && p.CategoryId == model.CategoryId &&
                p.Month == model.Month && p.Year == model.Year);
            if (duplicate)
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục này đã có kế hoạch chi tiêu trong tháng đã chọn.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await GetExpenseCategoriesAsync(userId);
                return View(model);
            }

            _context.SpendingPlans.Add(new SpendingPlan
            {
                UserId = userId,
                CategoryId = model.CategoryId,
                PlannedAmount = model.PlannedAmount,
                Month = model.Month,
                Year = model.Year,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm kế hoạch chi tiêu.";
            return RedirectToAction(nameof(Index), new { month = model.Month, year = model.Year });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var plan = await _context.SpendingPlans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (plan == null) return NotFound();

            ViewBag.Categories = await GetExpenseCategoriesAsync(userId);
            return View(new SpendingPlanViewModel
            {
                Id = plan.Id,
                CategoryId = plan.CategoryId,
                PlannedAmount = plan.PlannedAmount,
                Month = plan.Month,
                Year = plan.Year
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SpendingPlanViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var plan = await _context.SpendingPlans.FirstOrDefaultAsync(p => p.Id == model.Id && p.UserId == userId);
            if (plan == null) return NotFound();

            var categoryValid = await _context.Categories.AnyAsync(c =>
                c.Id == model.CategoryId && c.Type == "Expense" && (c.IsDefault || c.UserId == userId));
            if (!categoryValid)
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục chi tiêu không hợp lệ.");

            var duplicate = await _context.SpendingPlans.AnyAsync(p =>
                p.Id != model.Id && p.UserId == userId && p.CategoryId == model.CategoryId &&
                p.Month == model.Month && p.Year == model.Year);
            if (duplicate)
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục này đã có kế hoạch chi tiêu trong tháng đã chọn.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await GetExpenseCategoriesAsync(userId);
                return View(model);
            }

            plan.CategoryId = model.CategoryId;
            plan.PlannedAmount = model.PlannedAmount;
            plan.Month = model.Month;
            plan.Year = model.Year;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật kế hoạch chi tiêu thành công.";
            return RedirectToAction(nameof(Index), new { month = model.Month, year = model.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var plan = await _context.SpendingPlans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (plan == null) return NotFound();

            var month = plan.Month;
            var year = plan.Year;
            _context.SpendingPlans.Remove(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa kế hoạch chi tiêu.";
            return RedirectToAction(nameof(Index), new { month, year });
        }
    }
}
