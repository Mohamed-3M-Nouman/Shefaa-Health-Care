using Bogus;
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
            string[] roles = { "Admin", "Doctor", "Patient" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedDoctorsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // 1. Seed Specializations
            var specializationsList = new List<string> 
            { 
                "باطنة", "أطفال", "عظام", "جلدية", "عيون", 
                "أنف وأذن وحنجرة", "أسنان", "نساء وتوليد", "مخ وأعصاب", "مسالك بولية" 
            };

            var existingSpecs = await context.Specializations.Select(s => s.Name).ToListAsync();
            var specsToAdd = specializationsList
                .Where(s => !existingSpecs.Contains(s))
                .Select(s => new Specialization { Name = s })
                .ToList();

            if (specsToAdd.Any())
            {
                await context.Specializations.AddRangeAsync(specsToAdd);
                await context.SaveChangesAsync();
            }

            var allSpecializations = await context.Specializations.ToListAsync();
            if (!allSpecializations.Any()) return;

            // 2. Seed Doctors using Bogus
            var existingDoctorsCount = await context.Doctors.CountAsync();
            if (existingDoctorsCount >= 50) return; // Prevent duplicating seeds

            var faker = new Faker("ar");

            for (int i = 0; i < 50; i++)
            {
                var email = faker.Internet.Email();
                if (await userManager.FindByEmailAsync(email) != null) continue;

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    PhoneNumber = faker.Phone.PhoneNumber("010########"),
                    UserType = "Doctor",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "Doctor@123");
                if (result.Succeeded)
                {
                    // Attempt to add role (Requires roles to be seeded first)
                    try { await userManager.AddToRoleAsync(user, "Doctor"); } catch { }

                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        FullName = $"د. {faker.Name.FullName()}",
                        SpecializationId = faker.PickRandom(allSpecializations).Id,
                        ConsultationFee = faker.Random.Number(10, 50) * 10, // 100–500 EGP, rounded to nearest 10
                        Bio = faker.Lorem.Paragraph(),
                        IsVerified = true,
                        City = faker.Address.City(),
                        ClinicAddress = faker.Address.StreetAddress(),
                        ExperienceYears = faker.Random.Number(5, 30),
                        Rating = Math.Round(3.5m + (decimal)(faker.Random.Double() * 1.5), 1),
                        ReviewCount = faker.Random.Number(10, 500)
                    };

                    context.Doctors.Add(doctor);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
