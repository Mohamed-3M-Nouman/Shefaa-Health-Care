using System.ComponentModel.DataAnnotations;

namespace ShefaaHealthCare.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // ── Doctor Display Data (Read-only) ──
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecializationName { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public string? DoctorCity { get; set; }
        public string? ClinicAddress { get; set; }
        public decimal DoctorRating { get; set; }
        public int DoctorExperience { get; set; }

        // ── Booking Data ──
        [Required(ErrorMessage = "يرجى تحديد تاريخ الموعد.")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الموعد")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "يرجى تحديد وقت الموعد.")]
        [Display(Name = "وقت الموعد")]
        public string AppointmentTime { get; set; } = string.Empty; // "HH:mm"

        public List<DateTime> AvailableDates { get; set; } = [];

        // ── Payment Method ──
        [Required(ErrorMessage = "يرجى اختيار طريقة الدفع.")]
        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } = "Visa";

        // ── Dummy Payment (NEVER saved to DB) ──
        [Display(Name = "اسم حامل البطاقة")]
        [MaxLength(100)]
        public string? CardHolderName { get; set; }

        [Display(Name = "رقم البطاقة")]
        public string? CardNumber { get; set; }

        [Display(Name = "تاريخ الانتهاء")]
        public string? ExpiryDate { get; set; }

        [Display(Name = "CVV")]
        public string? CVV { get; set; }
    }

    public class BookingReceiptViewModel
    {
        public int AppointmentId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecializationName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public decimal ConsultationFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DoctorCity { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime BookedAt { get; set; }
    }
}
