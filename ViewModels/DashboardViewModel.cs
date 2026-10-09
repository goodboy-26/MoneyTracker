using MoneyTracker.Models;

namespace MoneyTracker.ViewModels
{
    public class DashboardViewModel
    {
        // ==============================
        // THU / CHI
        // ==============================

        public decimal TotalIncome { get; set; }

        public decimal TotalExpense { get; set; }

        // Chênh lệch thực tế Thu - Chi
        public decimal Balance { get; set; }


        // ==============================
        // SO SÁNH THÁNG TRƯỚC
        // ==============================

        public decimal PreviousMonthIncome { get; set; }

        public decimal PreviousMonthExpense { get; set; }

        public decimal IncomeChangePercent { get; set; }

        public decimal ExpenseChangePercent { get; set; }


        // ==============================
        // PHÂN BỔ TÀI CHÍNH
        // ==============================

        // Tổng SpendingPlan đã phân bổ trong tháng
        public decimal SpendingPlanLimit { get; set; }

        // Tổng Expense thực tế
        public decimal SpendingPlanUsed { get; set; }

        // SpendingPlan - Expense
        // Có thể âm nếu chi vượt ngân sách
        public decimal SpendingPlanRemaining { get; set; }

        public decimal SpendingPlanPercent { get; set; }


        // Tổng tiền đưa vào Savings trong tháng
        public decimal MonthlySavings { get; set; }

        // Income - SpendingPlan - Savings
        public decimal RemainingBalance { get; set; }


        // ==============================
        // GIAO DỊCH
        // ==============================

        public int TransactionCount { get; set; }

        public List<Transaction> RecentTransactions { get; set; } = new();


        // ==============================
        // BIỂU ĐỒ 6 THÁNG
        // ==============================

        public List<string> MonthLabels { get; set; } = new();

        public List<decimal> MonthlyIncome { get; set; } = new();

        public List<decimal> MonthlyExpense { get; set; } = new();


        // ==============================
        // CHI TIÊU THEO DANH MỤC
        // ==============================

        public List<string> CategoryLabels { get; set; } = new();

        public List<decimal> CategoryExpenseValues { get; set; } = new();


        // ==============================
        // SAVINGS GOAL
        // ==============================

        public SavingsGoal? FeaturedSavingsGoal { get; set; }

        public List<SavingsGoal> SavingsGoals { get; set; } = new();

        public int SavingsGoalCount { get; set; }

        public int CompletedSavingsGoalCount { get; set; }


        // ==============================
        // PHÂN TÍCH CHI TIÊU
        // ==============================

        public SpendingAnalysisViewModel SpendingAnalysis { get; set; }
            = new SpendingAnalysisViewModel();
    }
}