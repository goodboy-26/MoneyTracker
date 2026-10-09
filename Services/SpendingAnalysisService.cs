using Microsoft.EntityFrameworkCore;
using MoneyTracker.Data;
using MoneyTracker.ViewModels;

namespace MoneyTracker.Services
{
    public class SpendingAnalysisService
    {
        private readonly AppDbContext _context;

        public SpendingAnalysisService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<SpendingAnalysisViewModel> AnalyzeAsync(
            string userId)
        {
            var today = DateTime.Today;

            // ==============================
            // THÁNG HIỆN TẠI
            // ==============================

            var currentMonthStart =
                new DateTime(
                    today.Year,
                    today.Month,
                    1);

            var nextMonthStart =
                currentMonthStart.AddMonths(1);


            // ==============================
            // THÁNG TRƯỚC
            // ==============================

            var previousMonthStart =
                currentMonthStart.AddMonths(-1);


            // ==============================
            // GIAO DỊCH CHI THÁNG NÀY
            // ==============================

            var currentExpenses =
                await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t =>
                        t.UserId == userId &&
                        t.Type == "Expense" &&
                        t.TransactionDate >= currentMonthStart &&
                        t.TransactionDate < nextMonthStart)
                    .ToListAsync();


            var currentMonthExpense =
                currentExpenses.Sum(t => t.Amount);


            // ==============================
            // TỔNG CHI THÁNG TRƯỚC
            // ==============================

            var previousMonthExpense =
                await _context.Transactions
                    .Where(t =>
                        t.UserId == userId &&
                        t.Type == "Expense" &&
                        t.TransactionDate >= previousMonthStart &&
                        t.TransactionDate < currentMonthStart)
                    .SumAsync(t => (decimal?)t.Amount)
                ?? 0;


            // ==============================
            // % THAY ĐỔI
            // ==============================

            decimal expenseChangePercent = 0;

            if (previousMonthExpense > 0)
            {
                expenseChangePercent =
                    ((currentMonthExpense -
                      previousMonthExpense)
                     / previousMonthExpense)
                    * 100;
            }


            // ==============================
            // DANH MỤC CHI NHIỀU NHẤT
            // ==============================

