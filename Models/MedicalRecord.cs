using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShefaaHealthCare.Models
{
    public class MedicalRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientMedicalProfileId { get; set; }

        [ForeignKey("PatientMedicalProfileId")]
        public virtual PatientMedicalProfile PatientMedicalProfile { get; set; } = null!;

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [MaxLength(2000)]
        public string? Diagnosis { get; set; }

        [MaxLength(2000)]
        public string? Prescription { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
