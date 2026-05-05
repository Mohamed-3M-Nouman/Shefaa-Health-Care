using System.ComponentModel.DataAnnotations;

namespace ShefaaHealthCare.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MaxLength(150, ErrorMessage = "الاسم لا يتجاوز 150 حرفاً")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "نوع المستخدم")]
        public string UserType { get; set; } = string.Empty;

        // Patient-specific fields
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

        // Doctor-specific fields
        [Display(Name = "التخصص")]
        public int? SpecializationId { get; set; }

        [Display(Name = "رسوم الاستشارة")]
        [Range(0, 10000, ErrorMessage = "رسوم الاستشارة يجب أن تكون بين 0 و 10000")]
        public decimal? ConsultationFee { get; set; }

        [Display(Name = "حالة التحقق")]
        public bool IsVerified { get; set; }
    }
}
