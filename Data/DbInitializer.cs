using Microsoft.AspNetCore.Identity;
using MoneyTracker.Models;

namespace MoneyTracker.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<AppUser>>();

            var context =
                serviceProvider.GetRequiredService<AppDbContext>();


            // Tạo roll

            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            // Tạo admin mặc định
        

            string adminEmail = "admin@moneytracker.com";
            string adminPassword = "Admin123";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new AppUser
                {
                    FullName = "MoneyTracker Admin",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var result =
                    await userManager.CreateAsync(
                        admin,
                        adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        admin,
                        "Admin");
                }
            }

            // Tạo danh mục mặc định
       

            if (!context.Categories.Any(c => c.IsDefault))
            {
                var categories = new List<Category>
                {
                    // Thu nhập
                    new Category
                    {
                        Name = "Lương",
                        Type = "Income",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Thưởng",
                        Type = "Income",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Freelance",
                        Type = "Income",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Đầu tư",
                        Type = "Income",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Thu nhập khác",
                        Type = "Income",
                        IsDefault = true
                    },

                    // Chi tiêu
                    new Category
                    {
                        Name = "Ăn uống",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Đi lại",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Mua sắm",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Giải trí",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Học tập",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Y tế",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Hóa đơn",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Tiết kiệm",
                        Type = "Expense",
                        IsDefault = true
                    },

                    new Category
                    {
                        Name = "Chi tiêu khác",
                        Type = "Expense",
                        IsDefault = true
                    }
                };

                context.Categories.AddRange(categories);

                await context.SaveChangesAsync();
            }
        }
    }
}