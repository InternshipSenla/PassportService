using Microsoft.AspNetCore.Mvc;
using PassportService.Core;
using PassportService.Services;

namespace PassportService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PassportController :Controller
    {
        private IPassportRepository _passportService;

        public PassportController(IPassportRepository passportService)
        {
            _passportService = passportService;
        }

        [HttpGet("GetPassportsBySeriesAndNumber/{series}/{number}")]
        public async Task<IActionResult> GetPassportsBySeriesAndNumber(string series, string number)
        {
            List<Passport> passports = await _passportService.GetPassportsBySeriesAndNumber(series, number);
            return Ok(Results.Json(passports));
        }

        [HttpGet("GetInactivePassportsBySeriesAndNumber/{series}/{number}")]
        public async Task<IActionResult> GetInactivePassportsBySeriesAndNumber(string series, string number)
        {
            List<Passport> passports = await _passportService.GetInactivePassportsBySeriesAndNumber(series, number);
            return Ok(Results.Json(passports));
        }

        [HttpGet("GetPassportsByDate/{date}")]
        public async Task<IActionResult> GetPassportsByDate(DateTime date)
        {
            // Преобразуем дату в UTC
            DateTime utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            List<Passport> passports = await _passportService.GetPassportsByDate(utcDate);

            if(passports == null || passports.Count == 0)
            {
                return NotFound(new { Message = "Паспорта не найдены за указанную дату." });
            }

            return Ok(Results.Json(passports));
        }
    }
}
