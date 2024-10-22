using CsvHelper;
using Microsoft.EntityFrameworkCore;
using PassportService.Core;
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

        public CsvPassportLoaderService(IPassportRepository passportService, IConfiguration configuration, ILogger<PassportService> logger)
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

        public async Task LoadPassportsFromCsvAsync()
        {
            UnpackingCSVFile();
            string pathToFolderWhithCSVFile = _configuration.GetConnectionString("CSVFileFolder");
            string pathToCSVFile = Directory.GetFiles(pathToFolderWhithCSVFile, "*.csv").FirstOrDefault();

            var passports = new List<Passport>();
            const int batchSize = 1000; // Размер партии для добавления в БД
            var batch = new List<Passport>(batchSize);

            try
            {
                using(var reader = new StreamReader(pathToCSVFile))
                //using(var reader = new StreamReader(@"c://file.csv"))
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
            }
            catch(FileNotFoundException ex)
            {
                throw new FileNotFoundException($"Ошибка: CSV файл не найден. Путь: {ex.FileName}", ex);
            }
            catch(Exception ex)
            {
                throw new Exception($"Произошла непредвиденная ошибка при работе с CSV файлом: {ex.Message}", ex);
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
