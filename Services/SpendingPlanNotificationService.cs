using Microsoft.EntityFrameworkCore;
using MoneyTracker.Data;
using MoneyTracker.Models;

namespace MoneyTracker.Services
{
    public class SpendingPlanNotificationService
    {
        private readonly AppDbContext _context;

        public SpendingPlanNotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CheckSpendingPlanAsync(
            string userId,
            int categoryId,
            DateTime transactionDate)
        {
            var month = transactionDate.Month;
            var year = transactionDate.Year;

            var spendingPlan = await _context.SpendingPlans
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b =>
                    b.UserId == userId &&
                    b.CategoryId == categoryId &&
                    b.Month == month &&
                    b.Year == year);

            if (spendingPlan == null || spendingPlan.PlannedAmount <= 0)
                return;

            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var spent = await _context.Transactions
                .Where(t =>
                    t.UserId == userId &&
                    t.CategoryId == categoryId &&
                    t.Type == "Expense" &&
                    t.TransactionDate >= monthStart &&
                    t.TransactionDate < monthEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var percent = spent / spendingPlan.PlannedAmount * 100;

            string? notificationType = null;
            string? content = null;

            if (percent >= 100)
            {
                notificationType = "Danger";

                content =
                    $"Bạn đã vượt kế hoạch chi tiêu {spendingPlan.Category?.Name}. " +
                    $"Đã chi {spent:N0} ₫ / {spendingPlan.PlannedAmount:N0} ₫.";
            }
            else if (percent >= 80)
            {
                notificationType = "Warning";

                content =
                    $"Bạn đã sử dụng {percent:N0}% kế hoạch chi tiêu " +
                    $"{spendingPlan.Category?.Name}. " +
                    $"Đã chi {spent:N0} ₫ / {spendingPlan.PlannedAmount:N0} ₫.";
            }

            if (content == null)
                return;

            // Không tạo lại cùng một loại cảnh báo
            // cho danh mục trong cùng tháng
            var keyword = notificationType == "Danger"
                ? $"vượt kế hoạch chi tiêu {spendingPlan.Category?.Name}"
                : $"kế hoạch chi tiêu {spendingPlan.Category?.Name}";

            var alreadyExists = await _context.Notifications
                .AnyAsync(n =>
                    n.UserId == userId &&
                    n.Type == notificationType &&
                    n.CreatedAt >= monthStart &&
                    n.CreatedAt < monthEnd &&
                    n.Content.Contains(keyword));

            if (alreadyExists)
                return;

            var notification = new Notification
            {
                UserId = userId,
                Content = content,
                Type = notificationType,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
        }
    }
}