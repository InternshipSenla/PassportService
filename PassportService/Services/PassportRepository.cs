using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using PassportService.Core;
using PassportService.Infrastructure;

namespace PassportService.Services
{
    public class PassportRepository :IPassportRepository
    {
        public DateTime today = DateTime.UtcNow;
        private PassportDbContext _dbContext;
        DbContextOptions<PassportDbContext> _options;

        public PassportRepository(DbContextOptions<PassportDbContext> options, PassportDbContext dbContext)
        {
            _options = options;
            _dbContext = dbContext;
        }

        public Task<List<Passport>> GetAllPassports()
        {
            return _dbContext.Passports.ToListAsync();
        }

        public Task<List<Passport>> GetPassportsByNumber(string Number)
        {
            return _dbContext.Passports
                .Where(p => p.Number == Number)
                .ToListAsync();
        }

        public Task<List<Passport>> GetPassportsBySeries(string Series)
        {
            return _dbContext.Passports
                .Where(p => p.Series == Series)
                .ToListAsync();
        }

        public Task<List<Passport>> GetInactivePassportsBySeries(string Series)
        {
            return _dbContext.Passports
                .Where(p => p.Series == Series &&
                    (p.RemovedAt == null || !p.RemovedAt.Any()
                    || p.CreatedAt.Any() && p.CreatedAt.Max() > p.RemovedAt.Max())
                ).ToListAsync();
        }

        public Task<List<Passport>> GetInactivePassportsByNumber(string Number)
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

        public Task<Passport?> GetPassportAsync(Passport passport)
        {
            return _dbContext.Passports
               .Where(existing => existing.Series == passport.Series && existing.Number == passport.Number).FirstOrDefaultAsync(); ;
        }

        public async Task<List<Passport>?> GetPassportsThatAreInDbAndInCollection(IEnumerable<Passport> passports)
        {
            using var dbContext = new PassportDbContext(_options);
            var seriesNumbers = new HashSet<string>(passports.Select(p => p.Series + p.Number));

            var existingPassports = await dbContext.Passports
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
            using var dbContext = new PassportDbContext(_options);
            await dbContext.BulkUpdateAsync(passports);
        }

        public async Task<bool> AddPassportsAsync(List<Passport> passports)
        {
            using var dbContext = new PassportDbContext(_options);        
            try
            {
                // Пытаемся выполнить массовое добавление всех паспортов
                await dbContext.BulkInsertAsync(passports);
            }
            catch(Npgsql.PostgresException ex) when(ex.SqlState == "23505")
            {
                // Если есть дубликаты, возвращаем false               
                return false;
            }
            return true;
        }

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

        public async Task UpdateDeletedPassportTasks()
        {
            const int pageSize = 20000;
            int? lastProcessedId = null;
            bool hasMoreRecords = true;
            int semaforeCount = 4;
            using var semaphore = new SemaphoreSlim(semaforeCount);
            var tasks = new List<Task>();
            var query = _dbContext.Passports
                 .Where(passport =>
                     !passport.DateLastRequest.Date.Equals(today.Date) &&
                     (passport.RemovedAt == null ||
                      passport.RemovedAt.Any() &&
                      passport.CreatedAt.Max() > passport.RemovedAt.Max()));

            while(hasMoreRecords)
            {
                query = _dbContext.Passports
                 .Where(passport =>
                     !passport.DateLastRequest.Date.Equals(today.Date) &&
                     (passport.RemovedAt == null ||
                      passport.RemovedAt.Any() &&
                      passport.CreatedAt.Max() > passport.RemovedAt.Max()));

                // Если lastProcessedId уже задан, выбираем только записи с Id больше него
                if(lastProcessedId.HasValue)
                {
                    query = query.Where(p => p.Id > lastProcessedId.Value);
                }

                // Получаем очередной пакет данных
                var passportsBatch = await query
                    .OrderBy(p => p.Id) // упорядочивание для последовательной выборки
                    .Take(pageSize)
                    .ToListAsync();

                if(passportsBatch.Count == 0)
                {
                    hasMoreRecords = false;
                    break;
                }

                await semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        // Обрабатываем данные
                        await UpdateDeletedPassports(passportsBatch);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);

                // Устанавливаем последний обработанный Id для следующего цикла
                lastProcessedId = passportsBatch.Last().Id;
            }
            await Task.WhenAll(tasks);
        }

        private async Task UpdateDeletedPassports(List<Passport> passportsBatch)
        {
            foreach(var passport in passportsBatch)
            {
                if(passport.RemovedAt == null)
                {
                    passport.RemovedAt = new List<DateTime?>();
                }
                passport.RemovedAt.Add(today);
            }
            await UpdatePassports(passportsBatch);
        }
    }
}
