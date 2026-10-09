using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoneyTracker.Models;

namespace MoneyTracker.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<SpendingPlan> SpendingPlans { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<SavingsGoal> SavingsGoals { get; set; }

        public DbSet<SavingsContribution> SavingsContributions { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // ==========================================
            // TRANSACTION
            // ==========================================

            builder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // SPENDING PLAN
            // ==========================================

            builder.Entity<SpendingPlan>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SpendingPlan>()
                .HasOne(b => b.Category)
                .WithMany(c => c.SpendingPlans)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // NOTIFICATION
            // ==========================================

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // SAVINGS GOAL
            // ==========================================

            builder.Entity<SavingsGoal>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // SAVINGS CONTRIBUTION
            // ==========================================

            builder.Entity<SavingsContribution>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SavingsContribution>()
                .HasOne(s => s.SavingsGoal)
                .WithMany()
                .HasForeignKey(s => s.SavingsGoalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}