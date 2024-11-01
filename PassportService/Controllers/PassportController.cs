using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PassportService.Core;
using PassportService.Infrastructure;
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
     
        [HttpGet("GetPassportsBySeries/{Series}")]
        public async Task<IActionResult> GetPassportsBySeries(string Series)
        {
            List<Passport> passports = await _passportService.GetPassportsBySeries(Series);
            return Ok(Results.Json(passports));
        }

        [HttpGet("GetPassportsByNumber/{Number}")]
        public async Task<IActionResult> GetPassportsByNumber(string Number)
        {
            List<Passport> passports = await _passportService.GetPassportsByNumber(Number);
            return Ok(Results.Json(passports));
        }

        [HttpGet("GetInactivePassportsBySeries/{Series}")]
        public async Task<IActionResult> GetInactivePassportsBySeries(string Series)
        {
            List<Passport> passports = await _passportService.GetInactivePassportsBySeries(Series);
            return Ok(Results.Json(passports));
        }

        [HttpGet("GetInactivePassportsByNumber/{Number}")]
        public async Task<IActionResult> GetInactivePassportsByNumber(string Number)
        {
            List<Passport> passports = await _passportService.GetInactivePassportsByNumber(Number);
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
