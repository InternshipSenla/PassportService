using Microsoft.AspNetCore.Mvc;
using PassportService.Core;
using PassportService.Infrastructure;
using PassportService.Service;

namespace PassportService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PassportController :Controller
    {
        private IPassportRepository _passportService;
        private PassportDbContext _dbContext;

        public PassportController(IPassportRepository passportService, PassportDbContext dbContext)
        {
            _passportService = passportService;
            _dbContext = dbContext;
        }

        [HttpGet("AllPassports")]
        public async Task<IActionResult> GetAllPassports()
        {
            List<Passport> passports = await _passportService.GetAllPassports();
            return Ok(Results.Json(passports));
        }

        [HttpGet("Series/{Series}")]
        public async Task<IActionResult> GetPassportsBySeries(string Series)
        {
            List<Passport> passports = await _passportService.GetPassportsBySeries(Series);
            return Ok(Results.Json(passports));
        }

        [HttpGet("Number/{Number}")]
        public async Task<IActionResult> GetPassportsByNumber(string Number)
        {
            List<Passport> passports = await _passportService.GetPassportsByNumber(Number);
            return Ok(Results.Json(passports));
        }

        [HttpGet("SeriesAndNumber/{SeriesAndNumber}")]
        public async Task<IActionResult> GetPassportsBySeriesAndNumber(string SeriesAndNumber)
        {
            List<Passport> passports = await _passportService.GetPassportsBySeriesAndNumber(SeriesAndNumber);
            return Ok(Results.Json(passports));
        }

        [HttpGet("InactivePassportsSeries/{Series}")]
        public async Task<IActionResult> GetInactivePassportsBySeries(string Series)
        {
            List<Passport> passports = await _passportService.GetInactivePassportsBySeries(Series);
            return Ok(Results.Json(passports));
        }

        [HttpGet("InactivePassportsNumber/{Number}")]
        public async Task<IActionResult> GetInactivePassportsByNumber(string Number)
        {
            List<Passport> passports = await _passportService.GetInactivePassportsByNumber(Number);
            return Ok(Results.Json(passports));
        }

        [HttpGet("InactivePassportsSeriesAndNumber/{SeriesAndNumber}")]
        public async Task<IActionResult> GetInactivePassportsBySeriesAndNumber(string SeriesAndNumber)
        {
            List<Passport> passports = await _passportService.GetInactivePassportsBySeriesAndNumber(SeriesAndNumber);
            return Ok(Results.Json(passports));
        }

        [HttpGet("PassportsByDate/{date}")]
        public async Task<IActionResult> GetPassportsByDate(string date)
        {
            if(!DateTime.TryParse(date, out var parsedDate))
            {
                throw new ArgumentException("Неверный формат даты.");
            }

            // Преобразуем дату в UTC
            DateTime utcDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);

            List<Passport> passports = await _passportService.GetPassportsByDate(utcDate);

            if(passports == null || passports.Count == 0)
            {
                return NotFound(new { Message = "Паспорта не найдены за указанную дату." });
            }

            return Ok(Results.Json(passports));
        }
    }
}
