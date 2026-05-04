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

        // ══════════════════════════════════════════
        //  TEST USERS (Development Only)
        // ══════════════════════════════════════════

        public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // ─── Test Doctor ───
            await SeedTestDoctorAsync(userManager, context);

            // ─── Test Patient ───
            await SeedTestPatientAsync(userManager, context);
        }

        private static async Task SeedTestDoctorAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            const string doctorEmail = "doctor@shefaa.com";
            const string doctorPassword = "Doctor@123";

            // تجنب التكرار - إذا موجود بالفعل لا تنشئه
            if (await userManager.FindByEmailAsync(doctorEmail) != null)
                return;

            // 1. إنشاء حساب Identity
            var doctorUser = new ApplicationUser
            {
                UserName = doctorEmail,
                Email = doctorEmail,
                EmailConfirmed = true,
                PhoneNumber = "01012345678",
                UserType = "Doctor",
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(doctorUser, doctorPassword);
            if (!result.Succeeded) return;

            await userManager.AddToRoleAsync(doctorUser, "Doctor");

            // 2. إيجاد تخصص "قلب"
            var cardioSpec = await context.Specializations
                .FirstOrDefaultAsync(s => s.Name == "قلب");

            if (cardioSpec == null) return;

            // 3. إنشاء سجل الطبيب (Verified = true للاختبار)
            var doctor = new Doctor
            {
                UserId = doctorUser.Id,
                FullName = "د. أحمد محمد",
                SpecializationId = cardioSpec.Id,
                ConsultationFee = 250.00m,
                Bio = "استشاري أمراض القلب والأوعية الدموية - خبرة +15 سنة في جراحات القلب المفتوح والقسطرة التشخيصية والعلاجية.",
                IsVerified = true,
                SyndicateIdCardPath = null,
                CertificatePath = null
            };

            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();

            // 4. إنشاء جدول مواعيد (الأحد - الاثنين - الأربعاء)
            var schedules = new List<DoctorSchedule>
            {
                new()
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = 0, // الأحد
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    SlotDurationMinutes = 30
                },
                new()
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = 1, // الاثنين
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    SlotDurationMinutes = 30
                },
                new()
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = 3, // الأربعاء
                    StartTime = new TimeSpan(13, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    SlotDurationMinutes = 20
                }
            };

            context.DoctorSchedules.AddRange(schedules);
            await context.SaveChangesAsync();
        }

        private static async Task SeedTestPatientAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            const string patientEmail = "patient@shefaa.com";
            const string patientPassword = "Patient@123";

            // تجنب التكرار
            if (await userManager.FindByEmailAsync(patientEmail) != null)
                return;

            // 1. إنشاء حساب Identity
            var patientUser = new ApplicationUser
            {
                UserName = patientEmail,
                Email = patientEmail,
                EmailConfirmed = true,
                PhoneNumber = "01098765432",
                UserType = "Patient",
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(patientUser, patientPassword);
            if (!result.Succeeded) return;

            await userManager.AddToRoleAsync(patientUser, "Patient");

            // 2. إنشاء سجل المريض
            var patient = new Patient
            {
                UserId = patientUser.Id,
                FullName = "محمد علي حسن",
                DateOfBirth = new DateTime(1995, 3, 15),
                Gender = "ذكر",
                BloodType = "A+"
            };

            context.Patients.Add(patient);
            await context.SaveChangesAsync();

            // 3. إنشاء الملف الطبي
            var medicalProfile = new PatientMedicalProfile
            {
                PatientId = patient.Id,
                ChronicDiseases = "ضغط دم مرتفع",
                Allergies = "حساسية من البنسلين",
                FamilyHistory = "تاريخ عائلي لأمراض القلب"
            };

            context.PatientMedicalProfiles.Add(medicalProfile);
            await context.SaveChangesAsync();
        }
    }
}
