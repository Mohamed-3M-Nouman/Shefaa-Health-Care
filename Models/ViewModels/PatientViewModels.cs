using System.ComponentModel.DataAnnotations;

namespace ShefaaHealthCare.Models.ViewModels
{
    // ══════════════════════════════════════════
    //  PATIENT DASHBOARD
    // ══════════════════════════════════════════
    public class PatientDashboardViewModel
    {
        public string PatientName { get; set; } = string.Empty;
        public int TotalAppointments { get; set; }
        public int UpcomingAppointmentsCount { get; set; }
        public int CompletedAppointmentsCount { get; set; }
        public int CancelledAppointmentsCount { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; } = [];
    }

    // ══════════════════════════════════════════
    //  APPOINTMENTS LIST
    // ══════════════════════════════════════════
    public class AppointmentListViewModel
    {
        public List<AppointmentItemViewModel> Appointments { get; set; } = [];
    }

    public class AppointmentItemViewModel
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecializationName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public string? DoctorCity { get; set; }

        /// <summary>
        /// True if the appointment is >= 24 hours away AND not already Cancelled/Completed.
        /// </summary>
        public bool CanCancel { get; set; }
    }

    // ══════════════════════════════════════════
    //  MEDICAL PROFILE
    // ══════════════════════════════════════════
    public class MedicalProfileViewModel
    {
        public int PatientId { get; set; }
        public int? ProfileId { get; set; }

        [MaxLength(5)]
        [Display(Name = "فصيلة الدم")]
        public string? BloodType { get; set; }

        [Display(Name = "الأمراض المزمنة")]
        [MaxLength(1000)]
        public string? ChronicDiseases { get; set; }

        [Display(Name = "الحساسية")]
        [MaxLength(1000)]
        public string? Allergies { get; set; }

        [Display(Name = "التاريخ العائلي")]
        [MaxLength(1000)]
        public string? FamilyHistory { get; set; }

        public List<MedicalAttachment> Attachments { get; set; } = [];
    }
}
