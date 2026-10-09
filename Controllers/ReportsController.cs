using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Data;
using MoneyTracker.Models;
using MoneyTracker.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MoneyTracker.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReportsController(
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =====================================================
        // XEM BÁO CÁO
        // =====================================================

        public async Task<IActionResult> Index(
            string period = "month",
            DateTime? date = null)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var selectedDate = date ?? DateTime.Today;

            var (fromDate, toDate, normalizedPeriod) =
                GetDateRange(period, selectedDate);

            period = normalizedPeriod;


            // =================================================
            // GIAO DỊCH
            // =================================================

            var transactions =
                await GetTransactions(
                    userId,
                    fromDate,
                    toDate);


            var totalIncome =
                transactions
                    .Where(t => t.Type == "Income")
                    .Sum(t => t.Amount);


            var totalExpense =
                transactions
                    .Where(t => t.Type == "Expense")
                    .Sum(t => t.Amount);


            var balance =
                totalIncome - totalExpense;


            // =================================================
            // BIỂU ĐỒ THU - CHI
            // =================================================

            var chartLabels =
                new List<string>();

            var incomeValues =
                new List<decimal>();

            var expenseValues =
                new List<decimal>();


            if (period == "day")
            {
                chartLabels.Add(
                    fromDate.ToString("dd/MM"));

                incomeValues.Add(totalIncome);

                expenseValues.Add(totalExpense);
            }

            else if (period == "week")
            {
                for (var i = 0; i < 7; i++)
                {
                    var currentDate =
                        fromDate.AddDays(i);

                    var nextDate =
                        currentDate.AddDays(1);


                    chartLabels.Add(
                        currentDate.ToString("dd/MM"));


                    var income =
                        transactions
                            .Where(t =>
                                t.Type == "Income" &&
                                t.TransactionDate >= currentDate &&
                                t.TransactionDate < nextDate)
                            .Sum(t => t.Amount);


                    var expense =
                        transactions
                            .Where(t =>
                                t.Type == "Expense" &&
                                t.TransactionDate >= currentDate &&
                                t.TransactionDate < nextDate)
                            .Sum(t => t.Amount);


                    incomeValues.Add(income);

                    expenseValues.Add(expense);
                }
            }

            else
            {
                var days =
                    DateTime.DaysInMonth(
                        fromDate.Year,
                        fromDate.Month);


                for (var i = 1; i <= days; i++)
                {
                    var currentDate =
                        new DateTime(
                            fromDate.Year,
                            fromDate.Month,
                            i);

                    var nextDate =
                        currentDate.AddDays(1);


                    chartLabels.Add(
                        i.ToString());


                    var income =
                        transactions
                            .Where(t =>
                                t.Type == "Income" &&
                                t.TransactionDate >= currentDate &&
                                t.TransactionDate < nextDate)
                            .Sum(t => t.Amount);


                    var expense =
                        transactions
                            .Where(t =>
                                t.Type == "Expense" &&
                                t.TransactionDate >= currentDate &&
                                t.TransactionDate < nextDate)
                            .Sum(t => t.Amount);


                    incomeValues.Add(income);

                    expenseValues.Add(expense);
                }
            }


            // =================================================
            // CHI THEO DANH MỤC
            // =================================================

            var expenseByCategory =
                transactions
                    .Where(t =>
                        t.Type == "Expense" &&
                        t.Category != null)
                    .GroupBy(t => t.Category!.Name)
                    .Select(g => new
                    {
                        Name = g.Key,
                        Total = g.Sum(t => t.Amount)
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();


            // =================================================
            // SPENDING PLAN
            //
            // SpendingPlan là kế hoạch phân bổ.
            // Không trừ SpendingPlan khỏi Expense.
            // Actual Expense được so sánh với SpendingPlan.
            // =================================================

            var spendingPlanItems =
                new List<SpendingPlanReportItemViewModel>();


            // SpendingPlan hiện tại của project được lưu theo tháng/năm,
            // vì vậy với report ngày/tuần ta lấy các spendingPlan thuộc
            // những tháng mà khoảng báo cáo đi qua.

            var lastReportDate =
                toDate.AddTicks(-1);


            var spendingPlans =
                await _context.SpendingPlans
                    .Include(b => b.Category)
                    .Where(b =>
                        b.UserId == userId)
                    .ToListAsync();


            var relevantSpendingPlans =
                spendingPlans
                    .Where(b =>
                    {
                        var planMonthStart =
                            new DateTime(
                                b.Year,
                                b.Month,
                                1);

                        var planMonthEnd =
                            planMonthStart.AddMonths(1);

                        return
                            planMonthStart <= lastReportDate &&
                            planMonthEnd > fromDate;
                    })
                    .ToList();


            foreach (var spendingPlan in relevantSpendingPlans)
            {
                var planMonthStart =
                    new DateTime(
                        spendingPlan.Year,
                        spendingPlan.Month,
                        1);

                var planMonthEnd =
                    planMonthStart.AddMonths(1);


                // Chỉ lấy phần giao nhau với kỳ báo cáo.
                // Với report tháng -> toàn bộ tháng.
                // Với ngày/tuần -> chi thực tế chỉ trong ngày/tuần.

                var expenseStart =
                    fromDate > planMonthStart
                        ? fromDate
                        : planMonthStart;

                var expenseEnd =
                    toDate < planMonthEnd
                        ? toDate
                        : planMonthEnd;


                var actualExpense =
                    await _context.Transactions
                        .Where(t =>
                            t.UserId == userId &&
                            t.Type == "Expense" &&
                            t.CategoryId == spendingPlan.CategoryId &&
                            t.TransactionDate >= expenseStart &&
                            t.TransactionDate < expenseEnd)
                        .SumAsync(t => (decimal?)t.Amount)
                    ?? 0;


                var variance =
                    spendingPlan.PlannedAmount -
                    actualExpense;


                var usagePercent =
                    spendingPlan.PlannedAmount > 0
                        ? actualExpense /
                          spendingPlan.PlannedAmount *
                          100
                        : 0;


                spendingPlanItems.Add(
                    new SpendingPlanReportItemViewModel
                    {
                        CategoryId =
                            spendingPlan.CategoryId,

                        CategoryName =
                            spendingPlan.Category?.Name
                            ?? "Không xác định",

                        PlannedAmount =
                            spendingPlan.PlannedAmount,

                        ActualExpense =
                            actualExpense,

                        Variance =
                            variance,

                        UsagePercent =
                            Math.Round(
                                usagePercent,
                                1)
                    });
            }


            var totalSpendingPlan =
                spendingPlanItems.Sum(
                    x => x.PlannedAmount);


            var spendingPlanExpense =
                spendingPlanItems.Sum(
                    x => x.ActualExpense);


            var spendingPlanVariance =
                totalSpendingPlan -
                spendingPlanExpense;


            var spendingPlanUsagePercent =
                totalSpendingPlan > 0
                    ? spendingPlanExpense /
                      totalSpendingPlan *
                      100
                    : 0;


            // =================================================
            // SAVINGS TRONG KỲ
            // =================================================

            var periodSavings =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId &&
                        s.ContributionDate >= fromDate &&
                        s.ContributionDate < toDate)
                    .SumAsync(s => (decimal?)s.Amount)
                ?? 0;

            // Tiết kiệm được lấy từ số dư, không phải Expense.
            // Số còn lại = Thu - Chi thực tế - Tiết kiệm đã chuyển khỏi số dư.
            balance = totalIncome - totalExpense - periodSavings;

            // Còn lại thực tế = Thu - Chi - Tiết kiệm trong kỳ.


            // =================================================
            // TỔNG SAVINGS TỪ TRƯỚC TỚI NAY
            // =================================================

            var totalSavings =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId)
                    .SumAsync(s => (decimal?)s.Amount)
                ?? 0;


            // =================================================
            // SAVINGS THEO THÁNG
            // 12 THÁNG GẦN NHẤT
            // =================================================

            var savingsStart =
                new DateTime(
                    selectedDate.Year,
                    selectedDate.Month,
                    1)
                .AddMonths(-11);


            var savingsEnd =
                new DateTime(
                    selectedDate.Year,
                    selectedDate.Month,
                    1)
                .AddMonths(1);


            var savingsData =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId &&
                        s.ContributionDate >= savingsStart &&
                        s.ContributionDate < savingsEnd)
                    .GroupBy(s => new
                    {
                        s.ContributionDate.Year,
                        s.ContributionDate.Month
                    })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Amount =
                            g.Sum(x => x.Amount)
                    })
                    .ToListAsync();


            var savingsMonthlyItems =
                new List<SavingsMonthlyItemViewModel>();


            for (var i = 0; i < 12; i++)
            {
                var month =
                    savingsStart.AddMonths(i);


                var amount =
                    savingsData
                        .Where(x =>
                            x.Year == month.Year &&
                            x.Month == month.Month)
                        .Select(x => x.Amount)
                        .FirstOrDefault();


                savingsMonthlyItems.Add(
                    new SavingsMonthlyItemViewModel
                    {
                        Month =
                            month.Month,

                        Year =
                            month.Year,

                        Amount =
                            amount
                    });
            }


            // =================================================
            // VIEW MODEL
            // =================================================

            var model =
                new ReportViewModel
                {
                    Period =
                        period,

                    FromDate =
                        fromDate,

                    ToDate =
                        toDate.AddTicks(-1),

                    TotalIncome =
                        totalIncome,

                    TotalExpense =
                        totalExpense,

                    Balance =
                        balance,

                    TotalSpendingPlan =
                        totalSpendingPlan,

                    SpendingPlanExpense =
                        spendingPlanExpense,

                    SpendingPlanVariance =
                        spendingPlanVariance,

                    SpendingPlanUsagePercent =
                        Math.Round(
                            spendingPlanUsagePercent,
                            1),

                    PeriodSavings =
                        periodSavings,


                    TotalSavings =
                        totalSavings,

                    Transactions =
                        transactions,

                    ChartLabels =
                        chartLabels,

                    IncomeValues =
                        incomeValues,

                    ExpenseValues =
                        expenseValues,

                    CategoryLabels =
                        expenseByCategory
                            .Select(x => x.Name)
                            .ToList(),

                    CategoryValues =
                        expenseByCategory
                            .Select(x => x.Total)
                            .ToList(),

                    SpendingPlanItems =
                        spendingPlanItems
                            .OrderByDescending(
                                x => x.PlannedAmount)
                            .ToList(),

                    SavingsMonthlyItems =
                        savingsMonthlyItems
                };


            return View(model);
        }


        // =====================================================
        // EXCEL
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportExcel(
            string period = "month",
            DateTime? date = null)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();


            var selectedDate =
                date ?? DateTime.Today;


            var (fromDate, toDate, normalizedPeriod) =
                GetDateRange(
                    period,
                    selectedDate);

            period =
                normalizedPeriod;


            var transactions =
                await GetTransactions(
                    userId,
                    fromDate,
                    toDate);


            var totalIncome =
                transactions
                    .Where(t => t.Type == "Income")
                    .Sum(t => t.Amount);


            var totalExpense =
                transactions
                    .Where(t => t.Type == "Expense")
                    .Sum(t => t.Amount);


            var balance =
                totalIncome -
                totalExpense;


            var periodSavings =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId &&
                        s.ContributionDate >= fromDate &&
                        s.ContributionDate < toDate)
                    .SumAsync(s => (decimal?)s.Amount)
                ?? 0;

            // Tiết kiệm được lấy từ số dư, không phải Expense.
            // Số còn lại = Thu - Chi thực tế - Tiết kiệm đã chuyển khỏi số dư.
            balance = totalIncome - totalExpense - periodSavings;

            // Còn lại thực tế = Thu - Chi - Tiết kiệm trong kỳ.


            var totalSavings =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId)
                    .SumAsync(s => (decimal?)s.Amount)
                ?? 0;


            var spendingPlanReport =
                await GetSpendingPlanReport(
                    userId,
                    fromDate,
                    toDate);


            using var workbook =
                new XLWorkbook();


            // =================================================
            // SHEET 1 - TỔNG QUAN
            // =================================================

            var worksheet =
                workbook.Worksheets.Add(
                    "BaoCao");


            worksheet.Cell("A1").Value =
                "BÁO CÁO TÀI CHÍNH";

            worksheet.Range("A1:F1")
                .Merge();


            worksheet.Cell("A1")
                .Style.Font.Bold =
                true;

            worksheet.Cell("A1")
                .Style.Font.FontSize =
                18;

            worksheet.Cell("A1")
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;


            worksheet.Cell("A2").Value =
                $"Từ {fromDate:dd/MM/yyyy} " +
                $"đến {toDate.AddTicks(-1):dd/MM/yyyy}";

            worksheet.Range("A2:F2")
                .Merge();

            worksheet.Cell("A2")
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;


            worksheet.Cell("A4").Value =
                "Tổng thu";

            worksheet.Cell("B4").Value =
                totalIncome;


            worksheet.Cell("C4").Value =
                "Tổng chi";

            worksheet.Cell("D4").Value =
                totalExpense;


            worksheet.Cell("E4").Value =
                "Chênh lệch";

            worksheet.Cell("F4").Value =
                balance;


            worksheet.Cell("A5").Value =
                "Tiết kiệm trong kỳ";

            worksheet.Cell("B5").Value =
                periodSavings;


            worksheet.Cell("C5").Value =
                "Tổng tiết kiệm";

            worksheet.Cell("D5").Value =
                totalSavings;


            worksheet.Cell("E5").Value =
                "Dư/Vượt thực tế sau tiết kiệm";

            worksheet.Cell("F5").Value =
                spendingPlanReport.Sum(x => x.Variance) - periodSavings;


            foreach (var cell in new[]
            {
                "B4",
                "D4",
                "F4",
                "B5",
                "D5",
                "F5"
            })
            {
                worksheet.Cell(cell)
                    .Style.NumberFormat.Format =
                    "#,##0 \"₫\"";
            }


            // =================================================
            // GIAO DỊCH
            // =================================================

            var headerRow = 7;


            worksheet.Cell(
                headerRow,
                1).Value = "STT";

            worksheet.Cell(
                headerRow,
                2).Value = "Giao dịch";

            worksheet.Cell(
                headerRow,
                3).Value = "Danh mục";

            worksheet.Cell(
                headerRow,
                4).Value = "Ngày giờ";

            worksheet.Cell(
                headerRow,
                5).Value = "Loại";

            worksheet.Cell(
                headerRow,
                6).Value = "Số tiền";


            var headerRange =
                worksheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    6);


            headerRange.Style.Font.Bold =
                true;


            var row =
                headerRow + 1;

            var stt = 1;


            foreach (var item in transactions)
            {
                worksheet.Cell(row, 1)
                    .Value = stt;

                worksheet.Cell(row, 2)
                    .Value = item.Title;

                worksheet.Cell(row, 3)
                    .Value =
                    item.Category?.Name ?? "";

                worksheet.Cell(row, 4)
                    .Value =
                    item.TransactionDate;

                worksheet.Cell(row, 4)
                    .Style.DateFormat.Format =
                    "dd/MM/yyyy HH:mm";

                worksheet.Cell(row, 5)
                    .Value =
                    item.Type == "Income"
                        ? "Thu nhập"
                        : "Chi tiêu";

                worksheet.Cell(row, 6)
                    .Value =
                    item.Amount;

                worksheet.Cell(row, 6)
                    .Style.NumberFormat.Format =
                    "#,##0 \"₫\"";


                row++;
                stt++;
            }


            // =================================================
            // SHEET SPENDING PLAN
            // =================================================

            var spendingPlanSheet =
                workbook.Worksheets.Add(
                    "KeHoachChiTieu");


            spendingPlanSheet.Cell("A1").Value =
                "BÁO CÁO NGÂN SÁCH";

            spendingPlanSheet.Range("A1:E1")
                .Merge();

            spendingPlanSheet.Cell("A1")
                .Style.Font.Bold =
                true;

            spendingPlanSheet.Cell("A1")
                .Style.Font.FontSize =
                16;


            spendingPlanSheet.Cell("A3").Value =
                "Danh mục";

            spendingPlanSheet.Cell("B3").Value =
                "Kế hoạch";

            spendingPlanSheet.Cell("C3").Value =
                "Thực chi";

            spendingPlanSheet.Cell("D3").Value =
                "Dư / Vượt";

            spendingPlanSheet.Cell("E3").Value =
                "% sử dụng";


            spendingPlanSheet.Range("A3:E3")
                .Style.Font.Bold =
                true;


            var spendingPlanRow = 4;


            foreach (var item in spendingPlanReport)
            {
                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    1).Value =
                    item.CategoryName;

                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    2).Value =
                    item.PlannedAmount;

                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    3).Value =
                    item.ActualExpense;

                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    4).Value =
                    item.Variance;

                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    5).Value =
                    item.UsagePercent / 100;


                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    2)
                    .Style.NumberFormat.Format =
                    "#,##0 \"₫\"";

                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    3)
                    .Style.NumberFormat.Format =
                    "#,##0 \"₫\"";

                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    4)
                    .Style.NumberFormat.Format =
                    "#,##0 \"₫\"";

                spendingPlanSheet.Cell(
                    spendingPlanRow,
                    5)
                    .Style.NumberFormat.Format =
                    "0.0%";


                spendingPlanRow++;
            }


            worksheet.Columns()
                .AdjustToContents();

            spendingPlanSheet.Columns()
                .AdjustToContents();


            using var stream =
                new MemoryStream();


            workbook.SaveAs(stream);


            var fileName =
                $"BaoCao_{period}_{selectedDate:yyyyMMdd}.xlsx";


            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        // =====================================================
        // PDF
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportPdf(
            string period = "month",
            DateTime? date = null)
        {
            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();


            var selectedDate =
                date ?? DateTime.Today;


            var (fromDate, toDate, normalizedPeriod) =
                GetDateRange(
                    period,
                    selectedDate);

            period =
                normalizedPeriod;


            var transactions =
                await GetTransactions(
                    userId,
                    fromDate,
                    toDate);


            var totalIncome =
                transactions
                    .Where(t => t.Type == "Income")
                    .Sum(t => t.Amount);


            var totalExpense =
                transactions
                    .Where(t => t.Type == "Expense")
                    .Sum(t => t.Amount);


            var balance =
                totalIncome -
                totalExpense;


            var periodSavings =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId &&
                        s.ContributionDate >= fromDate &&
                        s.ContributionDate < toDate)
                    .SumAsync(s => (decimal?)s.Amount)
                ?? 0;

            // Tiết kiệm được lấy từ số dư, không phải Expense.
            // Số còn lại = Thu - Chi thực tế - Tiết kiệm đã chuyển khỏi số dư.
            balance = totalIncome - totalExpense - periodSavings;

            // Còn lại thực tế = Thu - Chi - Tiết kiệm trong kỳ.


            var totalSavings =
                await _context.SavingsContributions
                    .Where(s =>
                        s.UserId == userId)
                    .SumAsync(s => (decimal?)s.Amount)
                ?? 0;


            var spendingPlanReport =
                await GetSpendingPlanReport(
                    userId,
                    fromDate,
                    toDate);


            var totalSpendingPlan =
                spendingPlanReport.Sum(
                    x => x.PlannedAmount);


            var spendingPlanExpense =
                spendingPlanReport.Sum(
                    x => x.ActualExpense);


            var spendingPlanVariance =
                totalSpendingPlan -
                spendingPlanExpense -
                periodSavings;


            var pdf =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(
                            PageSizes.A4);

                        page.Margin(30);

                        page.DefaultTextStyle(
                            x => x.FontSize(9));


                        page.Header()
                            .Column(column =>
                            {
                                column.Item()
                                    .AlignCenter()
                                    .Text(
                                        "BÁO CÁO TÀI CHÍNH")
                                    .Bold()
                                    .FontSize(20);

                                column.Item()
                                    .AlignCenter()
                                    .Text(
                                        $"Từ {fromDate:dd/MM/yyyy} " +
                                        $"đến {toDate.AddTicks(-1):dd/MM/yyyy}");
                            });


                        page.Content()
                            .PaddingVertical(20)
                            .Column(column =>
                            {
                                // TỔNG QUAN

                                column.Item()
                                    .PaddingBottom(10)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text(
                                                $"Tổng thu\n{totalIncome:N0} VND")
                                            .Bold();

                                        row.RelativeItem()
                                            .Text(
                                                $"Tổng chi\n{totalExpense:N0} VND")
                                            .Bold();

                                        row.RelativeItem()
                                            .Text(
                                                $"Chênh lệch\n{balance:N0} VND")
                                            .Bold();
                                    });


                                column.Item()
                                    .PaddingBottom(15)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text(
                                                $"Kế hoạch\n{totalSpendingPlan:N0} VND")
                                            .Bold();

                                        row.RelativeItem()
                                            .Text(
                                                $"Tiết kiệm kỳ\n{periodSavings:N0} VND")
                                            .Bold();

                                        row.RelativeItem()
                                            .Text(
                                                $"Tổng tiết kiệm\n{totalSavings:N0} VND")
                                            .Bold();
                                    });


                                // SPENDING PLAN

                                column.Item()
                                    .PaddingBottom(6)
                                    .Text(
                                        "NGÂN SÁCH")
                                    .Bold()
                                    .FontSize(12);


                                column.Item()
                                    .PaddingBottom(15)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                            });


                                        table.Header(header =>
                                        {
                                            header.Cell()
                                                .Text("Danh mục")
                                                .Bold();

                                            header.Cell()
                                                .AlignRight()
                                                .Text("Kế hoạch")
                                                .Bold();

                                            header.Cell()
                                                .AlignRight()
                                                .Text("Thực chi")
                                                .Bold();

                                            header.Cell()
                                                .AlignRight()
                                                .Text("Dư/Vượt")
                                                .Bold();
                                        });


                                        foreach (var item in spendingPlanReport)
                                        {
                                            table.Cell()
                                                .Text(
                                                    item.CategoryName);

                                            table.Cell()
                                                .AlignRight()
                                                .Text(
                                                    $"{item.PlannedAmount:N0}");

                                            table.Cell()
                                                .AlignRight()
                                                .Text(
                                                    $"{item.ActualExpense:N0}");

                                            table.Cell()
                                                .AlignRight()
                                                .Text(
                                                    $"{item.Variance:N0}");
                                        }
                                    });


                                column.Item()
                                    .PaddingBottom(6)
                                    .Text(
                                        "GIAO DỊCH")
                                    .Bold()
                                    .FontSize(12);


                                column.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns.ConstantColumn(25);
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn(1.4f);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn(1.3f);
                                            });


                                        table.Header(header =>
                                        {
                                            header.Cell()
                                                .Text("STT")
                                                .Bold();

                                            header.Cell()
                                                .Text("Giao dịch")
                                                .Bold();

                                            header.Cell()
                                                .Text("Danh mục")
                                                .Bold();

                                            header.Cell()
                                                .Text("Ngày giờ")
                                                .Bold();

                                            header.Cell()
                                                .Text("Loại")
                                                .Bold();

                                            header.Cell()
                                                .AlignRight()
                                                .Text("Số tiền")
                                                .Bold();
                                        });


                                        var index = 1;


                                        foreach (var item in transactions)
                                        {
                                            table.Cell()
                                                .Text(
                                                    index.ToString());

                                            table.Cell()
                                                .Text(
                                                    item.Title);

                                            table.Cell()
                                                .Text(
                                                    item.Category?.Name
                                                    ?? "");

                                            table.Cell()
                                                .Text(
                                                    item.TransactionDate
                                                        .ToString(
                                                            "dd/MM/yyyy HH:mm"));

                                            table.Cell()
                                                .Text(
                                                    item.Type == "Income"
                                                        ? "Thu"
                                                        : "Chi");

                                            table.Cell()
                                                .AlignRight()
                                                .Text(
                                                    $"{item.Amount:N0}");

                                            index++;
                                        }
                                    });


                                column.Item()
                                    .PaddingTop(12)
                                    .Text(
                                        $"Tổng kế hoạch: {totalSpendingPlan:N0} VND | " +
                                        $"Thực chi: {spendingPlanExpense:N0} VND | " +
                                        $"Dư/Vượt sau tiết kiệm: {spendingPlanVariance:N0} VND");
                            });


                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Trang ");

                                text.CurrentPageNumber();

                                text.Span(" / ");

                                text.TotalPages();
                            });
                    });
                });


            var pdfBytes =
                pdf.GeneratePdf();


            var fileName =
                $"BaoCao_{period}_{selectedDate:yyyyMMdd}.pdf";


            return File(
                pdfBytes,
                "application/pdf",
                fileName);
        }


        // =====================================================
        // SPENDING PLAN REPORT HELPER
        // =====================================================

        private async Task<List<SpendingPlanReportItemViewModel>>
            GetSpendingPlanReport(
                string userId,
                DateTime fromDate,
                DateTime toDate)
        {
            var lastDate =
                toDate.AddTicks(-1);


            var spendingPlans =
                await _context.SpendingPlans
                    .Include(b => b.Category)
                    .Where(b =>
                        b.UserId == userId)
                    .ToListAsync();


            var relevantSpendingPlans =
                spendingPlans
                    .Where(b =>
                    {
                        var monthStart =
                            new DateTime(
                                b.Year,
                                b.Month,
                                1);

                        var monthEnd =
                            monthStart.AddMonths(1);


                        return
                            monthStart <= lastDate &&
                            monthEnd > fromDate;
                    })
                    .ToList();


            var result =
                new List<SpendingPlanReportItemViewModel>();


            foreach (var spendingPlan in relevantSpendingPlans)
            {
                var monthStart =
                    new DateTime(
                        spendingPlan.Year,
                        spendingPlan.Month,
                        1);

                var monthEnd =
                    monthStart.AddMonths(1);


                var expenseStart =
                    fromDate > monthStart
                        ? fromDate
                        : monthStart;


                var expenseEnd =
                    toDate < monthEnd
                        ? toDate
                        : monthEnd;


                var actualExpense =
                    await _context.Transactions
                        .Where(t =>
                            t.UserId == userId &&
                            t.Type == "Expense" &&
                            t.CategoryId ==
                                spendingPlan.CategoryId &&
                            t.TransactionDate >=
                                expenseStart &&
                            t.TransactionDate <
                                expenseEnd)
                        .SumAsync(
                            t => (decimal?)t.Amount)
                    ?? 0;


                var variance =
                    spendingPlan.PlannedAmount -
                    actualExpense;


                var percent =
                    spendingPlan.PlannedAmount > 0
                        ? actualExpense /
                          spendingPlan.PlannedAmount *
                          100
                        : 0;


                result.Add(
                    new SpendingPlanReportItemViewModel
                    {
                        CategoryId =
                            spendingPlan.CategoryId,

                        CategoryName =
                            spendingPlan.Category?.Name
                            ?? "Không xác định",

                        PlannedAmount =
                            spendingPlan.PlannedAmount,

                        ActualExpense =
                            actualExpense,

                        Variance =
                            variance,

                        UsagePercent =
                            Math.Round(
                                percent,
                                1)
                    });
            }


            return result;
        }


        // =====================================================
        // TRANSACTIONS
        // =====================================================

        private async Task<List<Transaction>>
            GetTransactions(
                string userId,
                DateTime fromDate,
                DateTime toDate)
        {
            return await _context.Transactions
                .Include(t => t.Category)
                .Where(t =>
                    t.UserId == userId &&
                    t.TransactionDate >= fromDate &&
                    t.TransactionDate < toDate)
                .OrderByDescending(
                    t => t.TransactionDate)
                .ThenByDescending(
                    t => t.CreatedAt)
                .ToListAsync();
        }


        // =====================================================
        // DATE RANGE
        // =====================================================

        private static (
            DateTime FromDate,
            DateTime ToDate,
            string Period)
            GetDateRange(
                string period,
                DateTime selectedDate)
        {
            DateTime fromDate;
            DateTime toDate;


            if (period == "day")
            {
                fromDate =
                    selectedDate.Date;

                toDate =
                    fromDate.AddDays(1);
            }

            else if (period == "week")
            {
                var diff =
                    (7 +
                    (selectedDate.DayOfWeek -
                     DayOfWeek.Monday)) % 7;


                fromDate =
                    selectedDate.Date
                        .AddDays(-diff);


                toDate =
                    fromDate.AddDays(7);
            }

            else
            {
                period =
                    "month";


                fromDate =
                    new DateTime(
                        selectedDate.Year,
                        selectedDate.Month,
                        1);


                toDate =
                    fromDate.AddMonths(1);
            }


            return (
                fromDate,
                toDate,
                period);
        }
    }
}
