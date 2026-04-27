using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShefaaHealthCare.Models
{
    public class MedicalAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientMedicalProfileId { get; set; }

        [ForeignKey("PatientMedicalProfileId")]
        public virtual PatientMedicalProfile PatientMedicalProfile { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty; // Physical file stored in wwwroot/uploads/

        [MaxLength(100)]
        public string? DocumentType { get; set; } // e.g., "Lab Report", "X-Ray", "Prescription"

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
