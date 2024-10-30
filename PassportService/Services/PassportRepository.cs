using Microsoft.EntityFrameworkCore;
using PassportService.Core;
using PassportService.Infrastructure;
using System.Linq;
using EFCore.BulkExtensions;

namespace PassportService.Services
{
    public class PassportRepository :IPassportRepository
    {
        public DateTime today = DateTime.UtcNow;     
        private PassportDbContext _dbContext;

        public PassportRepository( PassportDbContext dbContext)
        {          
            _dbContext = dbContext;        
        }

        public Task<List<Passport>> GetAllPassports()
        {
            return _dbContext.Passports.ToListAsync();
        }

        public Task<List<Passport>> GetPassportsByNumber(int Number)
        {       
            return _dbContext.Passports
             .Where(p => p.Number == Number)
             .ToListAsync();
        }

        public Task<List<Passport>> GetPassportsBySeries(int Series)
        {
            return _dbContext.Passports
                 .Where(p => p.Series == Series)
                 .ToListAsync();
        }

        public Task<List<Passport>> GetInactivePassportsBySeries(int Series)
        {
            return _dbContext.Passports
                    .Where(p => p.Series == Series &&
                        (p.RemovedAt == null || !p.RemovedAt.Any()
                        || p.CreatedAt.Any() && p.CreatedAt.Max() > p.RemovedAt.Max())
                    ).ToListAsync();
        }

        public Task<List<Passport>> GetInactivePassportsByNumber(int Number)
        {
            return _dbContext.Passports
                  .Where(p => p.Number == Number &&
                     (p.RemovedAt == null || !p.RemovedAt.Any()
                     || p.CreatedAt.Any() && p.CreatedAt.Max() > p.RemovedAt.Max())
                 ).ToListAsync();
        }

        public async Task<List<Passport>> GetPassportsByDate(DateTime date)
        {
            var passportsByDate = await _dbContext.Passports
              .Where(passport =>
                        passport.CreatedAt.Any(createdDate => createdDate.Date == date.Date)
                        ||
                        passport.RemovedAt != null && passport.RemovedAt.Any(removedDate => removedDate.HasValue && removedDate.Value.Date == date.Date))
              .ToListAsync();
            return passportsByDate;
        }

        public  Task<Passport?> GetPassportAsync(Passport passport)
        {           
            return _dbContext.Passports
               .Where(existing => existing.Series == passport.Series && existing.Number == passport.Number).FirstOrDefaultAsync(); ;
        }

        public async Task<List<Passport>?> GetPassportsThatAreInDbAndInCollection(IEnumerable<Passport> passports)
        {
            var seriesNumbers = passports
                .Select(p => p.Series + p.Number)
                .ToList();

           // Выполняем запрос к базе данных, чтобы получить уже существующие паспорта
           var existingPassports = await _dbContext.Passports
               .Where(p => seriesNumbers.Contains(p.Series + p.Number))
               .ToListAsync();         

            return existingPassports;
        }

        public async Task UpdatePassport(Passport passport)
        {
            _dbContext.Update(passport);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdatePassports(List<Passport> passports)
        {
            await _dbContext.BulkUpdateAsync(passports);
        }

        public async Task AddPassportsAsync(List<Passport> passports)
        {
            await _dbContext.BulkInsertAsync(passports);
        }

        //public Task UpdatePassports(List<Passport> passports)
        //{
        //    _dbContext.UpdateRange(passports);     
        //    return _dbContext.SaveChangesAsync();
        //}

        //public async Task AddPassportsAsync(List<Passport> passports)
        //{
        //    await _dbContext.Passports.AddRangeAsync(passports);
        //    await _dbContext.SaveChangesAsync();
        //}

        public Task<List<Passport>> SerchDeletePassports()
        {
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
