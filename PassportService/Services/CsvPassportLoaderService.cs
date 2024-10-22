using CsvHelper;
using Microsoft.EntityFrameworkCore;
using PassportService.Core;
using System.Globalization;
using System.IO.Compression;

namespace PassportService.Service
{
    public class CsvPassportLoaderService :ICsvPassportLoaderRepository
    {
        public DateTime today = DateTime.UtcNow;
        private readonly ILogger<PassportService> _logger;
        IConfiguration _configuration;
        IPassportRepository _passportService;

        public CsvPassportLoaderService(IPassportRepository passportService, IConfiguration configuration, ILogger<PassportService> logger)
        {
            _passportService = passportService;
            _configuration = configuration;
            _logger = logger;
        }

        public string UnpackingCSVFile()
        {
            string pathToZipFile = _configuration.GetConnectionString("CSVFilePath");
            string pathToCSVFolder = _configuration.GetConnectionString("CSVFileFolder");
            try
            {
                ZipFile.ExtractToDirectory(pathToZipFile, pathToCSVFolder, true);
                //находим наш файл, который был созданы последним
                string? pathToCSVFile = Directory.GetFiles(pathToCSVFolder, "*.csv")
                                    .OrderByDescending(f => File.GetLastWriteTime(f))
                                    .FirstOrDefault();
                if(string.IsNullOrEmpty(pathToCSVFile))
                {
                    throw new FileNotFoundException();
                }
                return pathToCSVFile;
            }
            catch(FileNotFoundException)
            {
                _logger.LogError("ZIP-файл или CVS-файл в распакованной папке не найден.", pathToZipFile);
                throw new FileNotFoundException("ZIP-файл или CVS-файл в распакованной папке не найден.", pathToZipFile);
            }
            catch(InvalidDataException ex)
            {
                _logger.LogError($"Ошибка: файл не является допустимым ZIP-файлом. {ex.Message}");
                throw new InvalidDataException($"Ошибка: файл не является допустимым ZIP-файлом. {ex.Message}");
            }
            catch(IOException ex)
            {
                _logger.LogError($"Ошибка ввода-вывода: {ex.Message}");
                throw new IOException($"Ошибка ввода-вывода: {ex.Message}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Общая ошибка при разархивации: {ex.Message}");
                throw new Exception($"Общая ошибка при разархивации: {ex.Message}");
            }
        }

        public async Task LoadPassportsFromCsvAsync()
        {
            string pathToCSVFile = UnpackingCSVFile();

            var passports = new List<Passport>();
            const int batchSize = 1000; // Размер партии для добавления в БД
            var batch = new List<Passport>(batchSize);

            try
            {
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
                    exists.DateLastRequest = today; // Обновляем дату последнего обнаружения в файле
                    if(exists.RemovedAt != null && exists.RemovedAt.Any())
                    {
                        if(exists.RemovedAt.Max() > exists.CreatedAt.Max()) //если добавили паспорт, который удаляли (дата удаления позже даты создания)
                        {                                                   //в коллекцию дат Добавления добавляем сегодняюшнюю дату
                            exists.CreatedAt.Add(today);
                        }
                    }
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
