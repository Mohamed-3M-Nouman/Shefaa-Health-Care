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
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ══════════════════════════════════════════
        //  CHECKOUT (GET)
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Checkout(int doctorId)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Specialization)
                .FirstOrDefaultAsync(d => d.Id == doctorId && d.IsVerified);

            if (doctor == null) return NotFound();

            var model = new CheckoutViewModel
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.FullName,
                SpecializationName = doctor.Specialization?.Name ?? "—",
                ConsultationFee = doctor.ConsultationFee,
                DoctorCity = doctor.City,
                ClinicAddress = doctor.ClinicAddress,
                DoctorRating = doctor.Rating,
                DoctorExperience = doctor.ExperienceYears,
                AppointmentDate = DateTime.Today.AddDays(1),
                PaymentMethod = "Visa"
            };

            return View(model);
        }

        // ══════════════════════════════════════════
        //  GET AVAILABLE TIME SLOTS (AJAX)
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, DateTime selectedDate)
        {
            // 1. Map .NET DayOfWeek to our DB int (0=Sunday)
            int dayOfWeek = (int)selectedDate.DayOfWeek; // .NET: 0=Sunday

            // 2. Find schedule for this doctor on this day
            var schedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek);

            if (schedule == null)
                return Json(new { available = false, message = "لا يوجد جدول عمل لهذا الطبيب في هذا اليوم.", slots = Array.Empty<string>() });

            // 3. Generate all possible slots
            int slotMinutes = schedule.SlotDurationMinutes > 0 ? schedule.SlotDurationMinutes : 30;
            var allSlots = new List<string>();
            var current = schedule.StartTime;

            while (current.Add(TimeSpan.FromMinutes(slotMinutes)) <= schedule.EndTime)
            {
                allSlots.Add(current.ToString(@"hh\:mm"));
                current = current.Add(TimeSpan.FromMinutes(slotMinutes));
            }

            // 4. Find already booked slots on this date
            var bookedTimes = await _context.Appointments
                .Where(a => a.DoctorId == doctorId
                         && a.AppointmentDate.Date == selectedDate.Date
                         && a.Status != "Cancelled")
                .Select(a => a.AppointmentDate.TimeOfDay)
                .ToListAsync();

            var bookedSet = new HashSet<string>(bookedTimes.Select(t => t.ToString(@"hh\:mm")));

            // 5. Filter out booked slots
            var availableSlots = allSlots.Where(s => !bookedSet.Contains(s)).ToList();

            return Json(new { available = true, slots = availableSlots });
        }

        // ══════════════════════════════════════════
        //  PROCESS PAYMENT (POST)
        // ══════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Specialization)
                .FirstOrDefaultAsync(d => d.Id == model.DoctorId && d.IsVerified);

            if (doctor == null)
            {
                TempData["Error"] = "الطبيب المحدد غير موجود.";
                return RedirectToAction("Index", "Doctor");
            }

            // Repopulate display fields
            model.DoctorName = doctor.FullName;
            model.SpecializationName = doctor.Specialization?.Name ?? "—";
            model.ConsultationFee = doctor.ConsultationFee;
            model.DoctorCity = doctor.City;
            model.ClinicAddress = doctor.ClinicAddress;
            model.DoctorRating = doctor.Rating;
            model.DoctorExperience = doctor.ExperienceYears;

            // Visa-specific validation
            if (model.PaymentMethod == "Visa")
            {
                if (string.IsNullOrWhiteSpace(model.CardHolderName))
                    ModelState.AddModelError(nameof(model.CardHolderName), "يرجى إدخال اسم حامل البطاقة.");
                if (string.IsNullOrWhiteSpace(model.CardNumber) || !System.Text.RegularExpressions.Regex.IsMatch(model.CardNumber, @"^\d{16}$"))
                    ModelState.AddModelError(nameof(model.CardNumber), "رقم البطاقة يجب أن يتكون من 16 رقم.");
                if (string.IsNullOrWhiteSpace(model.ExpiryDate) || !System.Text.RegularExpressions.Regex.IsMatch(model.ExpiryDate, @"^(0[1-9]|1[0-2])\/\d{2}$"))
                    ModelState.AddModelError(nameof(model.ExpiryDate), "صيغة تاريخ الانتهاء: MM/YY");
                if (string.IsNullOrWhiteSpace(model.CVV) || !System.Text.RegularExpressions.Regex.IsMatch(model.CVV, @"^\d{3}$"))
                    ModelState.AddModelError(nameof(model.CVV), "رمز CVV يجب أن يتكون من 3 أرقام.");
            }

            if (model.AppointmentDate.Date < DateTime.Today)
                ModelState.AddModelError(nameof(model.AppointmentDate), "لا يمكن حجز موعد في تاريخ ماضٍ.");

            if (string.IsNullOrWhiteSpace(model.AppointmentTime))
                ModelState.AddModelError(nameof(model.AppointmentTime), "يرجى اختيار وقت الموعد.");

            if (!ModelState.IsValid)
                return View("Checkout", model);

            // Get patient
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
            {
                TempData["Error"] = "لم يتم العثور على سجل المريض.";
                return RedirectToAction("Dashboard", "Patient");
            }

            // Parse time and combine
            var timeParts = model.AppointmentTime.Split(':');
            var timeSpan = new TimeSpan(int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
            var appointmentDateTime = model.AppointmentDate.Date.Add(timeSpan);

            // Check slot not already booked
            var alreadyBooked = await _context.Appointments
                .AnyAsync(a => a.DoctorId == model.DoctorId
                            && a.AppointmentDate == appointmentDateTime
                            && a.Status != "Cancelled");

            if (alreadyBooked)
            {
                ModelState.AddModelError(nameof(model.AppointmentTime), "هذا الموعد تم حجزه بالفعل. يرجى اختيار وقت آخر.");
                return View("Checkout", model);
            }

            var appointment = new Appointment
            {
                PatientId = patient.Id,
                DoctorId = model.DoctorId,
                AppointmentDate = appointmentDateTime,
                Status = model.PaymentMethod == "Cash" ? "Pending" : "Confirmed",
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["PaymentMethod"] = model.PaymentMethod == "Cash" ? "الدفع عند الزيارة" : "بطاقة Visa";
            return RedirectToAction(nameof(Success), new { appointmentId = appointment.Id });
        }

        // ══════════════════════════════════════════
        //  SUCCESS / RECEIPT
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Success(int appointmentId)
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null) return RedirectToAction("Dashboard", "Patient");

            var appointment = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.Specialization)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patient.Id);

            if (appointment == null) return NotFound();

            var receipt = new BookingReceiptViewModel
            {
                AppointmentId = appointment.Id,
                DoctorName = appointment.Doctor.FullName,
                SpecializationName = appointment.Doctor.Specialization?.Name ?? "—",
                AppointmentDate = appointment.AppointmentDate,
                ConsultationFee = appointment.Doctor.ConsultationFee,
                Status = appointment.Status,
                DoctorCity = appointment.Doctor.City,
                PaymentMethod = TempData["PaymentMethod"]?.ToString() ?? "—",
                BookedAt = appointment.CreatedAt
            };

            return View(receipt);
        }
    }
}
