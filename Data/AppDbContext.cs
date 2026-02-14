// ========== БД КОНТЕКСТ: AppDbContext ==========
// Entity Framework Core контекст для работы с БД

using Microsoft.EntityFrameworkCore;
using PhotoHost.Models;

namespace PhotoHost.Data
{
    public class AppDbContext : DbContext
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
                // Use SQLite-compatible default timestamp
                entity.Property(e => e.UploadDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
