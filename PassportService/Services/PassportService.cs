using Microsoft.EntityFrameworkCore;
using PassportService.Core;
using PassportService.Infrastructure;

namespace PassportService.Service
{
    public class PassportService :IPassportRepository
    {
        public DateTime today = DateTime.UtcNow;
        IConfiguration _configuration;
        private PassportDbContext _dbContext;
        private readonly ILogger<PassportService> _logger;

        public PassportService(IConfiguration configuration, PassportDbContext dbContext, ILogger<PassportService> logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public Task<List<Passport>> GetAllPassports()
        {
            return _dbContext.Passports.ToListAsync();
        }

        public Task<List<Passport>> GetPassportsByNumber(string Number)
        {
            return _dbContext.Passports
                .Where(p => p.Number.Contains(Number))
                .ToListAsync();
        }

        public Task<List<Passport>> GetPassportsBySeries(string Series)
        {
            return _dbContext.Passports
                .Where(p => p.Series.Contains(Series))
                .ToListAsync();
        }

        public Task<List<Passport>> GetPassportsBySeriesAndNumber(string SeriesAndNumber)
        {
            var seriesPart = SeriesAndNumber.Length >= 4
                  ? SeriesAndNumber.Substring(0, 4)
                  : SeriesAndNumber;

            var numbeerPart = SeriesAndNumber.Length > 4
                  ? SeriesAndNumber.Substring(4, SeriesAndNumber.Length - 4)
                  : "";

            return _dbContext.Passports
                  .Where(p => p.Series.Contains(seriesPart))
                  .Where(p => p.Number.Contains(numbeerPart))
                  .ToListAsync();
        }

        public async Task<List<Passport>> GetPassportsByDate(DateTime date)
        {
            var passportsByDate = await _dbContext.Passports
              .Where(passport =>
                        passport.CreatedAt.Any(createdDate => createdDate.Date == date.Date)
                        ||
                        (passport.RemovedAt != null && passport.RemovedAt.Any(removedDate => removedDate.HasValue && removedDate.Value.Date == date.Date)))
              .ToListAsync();
            return passportsByDate;
        }

        public async Task<Passport> GetPassportAsync(Passport passport)
        {
            var exists = await _dbContext.Passports
               .Where(existing => existing.Series == passport.Series && existing.Number == passport.Number).FirstOrDefaultAsync();
            return exists;
        }

        public async Task UpdatePassport(Passport passport)
        {
            _dbContext.Update(passport);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddPassportsAsync(List<Passport> passports)
        {
            await _dbContext.Passports.AddRangeAsync(passports);
            await _dbContext.SaveChangesAsync();
        }      

        public Task<List<Passport>> SerchDeletePassports()
        {
            //return _dbContext.Passports
            //         .Where(passport => !passport.DateLastRequest.Date.Equals(today.Date)).ToListAsync();

            return _dbContext.Passports
                 .Where(passport =>
                     !passport.DateLastRequest.Date.Equals(today.Date) &&
                     (passport.RemovedAt == null ||
                     passport.RemovedAt.Any() &&
                     passport.CreatedAt.Max() > passport.RemovedAt.Max()))
                 .ToListAsync();
        }

        public async Task UpdateDeletedPassportAsync()
        {          
            var passportsToDelete = await SerchDeletePassports();

            foreach(var passportWasDelete in passportsToDelete)
            {
                if(passportWasDelete.RemovedAt == null)
                {
                    passportWasDelete.RemovedAt = new List<DateTime?>();
                }
                // Добавляем текущую дату в коллекцию
                passportWasDelete.RemovedAt.Add(today);
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}
