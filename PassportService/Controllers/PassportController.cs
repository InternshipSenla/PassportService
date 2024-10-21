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



    }
}
