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
        private readonly ILogger<PassportService> _logger;
        IConfiguration _configuration;
        IPassportRepository _passportService;

        public CsvPassportLoaderService(IPassportRepository passportService , IConfiguration configuration, ILogger<PassportService> logger)
        {
            _passportService = passportService;
            _configuration = configuration;    
            _logger = logger;
        }

        public void UnpackingCSVFile()
        {
            string pathToZipFile = _configuration.GetConnectionString("CSVFilePath");
            string pathToCSVFolder = _configuration.GetConnectionString("CSVFileFolder");     
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
        }
        public string GetPathToUnpackCSVFile()
        {
            string pathToFolderWhithCSVFile = _configuration.GetConnectionString("CSVFileFolder");
            string pathToCSVFile = Directory.GetFiles(pathToFolderWhithCSVFile, "*.csv").FirstOrDefault();
            if(string.IsNullOrEmpty(pathToCSVFile))
            {
                throw new FileNotFoundException("CSV-файл не найден в распакованной папке.", pathToFolderWhithCSVFile);
            }
            return pathToCSVFile;
        }
     
        public async Task LoadPassportsFromCsvAsync()
        {
            UnpackingCSVFile();
            string pathToCSVFile = GetPathToUnpackCSVFile();
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
            await _passportService.UpdateDeletedPassportAsync();
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
                    exists.DateLastRequest = passport.DateLastRequest; // Обновляем поля CreatedAt и RemovedAt                
                    await _passportService.UpdatePassport(exists);// Обновляем объект в контексте                  
                }
            }
            if(passportsToAdd.Any())
            {
                await _passportService.AddPassportsAsync(passportsToAdd);
            }
        }  
    }
}
