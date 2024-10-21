using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PassportService.Core;
using PassportService.Infrastructure;
using System.Globalization;
using System.IO.Compression;

namespace PassportService.Service
{
    public class CsvPassportLoaderService :ICsvPassportLoaderRepository
    {
        private DateTime today = DateTime.UtcNow;        
        private PassportDbContext _dbContext;
        private readonly ILogger<PassportService> _logger;
        IConfiguration _configuration;
        IPassportRepository _passportService;

        public CsvPassportLoaderService(IPassportRepository passportService , IConfiguration configuration, PassportDbContext dbContext, ILogger<PassportService> logger)
        {
            _passportService = passportService;
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public string PathToUnpackCSVFile()
        {
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
            return pathToCSVFile;
        }
        public async Task LoadPassportsFromCsvAsync()
        {
            string pathToCSVFile = PathToUnpackCSVFile();
            var passports = new List<Passport>();

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
                Passport exists = await _passportService.GetPassportAsync(passport);
                if(exists == null)
                {
                    passportsToAdd.Add(passport);
                }
                else
                {
                    exists.DateLastRequest = passport.DateLastRequest; // Например, обновляем поля CreatedAt и RemovedAt                
                    _passportService.UpdatePassport(exists);// Обновляем объект в контексте                  
                }
            }
            if(passportsToAdd.Any())
            {
                await _passportService.AddPasssporsAsync(passportsToAdd);
            }
            // Сохраняем изменения в БД
            await _passportService.SaveChangeDbAsync();
        }

        public async Task UpdateDeletedPassportAsync()
        {
            //вот тут вопрос
            //var passportsToDelete = await _dbContext.Passports
            //         .Where(passport => !passport.DateLastRequest.Date.Equals(today.Date)).ToListAsync();
            var passportsToDelete = await _passportService.SerchDeletePassports();

            foreach(var passportWasDelete in passportsToDelete)
            {
                if(passportWasDelete.RemovedAt == null)
                {
                    passportWasDelete.RemovedAt = new List<DateTime?>();
                }
                // Добавляем текущую дату в коллекцию
                passportWasDelete.RemovedAt.Add(today);
            }
            await _passportService.SaveChangeDbAsync();
        }
    }
}
