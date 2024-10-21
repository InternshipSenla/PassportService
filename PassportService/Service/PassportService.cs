using CsvHelper;
using Microsoft.EntityFrameworkCore;
using PassportService.Core;
using PassportService.Infrastructure;
using System.Globalization;
using System.IO.Compression;

namespace PassportService.Service
{
    public class PassportService :IPassportRepository
    {

        private DateTime today = DateTime.UtcNow;
        DbContextOptions<PassportDbContext> _option;
        IConfiguration _configuration;
        private PassportDbContext _dbContext;
        private readonly ILogger<PassportService> _logger;

        public PassportService(DbContextOptions<PassportDbContext> option, IConfiguration configuration, PassportDbContext dbContext, ILogger<PassportService> logger)
        {
            _option = option;
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

        public async Task LoadPassportsFromCsvAsync()
        {
            var passports = new List<Passport>();
            string pathToZipFile = _configuration.GetConnectionString("CSVFilePath");
            string pathToCSVFolder = _configuration.GetConnectionString("CSVFileFolder");
            string pathToCSVFile = "";
            // pathToCSVFile = Directory.GetFiles(pathToCSVFolder, "*.csv").FirstOrDefault();
            try
            {
                if(!File.Exists(pathToZipFile))
                {
                    throw new FileNotFoundException("ZIP-файл не найден.", pathToZipFile);
                }

                if(!Directory.Exists(pathToCSVFolder))
                {
                    Directory.CreateDirectory(pathToCSVFolder);
                }

                ZipFile.ExtractToDirectory(pathToZipFile, pathToCSVFolder, true);
                pathToCSVFile = Directory.GetFiles(pathToCSVFolder, "*.csv").FirstOrDefault();
                if(string.IsNullOrEmpty(pathToCSVFile))
                {
                    throw new FileNotFoundException("CSV-файл не найден в распакованной папке.", pathToCSVFolder);
                }
            }
            catch(InvalidDataException ex)
            {
                _logger.LogError($"Ошибка: файл не является допустимым ZIP-файлом. {ex.Message}");
                throw;
            }
            catch(IOException ex)
            {
                _logger.LogError($"Ошибка ввода-вывода: {ex.Message}");
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError($"Общая ошибка при разархивации: {ex.Message}");
                throw;
            }

            const int batchSize = 1000; // Размер партии для добавления в БД
            var batch = new List<Passport>(batchSize);

            using(var reader = new StreamReader(pathToCSVFile))
            using(var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                await foreach(var record in csv.GetRecordsAsync<PassportCsvRecord>())
                {
                    var passport = new Passport
                    {
                        Series = record.PASSP_SERIES,
                        Number = record.PASSP_NUMBER,
                        CreatedAt = new List<DateTime> { today },
                        DateLastRequest = today
                    };
                    batch.Add(passport);

                    // Добавляем записи в БД
                    if(batch.Count >= batchSize)
                    {
                        await AddPassportsIfNotExistsAsync(batch);
                        batch.Clear();
                    }
                }
            }
            // Добавляем оставшиеся записи, если они есть
            if(batch.Count > 0)
            {
                await AddPassportsIfNotExistsAsync(batch);
            }
            //проверяем удаленные записи
            await UpdateDeletedPassportAsync();
        }

        public async Task AddPassportsIfNotExistsAsync(IEnumerable<Passport> newPassports)
        {
            var passportsToAdd = new List<Passport>();
            foreach(var passport in newPassports)
            {
                // Проверяем, существует ли паспорт в БД     
                var exists = await _dbContext.Passports
                     .Where(existing => existing.Series == passport.Series && existing.Number == passport.Number).FirstOrDefaultAsync();
                if(exists == null)
                {
                    passportsToAdd.Add(passport);
                }
                else
                {
                    exists.DateLastRequest = passport.DateLastRequest; // Например, обновляем поля CreatedAt и RemovedAt
                    _dbContext.Update(exists); // Обновляем объект в контексте
                }
            }
            if(passportsToAdd.Any())
            {
                await _dbContext.Passports.AddRangeAsync(passportsToAdd);
            }
            // Сохраняем изменения в БД
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateDeletedPassportAsync()
        {
            var passportsToDelete = await _dbContext.Passports
                     .Where(passport => !passport.DateLastRequest.Date.Equals(today.Date)).ToListAsync();
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
