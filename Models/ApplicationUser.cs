using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ShefaaHealthCare.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string UserType { get; set; } = string.Empty; // "Patient", "Doctor", "Admin"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
    }
}
