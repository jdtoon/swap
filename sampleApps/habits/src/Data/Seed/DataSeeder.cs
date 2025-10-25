using Microsoft.AspNetCore.Identity;
using habits.Data.Models;

namespace habits.Data.Seed
{
    public static class DataSeeder
    {
        public static async Task Seed(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                string userEmail = "aishasuliman@gmail.com";
                string userEmail2 = "cooljunz29@gmail.com";
                string adminRole = "admin";
                string memberRole = "member";
                string userRole = "user";

                if (!await roleManager.RoleExistsAsync(adminRole))
                {
                    var role = new AppRole { Name = adminRole };
                    var result = await roleManager.CreateAsync(role);
                }
                if (!await roleManager.RoleExistsAsync(memberRole))
                {
                    var role = new AppRole { Name = memberRole };
                    var result = await roleManager.CreateAsync(role);
                }
                if (!await roleManager.RoleExistsAsync(userRole))
                {
                    var role = new AppRole { Name = userRole };
                    var result = await roleManager.CreateAsync(role);
                }

                if (!context.Users.Any(u => u.UserName == userEmail))
                {
                    var user = new AppUser
                    {
                        Email = userEmail,
                        UserName = userEmail,
                        EmailConfirmed = true,
                        Name = "Aisha",
                        Surname = "Suliman",
                        IsActive = true,
                    };

                    var result = await userManager.CreateAsync(user, "P@ssw0rd!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, adminRole);
                    }
                }

                if (!context.Users.Any(u => u.UserName == userEmail2))
                {
                    var user = new AppUser
                    {
                        Email = userEmail2,
                        UserName = userEmail2,
                        EmailConfirmed = true,
                        Name = "Junaid",
                        Surname = "Desai",
                        IsActive = true,
                    };

                    var result = await userManager.CreateAsync(user, "P@ssw0rd!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, adminRole);
                    }
                }
            }
        }
    }
}
