using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
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
        //  ACCOUNT PROFILE
        // ══════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new AccountViewModel
            {
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                CreatedAt = user.CreatedAt,
                FullName = user.UserName ?? user.Email ?? string.Empty
            };

            if (user.UserType == "Patient")
            {
                var patient = await _context.Patients
                    .Include(p => p.PatientMedicalProfile)
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (patient != null)
                {
                    model.FullName = patient.FullName;
                    model.DateOfBirth = patient.DateOfBirth;
                    model.Gender = patient.Gender;
                    model.BloodType = patient.BloodType;
                    model.ChronicDiseases = patient.PatientMedicalProfile?.ChronicDiseases;
                }
            }
            else if (user.UserType == "Doctor")
            {
                var doctor = await _context.Doctors
                    .Include(d => d.Specialization)
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor != null)
                {
                    model.FullName = doctor.FullName;
                    model.SpecializationName = doctor.Specialization?.Name;
                    model.ConsultationFee = doctor.ConsultationFee;
                    model.IsVerified = doctor.IsVerified;
                }
            }

            return View(model);
        }


        // ══════════════════════════════════════════
        //  LOGIN
        // ══════════════════════════════════════════

        [HttpGet]
            [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
            [AllowAnonymous]
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
        //  REGISTER (GET)
        // ══════════════════════════════════════════

        [HttpGet]
            [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            // إرسال التخصصات للواجهة
            var specializations = await _unitOfWork.Repository<Specialization>().GetAllAsync();
            ViewBag.Specializations = new SelectList(specializations, "Id", "Name");

            // تهيئة أيام الأسبوع للطبيب
            var model = new RegisterViewModel
            {
                Schedules = []
            };
            for (int i = 0; i < 7; i++)
            {
                model.Schedules.Add(new ScheduleItemViewModel { DayOfWeek = i, IsSelected = false });
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> IsEmailInUse(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(true);
            }
            else
            {
                return Json($"البريد الإلكتروني '{email}' مسجل بالفعل.");
            }
        }

        // ══════════════════════════════════════════
        //  REGISTER (POST)
        // ══════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
            [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // 1. حل كارثة الـ Validation
            if (model.UserType == "Patient")
            {
                ModelState.Remove("SpecializationId");
                ModelState.Remove("ConsultationFee");
                ModelState.Remove("SyndicateIdCard");
                ModelState.Remove("Certificate");
                for (int i = 0; i < (model.Schedules?.Count ?? 0); i++)
                    ModelState.Remove($"Schedules[{i}].DayOfWeek");
            }
            else if (model.UserType == "Doctor")
            {
                ModelState.Remove("DateOfBirth");
                ModelState.Remove("Gender");
                ModelState.Remove("BloodType");
            }

            // 2. حل كارثة الـ ViewBag في حالة وجود أخطاء
            if (!ModelState.IsValid)
            {
                var specs = await _unitOfWork.Repository<Specialization>().GetAllAsync();
                ViewBag.Specializations = new SelectList(specs, "Id", "Name");
                return View(model);
            }

            // 3. حل كارثة تسريب البيانات باستخدام Transaction
            // قائمة لتتبع المسارات الفعلية للملفات المرفوعة لإتاحة حذفها عند الفشل
            var uploadedPhysicalPaths = new List<string>();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    UserType = model.UserType,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.UserType);

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

                        var medicalProfile = new PatientMedicalProfile
                        {
                            PatientId = patient.Id,
                            ChronicDiseases = model.ChronicDiseases
                        };
                        await _unitOfWork.Repository<PatientMedicalProfile>().AddAsync(medicalProfile);
                        await _unitOfWork.CompleteAsync();
                    }
                    else if (model.UserType == "Doctor")
                    {
                        string? syndicatePath = null;
                        if (model.SyndicateIdCard != null)
                        {
                            var (webPath, physicalPath) = await SaveFileAsync(model.SyndicateIdCard, "documents");
                            syndicatePath = webPath;
                            uploadedPhysicalPaths.Add(physicalPath);
                        }

                        string? certificatePath = null;
                        if (model.Certificate != null)
                        {
                            var (webPath, physicalPath) = await SaveFileAsync(model.Certificate, "documents");
                            certificatePath = webPath;
                            uploadedPhysicalPaths.Add(physicalPath);
                        }

                        var doctor = new Doctor
                        {
                            UserId = user.Id,
                            FullName = model.FullName,
                            SpecializationId = model.SpecializationId ?? 0,
                            ConsultationFee = model.ConsultationFee ?? 0,
                            SyndicateIdCardPath = syndicatePath,
                            CertificatePath = certificatePath,
                            IsVerified = false
                        };
                        await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
                        await _unitOfWork.CompleteAsync();

                        if (model.Schedules != null && model.Schedules.Any(s => s.IsSelected))
                        {
                            foreach (var schedule in model.Schedules.Where(s => s.IsSelected))
                            {
                                var doctorSchedule = new DoctorSchedule
                                {
                                    DoctorId = doctor.Id,
                                    DayOfWeek = schedule.DayOfWeek,
                                    StartTime = schedule.StartTime ?? new TimeSpan(9, 0, 0),
                                    EndTime = schedule.EndTime ?? new TimeSpan(17, 0, 0),
                                    SlotDurationMinutes = schedule.SlotDurationMinutes
                                };
                                await _unitOfWork.Repository<DoctorSchedule>().AddAsync(doctorSchedule);
                            }
                            await _unitOfWork.CompleteAsync();
                        }
                    }

                    // تأكيد العملية وحفظ كل شيء
                    await transaction.CommitAsync();
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                // إرجاع أخطاء الـ Identity (مثل باسوورد ضعيف أو إيميل مستخدم)
                foreach (var error in result.Errors)
                {
                    string arabicError = error.Code switch
                    {
                        "DuplicateUserName" => $"البريد الإلكتروني '{model.Email}' مسجل مسبقاً.",
                        "DuplicateEmail" => $"البريد الإلكتروني '{model.Email}' مسجل مسبقاً.",
                        "PasswordTooShort" => "كلمة المرور قصيرة جداً (الحد الأدنى 6 أحرف).",
                        "PasswordRequiresNonAlphanumeric" => "كلمة المرور يجب أن تحتوي على رمز خاص واحد على الأقل (مثل @، #، $).",
                        "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.",
                        "PasswordRequiresLower" => "كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل.",
                        "PasswordRequiresUpper" => "كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل.",
                        _ => error.Description
                    };
                    ModelState.AddModelError(string.Empty, arabicError);
                }
            }
            catch (Exception)
            {
                // التراجع عن أي شيء تم حفظه في الداتا بيز بسبب ظهور Error
                await transaction.RollbackAsync();

                // حذف الملفات اليتيمة من الـ File System لمنع استهلاك المساحة
                foreach (var physicalPath in uploadedPhysicalPaths)
                {
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }

                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء إنشاء الحساب. تأكد من إدخال كافة البيانات بشكل صحيح.");
            }

            var fallbackSpecs = await _unitOfWork.Repository<Specialization>().GetAllAsync();
            ViewBag.Specializations = new SelectList(fallbackSpecs, "Id", "Name");
            return View(model);
        }

        // ══════════════════════════════════════════
        //  FORGOT PASSWORD
        // ══════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || user.PhoneNumber != model.PhoneNumber)
            {
                // لا نكشف عما إذا كان الحساب غير موجود أصلًا أو أن الهاتف خاطئ لدواعي الأمان
                ModelState.AddModelError(string.Empty, "البيانات المدخلة غير صحيحة أو لا تتطابق مع أي حساب لدينا.");
                return View(model);
            }

            // توليد رمز إعادة تعيين كلمة المرور المخفي (Token)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // توجيه المستخدم لصفحة إعادة التعيين مع تمرير التوكن والإيميل
            return RedirectToAction(nameof(ResetPassword), new { email = user.Email, token });
        }

        // ══════════════════════════════════════════
        //  RESET PASSWORD
        // ══════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (email == null || token == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "تم إعادة تعيين كلمة المرور بنجاح. يمكنك الآن تسجيل الدخول.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                string arabicError = error.Code switch
                {
                    "InvalidToken" => "رابط أو جلسة إعادة التعيين منتهية الصلاحية. يرجى المحاولة مرة أخرى.",
                    "PasswordTooShort" => "كلمة المرور قصيرة جداً (الحد الأدنى 6 أحرف).",
                    "PasswordRequiresNonAlphanumeric" => "كلمة المرور يجب أن تحتوي على رمز خاص واحد على الأقل (مثل @، #، $).",
                    "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.",
                    "PasswordRequiresLower" => "كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل.",
                    "PasswordRequiresUpper" => "كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل.",
                    _ => error.Description
                };
                ModelState.AddModelError(string.Empty, arabicError);
            }

            return View(model);
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

        /// <summary>
        /// يحفظ الملف على القرص ويُعيد مساراً مزدوجاً:
        /// - webPath   : المسار النسبي للاستخدام في قاعدة البيانات  (مثال: /uploads/documents/guid_file.pdf)
        /// - physicalPath : المسار الفعلي الكامل على الـ File System لإتاحة الحذف عند الحاجة
        /// </summary>
        private async Task<(string webPath, string physicalPath)> SaveFileAsync(IFormFile file, string subfolder)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(uploadsDir);

            var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
            var physicalPath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var webPath = $"/uploads/{subfolder}/{uniqueName}";
            return (webPath, physicalPath);
        }
    }
}
