// ========== МОДЕЛЬ: Photo ==========
// Класс для представления загруженной фотографии в БД

using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoHost.Models
{
    public class Photo
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public string Path { get; set; }

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    }
}
