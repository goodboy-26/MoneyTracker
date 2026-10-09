using MoneyTracker.Models;

namespace MoneyTracker.ViewModels
{
    public class ReportViewModel
    {
        public string Period { get; set; } = "month";

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }


        // =========================
        // THU - CHI
        // =========================

        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        public decimal Balance { get; set; }


        // =========================
        // KẾ HOẠCH CHI TIÊU
        // =========================

        public decimal TotalSpendingPlan { get; set; }

        public decimal SpendingPlanExpense { get; set; }

        public decimal SpendingPlanVariance { get; set; }

        // Tổng kế hoạch - thực chi - tiết kiệm trong kỳ
        public decimal ActualRemainingAgainstPlan { get; set; }

        public decimal SpendingPlanUsagePercent { get; set; }


        // =========================
        // TIẾT KIỆM
        // =========================

        public decimal PeriodSavings { get; set; }

        public decimal TotalSavings { get; set; }


        // =========================
        // GIAO DỊCH
        // =========================

        public List<Transaction> Transactions { get; set; }
            = new();


        // =========================
        // BIỂU ĐỒ THU - CHI
        // =========================

        public List<string> ChartLabels { get; set; }
            = new();

        public List<decimal> IncomeValues { get; set; }
            = new();

        public List<decimal> ExpenseValues { get; set; }
            = new();


        // =========================
        // CHI TIÊU DANH MỤC
        // =========================

        public List<string> CategoryLabels { get; set; }
            = new();

        public List<decimal> CategoryValues { get; set; }
            = new();


        // =========================
        // SPENDING PLAN THEO DANH MỤC
        // =========================

        public List<SpendingPlanReportItemViewModel> SpendingPlanItems { get; set; }
            = new();


        // =========================
        // SAVINGS THEO THÁNG
        // =========================

        public List<SavingsMonthlyItemViewModel> SavingsMonthlyItems { get; set; }
            = new();
    }


    public class SpendingPlanReportItemViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public decimal PlannedAmount { get; set; }

        public decimal ActualExpense { get; set; }

        public decimal Variance { get; set; }

        public decimal UsagePercent { get; set; }
    }


    public class SavingsMonthlyItemViewModel
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public decimal Amount { get; set; }

        public string MonthLabel =>
            $"Tháng {Month}/{Year}";
    }
}