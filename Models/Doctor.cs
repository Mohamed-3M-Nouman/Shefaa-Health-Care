using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShefaaHealthCare.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public int SpecializationId { get; set; }

        [ForeignKey("SpecializationId")]
        public virtual Specialization Specialization { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ConsultationFee { get; set; }

        public bool IsVerified { get; set; } = false;

        [MaxLength(500)]
        public string? SyndicateIdCardPath { get; set; }

        [MaxLength(500)]
        public string? CertificatePath { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(250)]
        public string? ClinicAddress { get; set; }

        public int ExperienceYears { get; set; } = 0;

        [Column(TypeName = "decimal(3,1)")]
        public decimal Rating { get; set; } = 0.0m;

        public int ReviewCount { get; set; } = 0;

        // Navigation Properties
        public virtual ICollection<DoctorSchedule> Schedules { get; set; } = [];
        public virtual ICollection<Appointment> Appointments { get; set; } = [];
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = [];
    }
}
