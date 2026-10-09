using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MoneyTracker.Data;

#nullable disable

namespace MoneyTracker.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260917083000_RenameBudgetToSpendingPlan")]
    public partial class RenameBudgetToSpendingPlan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Budgets",
                newName: "SpendingPlans");

            migrationBuilder.RenameColumn(
                name: "AmountLimit",
                table: "SpendingPlans",
                newName: "PlannedAmount");

            migrationBuilder.RenameIndex(
                name: "IX_Budgets_UserId",
                table: "SpendingPlans",
                newName: "IX_SpendingPlans_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Budgets_CategoryId",
                table: "SpendingPlans",
                newName: "IX_SpendingPlans_CategoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_SpendingPlans_UserId",
                table: "SpendingPlans",
                newName: "IX_Budgets_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_SpendingPlans_CategoryId",
                table: "SpendingPlans",
                newName: "IX_Budgets_CategoryId");

            migrationBuilder.RenameColumn(
                name: "PlannedAmount",
                table: "SpendingPlans",
                newName: "AmountLimit");

            migrationBuilder.RenameTable(
                name: "SpendingPlans",
                newName: "Budgets");
        }
    }
}
