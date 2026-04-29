using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShefaaHealthCare.Data;
using ShefaaHealthCare.Models;
using ShefaaHealthCare.Models.ViewModels;
using ShefaaHealthCare.Repositories.Interfaces;

namespace ShefaaHealthCare.Controllers
{
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IWebHostEnvironment env,
        IUnitOfWork unitOfWork) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;


        // ══════════════════════════════════════════
        //  LOGIN
        // ══════════════════════════════════════════

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            ModelState.AddModelError(string.Empty, "بيانات تسجيل الدخول غير صحيحة.");
            return View(model);
        }

        // ══════════════════════════════════════════
        //  REGISTER
        // ══════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var model = new RegisterViewModel
            {
                // Initialize schedule for 7 days
                Schedules = [.. Enum.GetValues<DayOfWeek>()
                    .Select(d => new ScheduleItemViewModel
                    {
                        DayOfWeek = d,
                        IsSelected = false,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        SlotDurationMinutes = 30
                    })]
            };

            // Populate Specializations dropdown
            ViewBag.Specializations = new SelectList(
                await _context.Specializations.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Re-populate dropdown on validation failure
            ViewBag.Specializations = new SelectList(
                await _context.Specializations.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            // Remove validation for fields not relevant to the selected UserType
            if (model.UserType == "Patient")
            {
                ModelState.Remove("SpecializationId");
                ModelState.Remove("ConsultationFee");
                ModelState.Remove("SyndicateIdCard");
                ModelState.Remove("Certificate");
                // Remove schedule validation
                for (int i = 0; i < 7; i++)
                {
                    ModelState.Remove($"Schedules[{i}].StartTime");
                    ModelState.Remove($"Schedules[{i}].EndTime");
                    ModelState.Remove($"Schedules[{i}].SlotDurationMinutes");
                }
            }
            else if (model.UserType == "Doctor")
            {
                ModelState.Remove("DateOfBirth");
                ModelState.Remove("Gender");
                ModelState.Remove("BloodType");
                ModelState.Remove("ChronicDiseases");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Create the Identity user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                UserType = model.UserType
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            // Assign role
            await _userManager.AddToRoleAsync(user, model.UserType);

            // ── Patient Registration ──
            if (model.UserType == "Patient")
            {
                var patient = new Patient
                {
                    UserId = user.Id,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth ?? DateTime.UtcNow,
                    Gender = model.Gender,
                    BloodType = model.BloodType
                };

                await _unitOfWork.Repository<Patient>().AddAsync(patient);
                await _unitOfWork.CompleteAsync();

                // Create empty PatientMedicalProfile
                var medicalProfile = new PatientMedicalProfile
                {
                    PatientId = patient.Id,
                    ChronicDiseases = model.ChronicDiseases
                };

                await _unitOfWork.Repository<PatientMedicalProfile>().AddAsync(medicalProfile);
                await _unitOfWork.CompleteAsync();
            }

            // ── Doctor Registration ──
            else if (model.UserType == "Doctor")
            {
                // Save uploaded files
                string? syndicatePath = null;
                string? certificatePath = null;

                if (model.SyndicateIdCard != null && model.SyndicateIdCard.Length > 0)
                {
                    syndicatePath = await SaveFileAsync(model.SyndicateIdCard, "documents");
                }

                if (model.Certificate != null && model.Certificate.Length > 0)
                {
                    certificatePath = await SaveFileAsync(model.Certificate, "documents");
                }

                var doctor = new Doctor
                {
                    UserId = user.Id,
                    FullName = model.FullName,
                    SpecializationId = model.SpecializationId ?? 0,
                    ConsultationFee = model.ConsultationFee ?? 0,
                    IsVerified = false,
                    SyndicateIdCardPath = syndicatePath,
                    CertificatePath = certificatePath
                };

                await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
                await _unitOfWork.CompleteAsync();

                // Create DoctorSchedule records for selected days
                foreach (var schedule in model.Schedules.Where(s => s.IsSelected))
                {
                    var doctorSchedule = new DoctorSchedule
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = (int)schedule.DayOfWeek,
                        StartTime = schedule.StartTime ?? new TimeSpan(9, 0, 0),
                        EndTime = schedule.EndTime ?? new TimeSpan(17, 0, 0),
                        SlotDurationMinutes = schedule.SlotDurationMinutes
                    };

                    await _unitOfWork.Repository<DoctorSchedule>().AddAsync(doctorSchedule);
                }

                await _unitOfWork.CompleteAsync();
            }

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // ══════════════════════════════════════════
        //  LOGOUT
        // ══════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ══════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════

        private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(uploadsDir);

            var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subfolder}/{uniqueName}";
        }
    }
}
