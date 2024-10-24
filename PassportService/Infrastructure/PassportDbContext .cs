using Microsoft.EntityFrameworkCore;
using PassportService.Core;

namespace PassportService.Infrastructure
{
    public class PassportDbContext :DbContext
    {    
        public DbSet<Passport> Passports { get; set; }

        public PassportDbContext(DbContextOptions<PassportDbContext> options)
            : base(options)
        {    }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Passport>()
                     .HasIndex(p => new { p.Series, p.Number })
                     .IsUnique();// Индекс для уникальности серии и номера
        }      
    }
}
