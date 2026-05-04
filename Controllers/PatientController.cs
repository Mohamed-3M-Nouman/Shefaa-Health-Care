using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShefaaHealthCare.Data;
using ShefaaHealthCare.Models;
using ShefaaHealthCare.Models.ViewModels;

namespace ShefaaHealthCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public PatientController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // ══════════════════════════════════════════
        //  Helper: Get current patient record
        // ══════════════════════════════════════════
        private async Task<Patient?> GetCurrentPatientAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return null;

            return await _context.Patients
                .Include(p => p.PatientMedicalProfile)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        // ══════════════════════════════════════════
        //  DASHBOARD (Index)
        // ══════════════════════════════════════════
        public async Task<IActionResult> Dashboard()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account");

            var now = DateTime.Now;

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Specialization)
                .Where(a => a.PatientId == patient.Id)
                .ToListAsync();

            var vm = new PatientDashboardViewModel
            {
                PatientName = patient.FullName,
                TotalAppointments = appointments.Count,
                UpcomingAppointmentsCount = appointments.Count(a => a.AppointmentDate > now && a.Status != "Cancelled"),
                CompletedAppointmentsCount = appointments.Count(a => a.Status == "Completed"),
                CancelledAppointmentsCount = appointments.Count(a => a.Status == "Cancelled"),
                UpcomingAppointments = appointments
                    .Where(a => a.AppointmentDate > now && a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentDate)
                    .Take(5)
                    .ToList()
            };

            return View(vm);
        }

        // ══════════════════════════════════════════
        //  APPOINTMENTS LIST
        // ══════════════════════════════════════════
        public async Task<IActionResult> Appointments()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account");

            var now = DateTime.Now;

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Specialization)
                .Where(a => a.PatientId == patient.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var vm = new AppointmentListViewModel
            {
                Appointments = appointments.Select(a => new AppointmentItemViewModel
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    DoctorName = a.Doctor.FullName,
                    SpecializationName = a.Doctor.Specialization?.Name ?? "—",
                    Status = a.Status,
                    ConsultationFee = a.Doctor.ConsultationFee,
                    DoctorCity = a.Doctor.City,
                    CanCancel = a.Status != "Cancelled"
                              && a.Status != "Completed"
                              && (a.AppointmentDate - now).TotalHours >= 24
                }).ToList()
            };

            return View(vm);
        }

        // ══════════════════════════════════════════
        //  CANCEL APPOINTMENT (POST)
        // ══════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == patient.Id);

            if (appointment == null)
            {
                TempData["Error"] = "لم يتم العثور على الموعد المطلوب.";
                return RedirectToAction(nameof(Appointments));
            }

            if (appointment.Status == "Cancelled")
            {
                TempData["Error"] = "هذا الموعد ملغي بالفعل.";
                return RedirectToAction(nameof(Appointments));
            }

            if (appointment.Status == "Completed")
            {
                TempData["Error"] = "لا يمكن إلغاء موعد تم إكماله بالفعل.";
                return RedirectToAction(nameof(Appointments));
            }

            var hoursRemaining = (appointment.AppointmentDate - DateTime.Now).TotalHours;
            if (hoursRemaining < 24)
            {
                TempData["Error"] = "لا يمكن إلغاء الموعد قبل أقل من 24 ساعة من موعده. يرجى التواصل مع الدعم.";
                return RedirectToAction(nameof(Appointments));
            }

            appointment.Status = "Cancelled";
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء الموعد بنجاح.";
            return RedirectToAction(nameof(Appointments));
        }

        // ══════════════════════════════════════════
        //  MEDICAL PROFILE (GET)
        // ══════════════════════════════════════════
        public async Task<IActionResult> MedicalProfile()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account");

            var profile = await _context.PatientMedicalProfiles
                .Include(p => p.MedicalAttachments)
                .FirstOrDefaultAsync(p => p.PatientId == patient.Id);

            var vm = new MedicalProfileViewModel
            {
                PatientId = patient.Id,
                ProfileId = profile?.Id,
                BloodType = patient.BloodType,
                ChronicDiseases = profile?.ChronicDiseases,
                Allergies = profile?.Allergies,
                FamilyHistory = profile?.FamilyHistory,
                Attachments = profile?.MedicalAttachments?.OrderByDescending(a => a.UploadedAt).ToList() ?? []
            };

            return View(vm);
        }

        // ══════════════════════════════════════════
        //  UPDATE PROFILE (POST)
        // ══════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(MedicalProfileViewModel model)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account");

            // Update blood type on Patient record
            patient.BloodType = model.BloodType;
            _context.Patients.Update(patient);

            // Get or create profile
            var profile = await _context.PatientMedicalProfiles
                .FirstOrDefaultAsync(p => p.PatientId == patient.Id);

            if (profile == null)
            {
                profile = new PatientMedicalProfile
                {
                    PatientId = patient.Id,
                    ChronicDiseases = model.ChronicDiseases,
                    Allergies = model.Allergies,
                    FamilyHistory = model.FamilyHistory
                };
                _context.PatientMedicalProfiles.Add(profile);
            }
            else
            {
                profile.ChronicDiseases = model.ChronicDiseases;
                profile.Allergies = model.Allergies;
                profile.FamilyHistory = model.FamilyHistory;
                _context.PatientMedicalProfiles.Update(profile);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تحديث الملف الطبي بنجاح.";
            return RedirectToAction(nameof(MedicalProfile));
        }

        // ══════════════════════════════════════════
        //  UPLOAD ATTACHMENT (POST)
        // ══════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(IFormFile file, string title)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account");

            // ─── Validate title ───
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "يرجى إدخال عنوان للملف المرفق.";
                return RedirectToAction(nameof(MedicalProfile));
            }

            // ─── Validate file presence ───
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "يرجى اختيار ملف لرفعه.";
                return RedirectToAction(nameof(MedicalProfile));
            }

            // ─── Validate file size (5 MB max) ───
            const long maxSize = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxSize)
            {
                TempData["Error"] = "حجم الملف يتجاوز الحد الأقصى (5 ميجابايت).";
                return RedirectToAction(nameof(MedicalProfile));
            }

            // ─── Validate file extension ───
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "صيغة الملف غير مسموح بها. الصيغ المقبولة: JPG, JPEG, PNG, GIF, PDF فقط.";
                return RedirectToAction(nameof(MedicalProfile));
            }

            // ─── Ensure medical profile exists ───
            var profile = await _context.PatientMedicalProfiles
                .FirstOrDefaultAsync(p => p.PatientId == patient.Id);

            if (profile == null)
            {
                profile = new PatientMedicalProfile { PatientId = patient.Id };
                _context.PatientMedicalProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            // ─── Save file to disk ───
            var userId = _userManager.GetUserId(User);
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "patients", userId!);
            Directory.CreateDirectory(uploadsDir);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var physicalPath = Path.Combine(uploadsDir, uniqueFileName);
            var relativePath = $"/uploads/patients/{userId}/{uniqueFileName}";

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // ─── Determine document type ───
            var docType = extension == ".pdf" ? "PDF" : "Image";

            // ─── Save attachment record ───
            var attachment = new MedicalAttachment
            {
                PatientMedicalProfileId = profile.Id,
                Title = title,
                FilePath = relativePath,
                DocumentType = docType,
                UploadedAt = DateTime.UtcNow
            };

            _context.MedicalAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم رفع الملف بنجاح.";
            return RedirectToAction(nameof(MedicalProfile));
        }
    }
}
