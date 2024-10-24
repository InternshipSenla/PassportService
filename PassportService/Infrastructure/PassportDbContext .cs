using Microsoft.EntityFrameworkCore;
using PassportService.Core;

namespace PassportService.Infrastructure
{
    public class PassportDbContext :DbContext
    {
        private IConfiguration _configuration;
        public DbSet<Passport> Passports { get; set; }

        public PassportDbContext(DbContextOptions<PassportDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
          => optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Passport>()
                     .HasIndex(p => new { p.Series, p.Number })
                     .IsUnique();// Индекс для уникальности серии и номера
        }      
    }
}
