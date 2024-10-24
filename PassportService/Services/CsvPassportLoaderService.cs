using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PassportService.Configuration;
using PassportService.Core;
using System.Globalization;
using System.IO.Compression;

namespace PassportService.Services
{
    public class CsvPassportLoaderService :ICsvPassportLoaderService
    {
        public DateTime today = DateTime.UtcNow;
        private readonly ILogger<PassportRepository> _logger;
        IConfiguration _configuration;
        IPassportRepository _passportService;
        private readonly IOptions<CsvFileSettings> _options;

        public CsvPassportLoaderService(IOptions<CsvFileSettings> options, IPassportRepository passportService, IConfiguration configuration, ILogger<PassportRepository> logger)
        {
            _passportService = passportService;
            _configuration = configuration;
            _logger = logger;
            _options = options;
        }

        private async Task<string> DownloadCsvFileAsync()
        {
            string url = _options.Value.CsvZipFileUrl;       
            using(var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var tempFilePath = Path.GetTempFileName(); 

                await using(var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                return tempFilePath; 
            }
        }

        public async Task<string> UnpackingCSVFile()
        {
            string pathToZipFile = await DownloadCsvFileAsync();          
            string pathToCSVFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(pathToCSVFolder);
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

            string pathToCSVFile = await UnpackingCSVFile();

            var passports = new List<Passport>();
            const int batchSize = 1000; // Размер партии для добавления в БД
            var batch = new List<Passport>(batchSize);

            try
            {
                using(var reader = new StreamReader(pathToCSVFile))
                {
                    using(var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        await foreach(var record in csv.GetRecordsAsync<PassportCsvModel>())
                        {
                            var passport = new Passport
                            {
                                Series = record.PASSP_SERIES,
                                Number = record.PASSP_NUMBER                                                        
                            };
                            batch.Add(passport);

                            // Добавляем записи в БД
                            if(batch.Count >= batchSize)
                            {
                                await AddPassports(batch);
                                batch.Clear();
                            }
                        }
                    }
                }
                // Добавляем оставшиеся записи, если они есть
                if(batch.Count > 0)
                {
                    await AddPassports(batch);
                }
            }
            catch(FileNotFoundException ex)
            {
                _logger.LogError($"Ошибка: CSV файл не найден. Путь: {ex.FileName}");
                throw new FileNotFoundException($"Ошибка: CSV файл не найден. Путь: {ex.FileName}", ex);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Произошла непредвиденная ошибка при работе с CSV файлом: {ex.Message}", ex);
                throw new Exception($"Произошла непредвиденная ошибка при работе с CSV файлом: {ex.Message}", ex);
            }
            //проверяем удаленные записи
            await _passportService.UpdateDeletedPassportAsync();
        }


        public async Task AddPassports(IEnumerable<Passport> newPassports)
        {
            List<Passport> newPassportsForAdd = new List<Passport>();
            List<Passport>? oldPassports = await _passportService.GetPassportsThatAreInDbAndInCollection(newPassports);
           
            if(oldPassports == null || !oldPassports.Any()) //если все паспорта новые 
            {
                await AddNewPassportsInDb(newPassports.ToList());                
            }
            else if(oldPassports.Count == newPassports.Count())    //если все старые    
            {
                await AddPassportsThatAreInDb(oldPassports);
            }
            else
            {
                newPassportsForAdd = newPassports
                        .Where(p => !oldPassports
                            .Any(ep => ep.Series == p.Series && ep.Number == p.Number))
                        .ToList();

               await AddNewPassportsInDb(newPassportsForAdd);
               await AddPassportsThatAreInDb(oldPassports);
            }
        }

        public async Task AddNewPassportsInDb(List<Passport> newPassports) //добавляем новые паспорта
        {
            foreach(Passport passport in newPassports)
            {
                if(passport.CreatedAt == null)
                {
                    passport.CreatedAt = new List<DateTime>();
                }
                passport.CreatedAt.Add(today);
                passport.DateLastRequest = today;
            }
            await _passportService.AddPassportsAsync(newPassports);
        }

        public async Task AddPassportsThatAreInDb(List<Passport> oldPassports)
        {//добавляем паспорта, которые были в БД (Они могут быть в файле, а могли быть удалены из БД и вновь появились в файле)
            foreach(Passport passport in oldPassports)
            {
                passport.DateLastRequest = today; // Обновляем дату последнего обнаружения в файле
                if(passport.RemovedAt != null && passport.RemovedAt.Any())
                {
                    if(passport.RemovedAt.Max() > passport.CreatedAt.Max()) //если добавили паспорт, который удаляли (дата удаления позже даты создания)
                    {                                                   //в коллекцию дат Добавления добавляем сегодняюшнюю дату
                        passport.CreatedAt.Add(today);
                    }
                }             
            }
            await _passportService.UpdatePassports(oldPassports);// Обновляем объект в контексте
        }
    }
}
