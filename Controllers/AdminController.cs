using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Data;
using MoneyTracker.Models;
using MoneyTracker.ViewModels;

namespace MoneyTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =====================================================
        // ADMIN DASHBOARD
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var startOfMonth =
                new DateTime(
                    now.Year,
                    now.Month,
                    1);

            var users =
                await _userManager.Users
                    .AsNoTracking()
                    .ToListAsync();


            // ==============================
            // KPI
            // ==============================

            var totalUsers =
                users.Count;

            var newUsersThisMonth =
                users.Count(u =>
                    u.CreatedAt >= startOfMonth);


            var lockedUsers =
                users.Count(u =>
                    u.LockoutEnd.HasValue &&
                    u.LockoutEnd.Value >
                    DateTimeOffset.Now);


            var activeUsers =
                totalUsers - lockedUsers;


            var totalTransactions =
                await _context.Transactions
                    .CountAsync();


            var totalCategories =
                await _context.Categories
                    .CountAsync();


            var totalSpendingPlans =
                await _context.SpendingPlans
                    .CountAsync();


            // ==============================
            // ROLE
            // ==============================

            var adminCount = 0;
            var normalUserCount = 0;

            foreach (var user in users)
            {
                var roles =
                    await _userManager
                        .GetRolesAsync(user);

                if (roles.Contains("Admin"))
                {
                    adminCount++;
                }
                else
                {
                    normalUserCount++;
                }
            }


            // ==============================
            // CATEGORY
            // ==============================

            var incomeCategoryCount =
                await _context.Categories
                    .CountAsync(c =>
                        c.Type == "Income");


            var expenseCategoryCount =
                await _context.Categories
                    .CountAsync(c =>
                        c.Type == "Expense");


            // ==============================
            // BIỂU ĐỒ 6 THÁNG
            // ==============================

            var monthLabels =
                new List<string>();

            var newUserValues =
                new List<int>();

            var transactionValues =
                new List<int>();


            for (var i = 5; i >= 0; i--)
            {
                var month =
                    new DateTime(
                        now.Year,
                        now.Month,
                        1)
                    .AddMonths(-i);


                var nextMonth =
                    month.AddMonths(1);


                monthLabels.Add(
                    month.ToString("MM/yyyy"));


                var userCount =
                    users.Count(u =>
                        u.CreatedAt >= month &&
                        u.CreatedAt < nextMonth);


                newUserValues.Add(
                    userCount);


                var transactionCount =
                    await _context.Transactions
                        .CountAsync(t =>
                            t.CreatedAt >= month &&
                            t.CreatedAt < nextMonth);


                transactionValues.Add(
                    transactionCount);
            }


            // ==============================
            // USER ĐĂNG KÝ GẦN ĐÂY
            // ==============================

            var recentUsers =
                users
                    .OrderByDescending(u =>
                        u.CreatedAt)
                    .Take(5)
                    .ToList();


            // ==============================
            // VIEW MODEL
            // ==============================

            var model =
                new AdminDashboardViewModel
                {
                    TotalUsers =
                        totalUsers,

                    NewUsersThisMonth =
                        newUsersThisMonth,

                    ActiveUsers =
                        activeUsers,

                    LockedUsers =
                        lockedUsers,

                    TotalTransactions =
                        totalTransactions,

                    TotalCategories =
                        totalCategories,

                    TotalSpendingPlans =
                        totalSpendingPlans,

                    AdminCount =
                        adminCount,

                    UserCount =
                        normalUserCount,

                    IncomeCategoryCount =
                        incomeCategoryCount,

                    ExpenseCategoryCount =
                        expenseCategoryCount,

                    MonthLabels =
                        monthLabels,

                    NewUserValues =
                        newUserValues,

                    TransactionValues =
                        transactionValues,

                    RecentUsers =
                        recentUsers
                };


            return View(model);
        }


        // =====================================================
        // DANH SÁCH USER
        // =====================================================
        public async Task<IActionResult> Users(
            string? search)
        {
            var currentUserId =
                _userManager.GetUserId(User);


            var query =
                _userManager.Users
                    .AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                search =
                    search.Trim();


                query =
                    query.Where(u =>
                        u.FullName.Contains(search) ||

                        (u.Email != null &&
                         u.Email.Contains(search)) ||

                        (u.PhoneNumber != null &&
                         u.PhoneNumber.Contains(search)));
            }


            var users =
                await query
                    .OrderByDescending(u =>
                        u.CreatedAt)
                    .ToListAsync();


            var result =
                new List<AdminUserViewModel>();


            foreach (var user in users)
            {
                var roles =
                    await _userManager
                        .GetRolesAsync(user);


                result.Add(
                    new AdminUserViewModel
                    {
                        Id =
                            user.Id,

                        FullName =
                            user.FullName,

                        Email =
                            user.Email ?? "",

                        PhoneNumber =
                            user.PhoneNumber,

                        CreatedAt =
                            user.CreatedAt,

                        IsLocked =
                            user.LockoutEnd.HasValue &&
                            user.LockoutEnd.Value >
                            DateTimeOffset.Now,

                        Role =
                            roles.FirstOrDefault()
                            ?? "User"
                    });
            }


            ViewBag.Search =
                search;

            ViewBag.CurrentUserId =
                currentUserId;


            return View(result);
        }


        // =====================================================
        // KHÓA / MỞ KHÓA
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(
            string id)
        {
            var currentUserId =
                _userManager.GetUserId(User);


            if (id == currentUserId)
            {
                TempData["Error"] =
                    "Bạn không thể khóa chính tài khoản Admin đang đăng nhập.";

                return RedirectToAction(
                    nameof(Users));
            }


            var user =
                await _userManager
                    .FindByIdAsync(id);


            if (user == null)
            {
                return NotFound();
            }


            var roles =
                await _userManager
                    .GetRolesAsync(user);


            if (roles.Contains("Admin"))
            {
                TempData["Error"] =
                    "Không thể khóa tài khoản Admin.";

                return RedirectToAction(
                    nameof(Users));
            }


            var isLocked =
                user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value >
                DateTimeOffset.Now;


            if (isLocked)
            {
                user.LockoutEnd =
                    null;

                TempData["Success"] =
                    "Đã mở khóa tài khoản.";
            }
            else
            {
                user.LockoutEnabled =
                    true;

                user.LockoutEnd =
                    DateTimeOffset.MaxValue;

                TempData["Success"] =
                    "Đã khóa tài khoản.";
            }


            await _userManager
                .UpdateAsync(user);


            return RedirectToAction(
                nameof(Users));
        }


        // =====================================================
        // CHI TIẾT USER
        // =====================================================
        public async Task<IActionResult> UserDetails(
            string id)
        {
            var user =
                await _userManager
                    .FindByIdAsync(id);


            if (user == null)
            {
                return NotFound();
            }


            var roles =
                await _userManager
                    .GetRolesAsync(user);


            var transactionCount =
                await _context.Transactions
                    .CountAsync(t =>
                        t.UserId == user.Id);


            var spendingPlanCount =
                await _context.SpendingPlans
                    .CountAsync(b =>
                        b.UserId == user.Id);


            var categoryCount =
                await _context.Categories
                    .CountAsync(c =>
                        c.UserId == user.Id);


            ViewBag.Role =
                roles.FirstOrDefault()
                ?? "User";


            ViewBag.TransactionCount =
                transactionCount;

            ViewBag.SpendingPlanCount =
                spendingPlanCount;

            ViewBag.CategoryCount =
                categoryCount;


            return View(user);
        }
    }
}