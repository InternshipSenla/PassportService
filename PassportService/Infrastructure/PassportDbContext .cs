﻿using Microsoft.EntityFrameworkCore;
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

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    //   base.OnModelCreating(modelBuilder);
        //    modelBuilder.Entity<Passport>().HasIndex(p => new { p.Series, p.Number }).IsUnique();  // Индекс для уникальности серии и номера
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var passports = GetPassports();
            modelBuilder.Entity<Passport>().HasData(passports);
        }

        private List<Passport> GetPassports()
        {
            return new List<Passport>
            {
                new Passport { Id = 1, Series = "1234", Number = "567890", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2020, 1, 15), DateTimeKind.Utc) }, RemovedAt = null, DateLastRequest = DateTime.UtcNow.AddDays(-2)},
                new Passport { Id = 2, Series = "2345", Number = "678901", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2019, 6, 10), DateTimeKind.Utc) }, RemovedAt = new List<DateTime ?> { DateTime.SpecifyKind(new DateTime(2022, 3, 5), DateTimeKind.Utc) },  DateLastRequest = DateTime.UtcNow.AddDays(-2)  },
                new Passport { Id = 3, Series = "3456", Number = "789012", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2021, 11, 22), DateTimeKind.Utc) }, RemovedAt = null,  DateLastRequest = DateTime.UtcNow.AddDays(-2)  },
                new Passport { Id = 4, Series = "4567", Number = "890123", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2018, 8, 30), DateTimeKind.Utc) }, RemovedAt = new List<DateTime ?> { DateTime.SpecifyKind(new DateTime(2023, 4, 15), DateTimeKind.Utc) },  DateLastRequest = DateTime.UtcNow.AddDays(-2)  },
                new Passport { Id = 5, Series = "5678", Number = "901234", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2022, 2, 28), DateTimeKind.Utc) }, RemovedAt = null, DateLastRequest = DateTime.UtcNow.AddDays(-2) },
                new Passport { Id = 6, Series = "6789", Number = "012345", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2020, 12, 1), DateTimeKind.Utc) }, RemovedAt = null,  DateLastRequest = DateTime.UtcNow.AddDays(-2)  },
                new Passport { Id = 7, Series = "7890", Number = "123456", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2021, 5, 20), DateTimeKind.Utc) }, RemovedAt = new List<DateTime ?> { DateTime.SpecifyKind(new DateTime(2024, 1, 10), DateTimeKind.Utc) }, DateLastRequest = DateTime.UtcNow.AddDays(-2)  },
                new Passport { Id = 8, Series = "8901", Number = "234567", CreatedAt = new List<DateTime> { DateTime.SpecifyKind(new DateTime(2019, 9, 15), DateTimeKind.Utc) }, RemovedAt = null,  DateLastRequest = DateTime.UtcNow.AddDays(-2) }
            };
        }
    }
}