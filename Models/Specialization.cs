using System.ComponentModel.DataAnnotations;

namespace ShefaaHealthCare.Models
{
    public class Specialization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation Properties
        public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}
