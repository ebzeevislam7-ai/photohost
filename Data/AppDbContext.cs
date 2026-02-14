// ========== БД КОНТЕКСТ: AppDbContext ==========
// Entity Framework Core контекст для работы с БД

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PhotoHost.Models;

namespace PhotoHost.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация таблицы Photos
            modelBuilder.Entity<Photo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Path).IsRequired().HasMaxLength(500);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.UploadDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Foreign key relationship
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Photos)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация AppUser
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(150);
                entity.Property(e => e.LastName).HasMaxLength(150);
                entity.Property(e => e.Bio).HasMaxLength(500);
            });
        }
    }
}

