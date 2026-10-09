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
    public class SavingsGoalsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public SavingsGoalsController(
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =====================================================
        // TÍNH SỐ TIỀN CÓ THỂ TIẾT KIỆM THÊM TRONG THÁNG
        // =====================================================

        private async Task<decimal> GetAvailableForSavingsAsync(
            string userId,
            DateTime date)
        {
            var monthStart = new DateTime(date.Year, date.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var income = await _context.Transactions
                .Where(t => t.UserId == userId &&
                            t.Type == "Income" &&
                            t.TransactionDate >= monthStart &&
                            t.TransactionDate < monthEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expense = await _context.Transactions
                .Where(t => t.UserId == userId &&
                            t.Type == "Expense" &&
                            t.TransactionDate >= monthStart &&
                            t.TransactionDate < monthEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var savings = await _context.SavingsContributions
                .Where(x => x.UserId == userId &&
                            x.ContributionDate >= monthStart &&
                            x.ContributionDate < monthEnd)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            // Kế hoạch chi tiêu không làm giảm số tiền có thể tiết kiệm.
            return income - expense - savings;
        }

        // =====================================================
        // DANH SÁCH MỤC TIÊU
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var goals =
                await _context.SavingsGoals
                    .Where(s =>
                        s.UserId == userId)
                    .OrderBy(s =>
                        s.TargetDate == null)
                    .ThenBy(s =>
                        s.TargetDate)
                    .ThenByDescending(s =>
                        s.CreatedAt)
                    .ToListAsync();


            return View(goals);
        }


        // =====================================================
        // TẠO MỤC TIÊU
        // =====================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(
                new SavingsGoalViewModel
                {
                    TargetDate =
                        DateTime.Today.AddMonths(6)
                });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SavingsGoalViewModel model)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            if (model.TargetDate.HasValue &&
                model.TargetDate.Value.Date <
                DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.TargetDate),
                    "Hạn hoàn thành không được nhỏ hơn ngày hiện tại.");
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var goal =
                new SavingsGoal
                {
                    UserId = userId,

                    Name = model.Name,

                    Description =
                        model.Description,

                    TargetAmount =
                        model.TargetAmount,

                    CurrentAmount = 0,

                    TargetDate =
                        model.TargetDate,

                    CreatedAt =
                        DateTime.Now
                };


            _context.SavingsGoals.Add(goal);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Tạo mục tiêu tiết kiệm thành công.";


            return RedirectToAction(
                nameof(Index));
        }


        // =====================================================
        // SỬA MỤC TIÊU
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var goal =
                await _context.SavingsGoals
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.UserId == userId);


            if (goal == null)
            {
                return NotFound();
            }


            var model =
                new SavingsGoalViewModel
                {
                    Id =
                        goal.Id,

                    Name =
                        goal.Name,

                    Description =
                        goal.Description,

                    TargetAmount =
                        goal.TargetAmount,

                    TargetDate =
                        goal.TargetDate
                };


            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            SavingsGoalViewModel model)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var goal =
                await _context.SavingsGoals
                    .FirstOrDefaultAsync(s =>
                        s.Id == model.Id &&
                        s.UserId == userId);


            if (goal == null)
            {
                return NotFound();
            }


            if (model.TargetDate.HasValue &&
                model.TargetDate.Value.Date <
                DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.TargetDate),
                    "Hạn hoàn thành không được nhỏ hơn ngày hiện tại.");
            }


            // Không cho giảm mục tiêu xuống dưới tiền đã tiết kiệm
            if (model.TargetAmount <
                goal.CurrentAmount)
            {
                ModelState.AddModelError(
                    nameof(model.TargetAmount),
                    $"Bạn đã tiết kiệm {goal.CurrentAmount:N0} ₫. " +
                    "Mục tiêu không thể thấp hơn số tiền hiện tại.");
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            goal.Name =
                model.Name;

            goal.Description =
                model.Description;

            goal.TargetAmount =
                model.TargetAmount;

            goal.TargetDate =
                model.TargetDate;

            goal.UpdatedAt =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Cập nhật mục tiêu tiết kiệm thành công.";


            return RedirectToAction(
                nameof(Index));
        }


        // =====================================================
        // THÊM TIỀN VÀO SAVINGS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AddAmount(
            int id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var goal =
                await _context.SavingsGoals
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.UserId == userId);


            if (goal == null)
            {
                return NotFound();
            }


            var available =
                await GetAvailableForSavingsAsync(
                    userId,
                    DateTime.Now);


            ViewBag.AvailableForSavings =
                Math.Max(available, 0);


            var model =
                new AddSavingsAmountViewModel
                {
                    Id =
                        goal.Id,

                    GoalName =
                        goal.Name,

                    CurrentAmount =
                        goal.CurrentAmount,

                    TargetAmount =
                        goal.TargetAmount
                };


            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAmount(
            AddSavingsAmountViewModel model)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var goal =
                await _context.SavingsGoals
                    .FirstOrDefaultAsync(s =>
                        s.Id == model.Id &&
                        s.UserId == userId);


            if (goal == null)
            {
                return NotFound();
            }


            // Gán lại dữ liệu hiển thị
            model.GoalName =
                goal.Name;

            model.CurrentAmount =
                goal.CurrentAmount;

            model.TargetAmount =
                goal.TargetAmount;


            var now =
                DateTime.Now;


            var available =
                await GetAvailableForSavingsAsync(
                    userId,
                    now);


            ViewBag.AvailableForSavings =
                Math.Max(available, 0);


            // ==============================================
            // MỤC TIÊU ĐÃ HOÀN THÀNH
            // ==============================================

            var remainingGoal =
                goal.TargetAmount -
                goal.CurrentAmount;


            if (remainingGoal <= 0)
            {
                TempData["Success"] =
                    "Mục tiêu này đã hoàn thành.";

                return RedirectToAction(
                    nameof(Index));
            }


            // ==============================================
            // KHÔNG CHO NẠP QUÁ MỤC TIÊU
            // ==============================================

            if (model.Amount >
                remainingGoal)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    $"Mục tiêu chỉ còn thiếu {remainingGoal:N0} ₫.");
            }


            // ==============================================
            // KHÔNG CHO NẠP QUÁ TIỀN CHƯA PHÂN BỔ
            // ==============================================

            if (model.Amount >
                available)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    $"Tháng {now.Month}/{now.Year} " +
                    $"bạn chỉ còn {Math.Max(available, 0):N0} ₫ sau khi trừ chi tiêu và tiền đã tiết kiệm.");
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ==============================================
            // TĂNG TIỀN TRONG GOAL
            // ==============================================

            goal.CurrentAmount +=
                model.Amount;

            goal.UpdatedAt =
                DateTime.Now;


            // ==============================================
            // GHI LỊCH SỬ SAVINGS
            // ==============================================

            var contribution =
                new SavingsContribution
                {
                    SavingsGoalId =
                        goal.Id,

                    UserId =
                        userId,

                    Amount =
                        model.Amount,

                    ContributionDate =
                        now,

                    CreatedAt =
                        DateTime.Now
                };


            _context.SavingsContributions.Add(
                contribution);


            await _context.SaveChangesAsync();


            // ==============================================
            // THÔNG BÁO
            // ==============================================

            if (goal.CurrentAmount >=
                goal.TargetAmount)
            {
                TempData["Success"] =
                    $"Chúc mừng! Bạn đã hoàn thành mục tiêu \"{goal.Name}\".";
            }
            else
            {
                TempData["Success"] =
                    $"Đã thêm {model.Amount:N0} ₫ vào mục tiêu \"{goal.Name}\".";
            }


            return RedirectToAction(
                nameof(Index));
        }


        // =====================================================
        // XÓA MỤC TIÊU
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var goal =
                await _context.SavingsGoals
                    .FirstOrDefaultAsync(s =>
                        s.Id == id &&
                        s.UserId == userId);


            if (goal == null)
            {
                return NotFound();
            }


            // Không xóa mục tiêu đã có lịch sử tiết kiệm.
            // Nếu xóa sẽ làm mất dữ liệu báo cáo theo tháng.
            var hasContributions =
                await _context.SavingsContributions
                    .AnyAsync(s =>
                        s.SavingsGoalId == goal.Id &&
                        s.UserId == userId);


            if (hasContributions)
            {
                TempData["Error"] =
                    "Không thể xóa mục tiêu đã có tiền tiết kiệm vì sẽ làm mất lịch sử. " +
                    "Bạn có thể sửa mục tiêu thay vì xóa.";

                return RedirectToAction(
                    nameof(Index));
            }


            _context.SavingsGoals.Remove(
                goal);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Xóa mục tiêu tiết kiệm thành công.";


            return RedirectToAction(
                nameof(Index));
        }
    }
}