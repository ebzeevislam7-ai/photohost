using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PhotoHost.Models
{
    public class AppUser : IdentityUser
    {
        [StringLength(150)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(150)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Bio { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
    }
}
