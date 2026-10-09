-- KIỂM TRA TIỀN TIẾT KIỆM THÁNG HIỆN TẠI
-- Chạy SELECT trước, KHÔNG tự động xóa dữ liệu.

SELECT
    Id,
    SavingsGoalId,
    UserId,
    Amount,
    ContributionDate,
    CreatedAt
FROM SavingsContributions
WHERE YEAR(ContributionDate) = YEAR(GETDATE())
  AND MONTH(ContributionDate) = MONTH(GETDATE())
ORDER BY ContributionDate DESC, Id DESC;

SELECT
    UserId,
    SUM(Amount) AS TotalSavingsThisMonth
FROM SavingsContributions
WHERE YEAR(ContributionDate) = YEAR(GETDATE())
  AND MONTH(ContributionDate) = MONTH(GETDATE())
GROUP BY UserId;

-- Công thức chuẩn của app:
-- SoDuTruocTietKiem = TongThu - TongChi
-- CoTheTietKiemThem = TongThu - TongChi - TietKiemDaGui
-- ConLai = TongThu - TongChi - TietKiemDaGui
--
-- Expense KHÔNG bị chặn khi số dư âm.
-- SpendingPlan KHÔNG tham gia các công thức trên.
