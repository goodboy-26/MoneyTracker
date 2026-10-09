namespace MoneyTracker.ViewModels
{
    public class SpendingAnalysisViewModel
    {
        // Tổng chi tháng hiện tại
        public decimal CurrentMonthExpense { get; set; }

        // Tổng chi tháng trước
        public decimal PreviousMonthExpense { get; set; }

        // % tăng/giảm so với tháng trước
        public decimal ExpenseChangePercent { get; set; }

        // Danh mục chi nhiều nhất
        public string? TopCategoryName { get; set; }

        public decimal TopCategoryAmount { get; set; }

        // % danh mục lớn nhất / tổng chi
        public decimal TopCategoryPercent { get; set; }

        // Dự kiến tổng chi cuối tháng
        public decimal PredictedMonthExpense { get; set; }

        // Số ngày đã qua trong tháng
        public int DaysPassed { get; set; }

        // Tổng số ngày của tháng
        public int DaysInMonth { get; set; }

        // Trung bình chi mỗi ngày
        public decimal AverageDailyExpense { get; set; }

        // Ngân sách
        public decimal SpendingPlanLimit { get; set; }

        public decimal SpendingPlanPercent { get; set; }

        // Nội dung nhận xét
        public List<string> Insights { get; set; } = new();
    }
}