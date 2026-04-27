using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShefaaHealthCare.Models
{
    public class PatientMedicalProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [MaxLength(1000)]
        public string? Allergies { get; set; }

        [MaxLength(1000)]
        public string? ChronicDiseases { get; set; }

        [MaxLength(1000)]
        public string? FamilyHistory { get; set; }

        // Navigation Properties
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<MedicalAttachment> MedicalAttachments { get; set; } = new List<MedicalAttachment>();
    }
}
