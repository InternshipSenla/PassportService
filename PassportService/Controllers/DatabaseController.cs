using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassportService.Core;
using PassportService.Infrastructure;
using PassportService.Services;

namespace PassportService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseController :Controller
    {
        private IPassportRepository _passportService;
        private ICsvPassportLoaderService _cvsPasportService;
        private PassportDbContext _dbContext;

        public DatabaseController(ICsvPassportLoaderService cvsPasportService, IPassportRepository passportService, PassportDbContext dbContext)
        {
            _cvsPasportService = cvsPasportService;
            _passportService = passportService;
            _dbContext = dbContext;
        }

        [HttpGet("GetPassportsFromFileAndAddPassportDb")]
        public async Task<IActionResult> GetPassportsFromFile()
        {
            await _cvsPasportService.LoadPassportsFromCsvAsync();
            List<Passport> passports = await _passportService.GetAllPassports();
            return Ok(Results.Json(passports));
        }

        [HttpDelete("ClearDatabase")]
        public async Task ClearDatabaseAsync()
        {
            var passports = await _dbContext.Passports.ToListAsync();
            _dbContext.Passports.RemoveRange(passports);
            await _dbContext.SaveChangesAsync();
        }

        [HttpDelete("ClearDatabaseReset")]
        public async Task ResetDatabaseAsync()
        {
            await _dbContext.Database.EnsureDeletedAsync(); // Удаляет базу данных
            await _dbContext.Database.EnsureCreatedAsync(); // Пересоздает базу данных
        }

    }
}
