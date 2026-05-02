using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShefaaHealthCare.Models;

namespace ShefaaHealthCare.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = ["Admin", "Doctor", "Patient"];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedSpecializationsAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var specializations = new[]
            {
                "مخ واعصاب",
                "جلدية",
                "نفسية و عصبية",
                "باطنة",
                "نساء و توليد",
                "جراحة",
                "مسالك بولية",
                "انف و اذن و حنجرة",
                "عظام",
                "عيون",
                "اسنان",
                "ذكورة",
                "اشعة و تحاليل",
                "علاج طبيعي",
                "قلب"
            };

            var existingNames = await context.Specializations
                .Select(s => s.Name)
                .ToListAsync();

            var missingSpecializations = specializations
                .Where(name => !existingNames.Contains(name))
                .Select(name => new Specialization { Name = name })
                .ToList();

            if (missingSpecializations.Count > 0)
            {
                await context.Specializations.AddRangeAsync(missingSpecializations);
                await context.SaveChangesAsync();
            }
        }
    }
}
