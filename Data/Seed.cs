using Microsoft.AspNetCore.Identity;

namespace webxemphim.Data
{
    public static class Seed
    {
        public static async Task EnsureAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            const string adminRole = "Admin";
            
            // Tạo role Admin nếu chưa có
            if (!await roleMgr.RoleExistsAsync(adminRole))
            {
                await roleMgr.CreateAsync(new IdentityRole(adminRole));
            }

            var adminEmail = "admin@bmovie.local";
            var adminPassword = "Admin@12345";
            
            // Kiểm tra xem đã có admin chưa (bằng cách kiểm tra role)
            var usersInAdminRole = await userMgr.GetUsersInRoleAsync(adminRole);
            
            // Nếu chưa có admin nào, tạo admin mặc định
            if (usersInAdminRole.Count == 0)
            {
                // Kiểm tra xem email đã tồn tại chưa (có thể bị xóa nhưng vẫn còn trong DB)
                var admin = await userMgr.FindByEmailAsync(adminEmail);
                
                if (admin == null)
                {
                    // Tạo admin mới
                    admin = new ApplicationUser 
                    { 
                        UserName = adminEmail, 
                        Email = adminEmail, 
                        EmailConfirmed = true 
                    };
                    var createResult = await userMgr.CreateAsync(admin, adminPassword);
                    if (!createResult.Succeeded)
                    {
                        // Log error nếu có
                        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationUser>>();
                        logger.LogError("Không thể tạo admin: {Errors}", string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    // User đã tồn tại nhưng không có role Admin, thêm role
                    if (!await userMgr.IsInRoleAsync(admin, adminRole))
                    {
                        await userMgr.AddToRoleAsync(admin, adminRole);
                    }
                }
                
                // Đảm bảo admin có role Admin
                if (admin != null && !await userMgr.IsInRoleAsync(admin, adminRole))
                {
                    await userMgr.AddToRoleAsync(admin, adminRole);
                }
            }
            else
            {
                // Đã có admin, đảm bảo admin mặc định cũng có role (nếu tồn tại)
                var admin = await userMgr.FindByEmailAsync(adminEmail);
                if (admin != null && !await userMgr.IsInRoleAsync(admin, adminRole))
                {
                    await userMgr.AddToRoleAsync(admin, adminRole);
                }
            }
        }
    }
}


