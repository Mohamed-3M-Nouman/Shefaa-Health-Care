using System.ComponentModel.DataAnnotations;

namespace ShefaaHealthCare.Models.ViewModels
{
    public class AccountViewModel
    {
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "رقم الهاتف")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "نوع الحساب")]
        public string UserType { get; set; } = string.Empty;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "الجنس")]
        public string? Gender { get; set; }

        [Display(Name = "فصيلة الدم")]
        public string? BloodType { get; set; }

        [Display(Name = "الأمراض المزمنة")]
        public string? ChronicDiseases { get; set; }

        [Display(Name = "التخصص")]
        public string? SpecializationName { get; set; }

        [Display(Name = "رسوم الاستشارة")]
        public decimal? ConsultationFee { get; set; }

        [Display(Name = "حالة التحقق")]
        public bool IsVerified { get; set; }
    }
}