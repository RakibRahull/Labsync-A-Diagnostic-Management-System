using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Identity;

namespace LabSync
{
    public static class DbSeeder
    {
        public static async Task SeedPasswords(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var hasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<User>>();

            var users = new[]
            {
                new { Id = 1, Password = "Admin@123" },
                new { Id = 2, Password = "Reception@123" },
                new { Id = 3, Password = "Tech@123" }
            };

            foreach (var item in users)
            {
                var user = await context.Users.FindAsync(item.Id);

                if (user != null)
                {
                    user.PasswordHash =
                        hasher.HashPassword(user, item.Password);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
