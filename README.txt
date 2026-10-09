BỘ FILE LOGIC CHUẨN

1) Ngân sách tháng = Tổng Income.
2) Tổng chi thực tế = chỉ Transaction Type = Expense.
3) Số dư trước tiết kiệm = Income - Expense.
4) Tiết kiệm chỉ được thêm nếu không vượt:
   Income - Expense - Savings đã gửi trong tháng.
5) Còn lại = Income - Expense - Savings.
6) Expense luôn được phép thêm, kể cả làm Còn lại âm.
7) SpendingPlan chỉ so sánh Kế hoạch với Expense theo danh mục.

LƯU Ý DỮ LIỆU HIỆN TẠI:
Nếu Dashboard vẫn ra -1.710.000 với Thu 6.050.000 và Chi 6.660.000,
thì DB đang có tổng Savings tháng = 1.100.000.
Đó không phải lỗi phép tính: 6.050.000 - 6.660.000 - 1.100.000 = -1.710.000.

Chạy KIEM_TRA_SAVINGS.sql trong SSMS để xem các SavingsContribution của tháng.
Không xóa tự động vì cần xác định khoản nào là dữ liệu bạn thực sự muốn giữ.
