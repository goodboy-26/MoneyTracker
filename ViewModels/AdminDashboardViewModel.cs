using MoneyTracker.Models;

namespace MoneyTracker.ViewModels
{
    public class AdminDashboardViewModel
    {
        // KPI
        public int TotalUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }

        public int TotalTransactions { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSpendingPlans { get; set; }

        // Role
        public int AdminCount { get; set; }
        public int UserCount { get; set; }

        // Category
        public int IncomeCategoryCount { get; set; }
        public int ExpenseCategoryCount { get; set; }

        // Biểu đồ 6 tháng
        public List<string> MonthLabels { get; set; } = new();
        public List<int> NewUserValues { get; set; } = new();
        public List<int> TransactionValues { get; set; } = new();

        // User mới
        public List<AppUser> RecentUsers { get; set; } = new();
    }
}