            var topCategory =
                currentExpenses
                    .Where(t => t.Category != null)
                    .GroupBy(t => new
                    {
                        t.CategoryId,
                        t.Category!.Name
                    })
                    .Select(g => new
                    {
                        Name = g.Key.Name,
                        Amount = g.Sum(x => x.Amount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .FirstOrDefault();


            decimal topCategoryPercent = 0;

            if (topCategory != null &&
                currentMonthExpense > 0)
            {
                topCategoryPercent =
                    topCategory.Amount /
                    currentMonthExpense *
                    100;
            }


            // ==============================
            // DỰ BÁO CUỐI THÁNG
            // ==============================

            var daysPassed =
                today.Day;

            var daysInMonth =
                DateTime.DaysInMonth(
                    today.Year,
                    today.Month);

            decimal averageDailyExpense = 0;
            decimal predictedMonthExpense = 0;

            if (daysPassed > 0)
            {
                averageDailyExpense =
                    currentMonthExpense /
                    daysPassed;

                predictedMonthExpense =
                    averageDailyExpense *
                    daysInMonth;
            }


            // ==============================
            // NGÂN SÁCH THÁNG
            // ==============================

            // Ngân sách tháng = tổng thu nhập tháng.
            // SpendingPlans chỉ là kế hoạch chi tiêu theo danh mục.
            var spendingPlanLimit =
                await _context.Transactions
                    .Where(t =>
                        t.UserId == userId &&
                        t.Type == "Income" &&
                        t.TransactionDate >= currentMonthStart &&
                        t.TransactionDate < nextMonthStart)
                    .SumAsync(t => (decimal?)t.Amount)
                ?? 0;


            decimal spendingPlanPercent = 0;

            if (spendingPlanLimit > 0)
            {
                spendingPlanPercent =
                    currentMonthExpense /
                    spendingPlanLimit *
                    100;
            }


            // ==============================
            // TẠO VIEW MODEL
            // ==============================

            var model =
                new SpendingAnalysisViewModel
                {
                    CurrentMonthExpense =
                        currentMonthExpense,

                    PreviousMonthExpense =
                        previousMonthExpense,

                    ExpenseChangePercent =
                        Math.Round(
                            expenseChangePercent,
                            1),

                    TopCategoryName =
                        topCategory?.Name,

                    TopCategoryAmount =
                        topCategory?.Amount ?? 0,

                    TopCategoryPercent =
                        Math.Round(
                            topCategoryPercent,
                            1),

                    PredictedMonthExpense =
                        Math.Round(
                            predictedMonthExpense,
                            0),

                    DaysPassed =
                        daysPassed,

                    DaysInMonth =
                        daysInMonth,

                    AverageDailyExpense =
                        Math.Round(
                            averageDailyExpense,
                            0),

                    SpendingPlanLimit =
                        spendingPlanLimit,

                    SpendingPlanPercent =
                        Math.Round(
                            spendingPlanPercent,
                            1)
                };


            // ==============================
            // TẠO NHẬN XÉT TỰ ĐỘNG
            // ==============================

            CreateInsights(model);

            return model;
        }


        private static void CreateInsights(
            SpendingAnalysisViewModel model)
        {
            // Chưa có dữ liệu
            if (model.CurrentMonthExpense <= 0)
            {
                model.Insights.Add(
                    "Bạn chưa có khoản chi nào trong tháng này.");

                return;
            }


            // ==============================
            // SO SÁNH THÁNG TRƯỚC
            // ==============================

            if (model.PreviousMonthExpense <= 0)
            {
                model.Insights.Add(
                    "Chưa có đủ dữ liệu tháng trước để so sánh chi tiêu.");
            }
            else if (model.ExpenseChangePercent > 0)
            {
                model.Insights.Add(
                    $"Chi tiêu tháng này đang cao hơn " +
                    $"{model.ExpenseChangePercent:N1}% " +
                    $"so với tổng chi tháng trước.");
            }
            else if (model.ExpenseChangePercent < 0)
            {
                model.Insights.Add(
                    $"Chi tiêu tháng này hiện thấp hơn " +
                    $"{Math.Abs(model.ExpenseChangePercent):N1}% " +
                    $"so với tổng chi tháng trước.");
            }
            else
            {
                model.Insights.Add(
                    "Chi tiêu tháng này hiện bằng tổng chi tháng trước.");
            }


            // ==============================
            // DANH MỤC LỚN NHẤT
            // ==============================

            if (!string.IsNullOrEmpty(
                    model.TopCategoryName))
            {
                model.Insights.Add(
                    $"{model.TopCategoryName} hiện là danh mục " +
                    $"chi nhiều nhất với " +
                    $"{model.TopCategoryAmount:N0} ₫, " +
                    $"chiếm {model.TopCategoryPercent:N1}% tổng chi.");
            }


            // ==============================
            // DỰ BÁO
            // ==============================

            if (model.PredictedMonthExpense > 0)
            {
                model.Insights.Add(
                    $"Nếu duy trì tốc độ chi tiêu hiện tại, " +
                    $"tổng chi cuối tháng ước tính khoảng " +
                    $"{model.PredictedMonthExpense:N0} ₫.");
            }


            // ==============================
            // NGÂN SÁCH
            // ==============================

            if (model.SpendingPlanLimit <= 0)
            {
                model.Insights.Add(
                    "Tháng này chưa có thu nhập nên ngân sách hiện là 0 ₫.");
            }
            else if (model.SpendingPlanPercent >= 100)
            {
                model.Insights.Add(
                    $"Bạn đã vượt ngân sách tháng " +
                    $"({model.SpendingPlanPercent:N1}% đã sử dụng).");
            }
            else if (model.SpendingPlanPercent >= 80)
            {
                model.Insights.Add(
                    $"Bạn đã sử dụng {model.SpendingPlanPercent:N1}% " +
                    $"ngân sách tháng. Hãy chú ý các khoản chi tiếp theo.");
            }
            else
            {
                model.Insights.Add(
                    $"Bạn đã sử dụng {model.SpendingPlanPercent:N1}% " +
                    $"ngân sách tháng.");
            }
        }
    }
}