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
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SpendingAnalysisService _spendingAnalysisService;

        public HomeController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            SpendingAnalysisService spendingAnalysisService)
        {
            _context = context;
            _userManager = userManager;
            _spendingAnalysisService = spendingAnalysisService;
        }


        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var today = DateTime.Today;

            var currentMonthStart =
                new DateTime(today.Year, today.Month, 1);

            var nextMonthStart =
                currentMonthStart.AddMonths(1);

            var previousMonthStart =
                currentMonthStart.AddMonths(-1);


            // =====================================================
            // GIAO DỊCH THÁNG HIỆN TẠI
            // =====================================================

            var currentMonthTransactions =
                await _context.Transactions
                    .Where(t =>
                        t.UserId == userId &&
                        t.TransactionDate >= currentMonthStart &&
                        t.TransactionDate < nextMonthStart)
                    .ToListAsync();


            var totalIncome =
                currentMonthTransactions
                    .Where(t => t.Type == "Income")
                    .Sum(t => t.Amount);


            var totalExpense =
                currentMonthTransactions
                    .Where(t => t.Type == "Expense")
                    .Sum(t => t.Amount);


            // Đây là chênh lệch THỰC TẾ
            // Không dùng để tính tiền được phép đưa vào Savings.
            var balance =
                totalIncome - totalExpense;


            // =====================================================
            // THÁNG TRƯỚC
            // =====================================================

            var previousMonthTransactions =
                await _context.Transactions
                    .Where(t =>
                        t.UserId == userId &&
                        t.TransactionDate >= previousMonthStart &&
                        t.TransactionDate < currentMonthStart)
                    .ToListAsync();


            var previousMonthIncome =
                previousMonthTransactions
                    .Where(t => t.Type == "Income")
                    .Sum(t => t.Amount);


            var previousMonthExpense =
                previousMonthTransactions
                    .Where(t => t.Type == "Expense")
                    .Sum(t => t.Amount);


            decimal incomeChangePercent = 0;

            if (previousMonthIncome > 0)
            {
                incomeChangePercent =
                    ((totalIncome - previousMonthIncome)
                     / previousMonthIncome) * 100;
            }


            decimal expenseChangePercent = 0;

            if (previousMonthExpense > 0)
            {
                expenseChangePercent =
                    ((totalExpense - previousMonthExpense)
                     / previousMonthExpense) * 100;
            }


            // =====================================================
            // SPENDING PLAN THÁNG HIỆN TẠI
            // =====================================================

            var spendingPlanLimit =
                await _context.SpendingPlans
                    .Where(b =>
                        b.UserId == userId &&
                        b.Month == today.Month &&
                        b.Year == today.Year)
                    .SumAsync(b =>
                        (decimal?)b.PlannedAmount)
                ?? 0;


            // Chi thực tế chỉ của các danh mục đã có kế hoạch trong tháng.
            // Kế hoạch chỉ dùng để so sánh, không ảnh hưởng ngân sách/còn lại.
            var plannedCategoryIds = await _context.SpendingPlans
                .Where(b => b.UserId == userId && b.Month == today.Month && b.Year == today.Year)
                .Select(b => b.CategoryId)
                .ToListAsync();

            var spendingPlanUsed = currentMonthTransactions
                .Where(t => t.Type == "Expense" && plannedCategoryIds.Contains(t.CategoryId))
                .Sum(t => t.Amount);


            // QUAN TRỌNG:
            // Không Math.Max(..., 0).
            // Cho phép âm để biết tháng này vượt SpendingPlan.
            var spendingPlanRemaining =
                spendingPlanLimit - spendingPlanUsed;


            decimal spendingPlanPercent = 0;

            if (spendingPlanLimit > 0)
            {
                spendingPlanPercent =
                    spendingPlanUsed /
                    spendingPlanLimit *
                    100;
            }


            // =====================================================
            // SAVINGS THÁNG HIỆN TẠI
            // =====================================================

            var monthlySavings =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId &&
                        s.ContributionDate >= currentMonthStart &&
                        s.ContributionDate < nextMonthStart)
                    .SumAsync(s =>
                        (decimal?)s.Amount)
                ?? 0;


            // =====================================================
            // TIỀN CÒN LẠI THỰC TẾ
            // Ngân sách = Tổng thu nhập tháng
            // Còn lại = Thu nhập - Chi thực tế - Tiết kiệm
            // Kế hoạch chi tiêu (SpendingPlans) KHÔNG tham gia công thức này.
            // =====================================================

            balance =
                totalIncome
                - totalExpense
                - monthlySavings;

            // Giữ property cũ để không làm vỡ các phần chưa dùng tới.
            var remainingBalance = balance;


            // =====================================================
            // SỐ GIAO DỊCH
            // =====================================================

            var transactionCount =
                currentMonthTransactions.Count;


            // =====================================================
            // GIAO DỊCH GẦN ĐÂY
            // =====================================================

            var recentTransactions =
                await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t =>
                        t.UserId == userId)
                    .OrderByDescending(t =>
                        t.TransactionDate)
                    .ThenByDescending(t =>
                        t.CreatedAt)
                    .Take(5)
                    .ToListAsync();


            // =====================================================
            // BIỂU ĐỒ 6 THÁNG
            // =====================================================

            var monthLabels =
                new List<string>();

            var monthlyIncome =
                new List<decimal>();

            var monthlyExpense =
                new List<decimal>();


            for (var i = 5; i >= 0; i--)
            {
                var month =
                    currentMonthStart.AddMonths(-i);

                var monthEnd =
                    month.AddMonths(1);


                var monthTransactions =
                    await _context.Transactions
                        .Where(t =>
                            t.UserId == userId &&
                            t.TransactionDate >= month &&
                            t.TransactionDate < monthEnd)
                        .ToListAsync();


                monthLabels.Add(
                    $"T{month.Month}/{month.Year}");


                monthlyIncome.Add(
                    monthTransactions
                        .Where(t =>
                            t.Type == "Income")
                        .Sum(t =>
                            t.Amount));


                monthlyExpense.Add(
                    monthTransactions
                        .Where(t =>
                            t.Type == "Expense")
                        .Sum(t =>
                            t.Amount));
            }


            // =====================================================
            // CHI TIÊU THEO DANH MỤC
            // =====================================================

            var categoryExpenses =
                await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t =>
                        t.UserId == userId &&
                        t.Type == "Expense" &&
                        t.TransactionDate >= currentMonthStart &&
                        t.TransactionDate < nextMonthStart)
                    .GroupBy(t =>
                        t.Category != null
                            ? t.Category.Name
                            : "Khác")
                    .Select(g => new
                    {
                        CategoryName = g.Key,
                        Amount = g.Sum(t => t.Amount)
                    })
                    .OrderByDescending(x =>
                        x.Amount)
                    .ToListAsync();


            var categoryLabels =
                categoryExpenses
                    .Select(x => x.CategoryName)
                    .ToList();


            var categoryExpenseValues =
                categoryExpenses
                    .Select(x => x.Amount)
                    .ToList();


            // =====================================================
            // SAVINGS GOALS
            // =====================================================

            var savingsGoals =
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


            var savingsGoalCount =
                savingsGoals.Count;


            var completedSavingsGoalCount =
                savingsGoals.Count(s =>
                    s.CurrentAmount >=
                    s.TargetAmount);


            var featuredSavingsGoal =
                savingsGoals
                    .Where(s =>
                        s.CurrentAmount <
                        s.TargetAmount)
                    .OrderBy(s =>
                        s.TargetDate == null)
                    .ThenBy(s =>
                        s.TargetDate)
                    .FirstOrDefault();


            if (featuredSavingsGoal == null)
            {
                featuredSavingsGoal =
                    savingsGoals
                        .OrderByDescending(s =>
                            s.UpdatedAt ??
                            s.CreatedAt)
                        .FirstOrDefault();
            }


            // =====================================================
            // PHÂN TÍCH CHI TIÊU
            // =====================================================

            var spendingAnalysis =
                await _spendingAnalysisService
                    .AnalyzeAsync(userId);


            // =====================================================
            // VIEW MODEL
            // =====================================================

            var model =
                new DashboardViewModel
                {
                    TotalIncome =
                        totalIncome,

                    TotalExpense =
                        totalExpense,

                    Balance =
                        balance,


                    PreviousMonthIncome =
                        previousMonthIncome,

                    PreviousMonthExpense =
                        previousMonthExpense,

                    IncomeChangePercent =
                        Math.Round(
                            incomeChangePercent,
                            1),

                    ExpenseChangePercent =
                        Math.Round(
                            expenseChangePercent,
                            1),


                    SpendingPlanLimit =
                        spendingPlanLimit,

                    SpendingPlanUsed =
                        spendingPlanUsed,

                    SpendingPlanRemaining =
                        spendingPlanRemaining,

                    SpendingPlanPercent =
                        Math.Round(
                            spendingPlanPercent,
                            1),


                    MonthlySavings =
                        monthlySavings,

                    RemainingBalance =
                        remainingBalance,


                    TransactionCount =
                        transactionCount,

                    RecentTransactions =
                        recentTransactions,


                    MonthLabels =
                        monthLabels,

                    MonthlyIncome =
                        monthlyIncome,

                    MonthlyExpense =
                        monthlyExpense,


                    CategoryLabels =
                        categoryLabels,

                    CategoryExpenseValues =
                        categoryExpenseValues,


                    FeaturedSavingsGoal =
                        featuredSavingsGoal,

                    SavingsGoals =
                        savingsGoals,

                    SavingsGoalCount =
                        savingsGoalCount,

                    CompletedSavingsGoalCount =
                        completedSavingsGoalCount,


                    SpendingAnalysis =
                        spendingAnalysis
                };


            return View(model);
        }
    }
}