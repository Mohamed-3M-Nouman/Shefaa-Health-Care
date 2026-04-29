using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ShefaaHealthCare.Models.ViewModels
{
    public class RegisterViewModel
    {
        // ── Base Info ──
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم لا يتجاوز 150 حرفاً")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى اختيار نوع الحساب")]
        [Display(Name = "نوع الحساب")]
        public string UserType { get; set; } = string.Empty; // "Patient" or "Doctor"

        // ── Patient Fields ──
        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "الجنس")]
        [MaxLength(10)]
        public string? Gender { get; set; }

        [Display(Name = "فصيلة الدم")]
        [MaxLength(5)]
        public string? BloodType { get; set; }

        [Display(Name = "الأمراض المزمنة")]
        [MaxLength(1000)]
        public string? ChronicDiseases { get; set; }

        // ── Doctor Fields ──
        [Display(Name = "التخصص")]
        public int? SpecializationId { get; set; }

        [Display(Name = "رسوم الاستشارة")]
        public decimal? ConsultationFee { get; set; }

        [Display(Name = "بطاقة النقابة")]
        public IFormFile? SyndicateIdCard { get; set; }

        [Display(Name = "شهادة التخصص")]
        public IFormFile? Certificate { get; set; }

        // ── Doctor Schedule ──
        public List<ScheduleItemViewModel> Schedules { get; set; } = [];
    }

    public class ScheduleItemViewModel
    {
        public int DayOfWeek { get; set; }

        [Display(Name = "مختار")]
        public bool IsSelected { get; set; }

        [Display(Name = "وقت البدء")]
        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "وقت الانتهاء")]
        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "مدة الموعد (دقائق)")]
        public int SlotDurationMinutes { get; set; } = 30;
    }
}
