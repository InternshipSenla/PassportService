using CsvHelper;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using PassportService.Configuration;
using PassportService.Core;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;

namespace PassportService.Services
{
    public class CsvPassportLoaderService :ICsvPassportLoaderService
    {
        public DateTime today = DateTime.UtcNow;
        private readonly ILogger<PassportRepository> _logger;
        IPassportRepository _passportRepository;
        private readonly IOptions<CsvFileSettings> _options;

        public CsvPassportLoaderService(IOptions<CsvFileSettings> options, IPassportRepository passportRepository, ILogger<PassportRepository> logger)
        {
            _passportRepository = passportRepository;
            _logger = logger;
            _options = options;
        }

        private string GetInputValue(HtmlNode formNode, string inputName)
        {
            var inputNode = formNode.SelectSingleNode($".//input[@name='{inputName}']");
            return inputNode?.GetAttributeValue("value", string.Empty) ?? string.Empty;
        }

        //получаю url для загрузки файла, если появилось окно с проверкой на вирус
        private string? GetDownloadURLAsync(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            _logger.LogInformation($"Полученный HTML: {content}");

            // Извлекаем ссылку на загрузку из формы
            var downloadFormNode = doc.DocumentNode.SelectSingleNode("//form[@id='download-form']");
            if(downloadFormNode != null)
            {
                var actionUrl = downloadFormNode.GetAttributeValue("action", string.Empty);
                var idValue = GetInputValue(downloadFormNode, "id");
                var exportValue = GetInputValue(downloadFormNode, "export");
                var confirmValue = GetInputValue(downloadFormNode, "confirm");
                var uuidValue = GetInputValue(downloadFormNode, "uuid");
                var atValue = GetInputValue(downloadFormNode, "at");

                var downloadUrl = $"{actionUrl}?id={idValue}&export={exportValue}&confirm={confirmValue}&uuid={uuidValue}&at={atValue}";
                _logger.LogInformation($"Ссылка на загрузку: {downloadUrl}");
                return downloadUrl; // Возвращаем ссылку на загрузку
            }
            else
            {
                _logger.LogError("Ссылка на загрузку не найдена в HTML-ответе.");
                return null; // Возвращаем null, если ссылка не найдена
            }
        }

        private string? GetFileNameByContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            string fileName = " "; // Имя по умолчанию
            string fileExtension = " "; // Значение по умолчанию для расширения
            try
            {
                if(contentDisposition != null && !string.IsNullOrEmpty(contentDisposition.FileName))
                {
                    // Извлекаем имя файла и расширение из заголовка
                    fileName = contentDisposition.FileName.Trim('"');
                    fileExtension = Path.GetExtension(contentDisposition.FileName);
                    //fileName = Path.GetFileNameWithoutExtension(fileName);
                    fileExtension = fileExtension.Trim('"');
                    // Проверка расширения
                    if(fileExtension != ".zip" && fileExtension != ".csv")
                    {
                        throw new InvalidDataException("Неподдерживаемый формат файла. Должен быть .zip или .csv.");
                    }
                }
                else
                {
                    return null;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Ошибка при извлечении имени файла: {ex.Message}");
                throw new Exception($"Ошибка при извлечении имени файла: {ex.Message}");
            }

            return fileName;
        }

        //получаю путь к загруженному файлу
        private async Task<string> GetPathToDownloadFileAsync()
        {
            string url = _options.Value.CsvZipFileUrl;
            using(var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Получаем заголовки ответа
                var contentDisposition = response.Content.Headers.ContentDisposition;

                string? fileName = GetFileNameByContentDisposition(contentDisposition);
                var content = await response.Content.ReadAsStringAsync();

                // Если получен HTML - значит страница с проверкой на вирусы
                if(content.Contains("<html>"))
                {
                    url = GetDownloadURLAsync(content);
                    if(url == null) return null; // Если не удалось получить URL, возвращаем null
                    // Выполняем повторный запрос для загрузки файла
                    response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    contentDisposition = response.Content.Headers.ContentDisposition;
                    fileName = GetFileNameByContentDisposition(contentDisposition);

                }
                string fileExtension = Path.GetExtension(fileName);
                fileName = Path.GetFileNameWithoutExtension(fileName);
                fileExtension = fileExtension.Trim('"');

                // Создаем временный файл с правильным расширением
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"{fileName}{fileExtension}");
                await using(var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                return tempFilePath;
            }
        }

        //распаковываю загруженный файл
        public async Task<string> UnpackingCSVFile()
        {
            string pathToZipFile = await GetPathToDownloadFileAsync();
            string fileExtension = Path.GetExtension(pathToZipFile);
            if(fileExtension.Equals(".csv"))
            {
                return pathToZipFile;
            }
            else if(fileExtension.Equals(".zip"))
            {
                string pathToCSVFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(pathToCSVFolder);
                try
                {
                    _logger.LogInformation("Распаковка файла!");
                    ZipFile.ExtractToDirectory(pathToZipFile, pathToCSVFolder, true);

                    // Находим наш файл, который был создан последним
                    string? pathToCSVFile = Directory.GetFiles(pathToCSVFolder, "*.csv")
                                                        .OrderByDescending(f => File.GetLastWriteTime(f))
                                                        .FirstOrDefault();

                    if(string.IsNullOrEmpty(pathToCSVFile))
                    {
                        throw new FileNotFoundException();
                    }

                    _logger.LogInformation("Файл успешно распакован!");
                    return pathToCSVFile;
                }
                catch(FileNotFoundException)
                {
                    _logger.LogError("CVS-файл не найден в распакованной папке.", pathToZipFile);
                    throw new FileNotFoundException("CVS-файл не найден в распакованной папке.", pathToZipFile);
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
            else
            {
                throw new Exception($"Файл не имеет нужного расширения {fileExtension}");
            }
        }

        public async Task LoadPassportsFromCsvAsync()
        {
            string pathToCSVFile = await UnpackingCSVFile();

            var passports = new List<Passport>();
            const int batchSize = 20000;
            var batch = new List<Passport>(batchSize);

            try
            {
                _logger.LogInformation("Работа с CSV файлом!");
                using(var reader = new StreamReader(pathToCSVFile))
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

                        if(batch.Count >= batchSize)
                        {
                            await AddPassports(batch.ToList());
                            batch.Clear();
                        }
                    }
                }

                // Обработка оставшихся паспортов, если они есть
                if(batch.Count > 0)
                {
                    await AddPassports(batch.ToList());
                }
                _logger.LogInformation("Работа с CSV файлом завершена!");
            }
            catch(FileNotFoundException ex)
            {
                _logger.LogError($"Ошибка: CSV файл не найден. Путь: {ex.FileName}");
                throw new FileNotFoundException($"Ошибка: CSV файл не найден. Путь: {ex.FileName}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Произошла непредвиденная ошибка при работе с CSV файлом: {ex.Message}");
                throw new Exception($"Произошла непредвиденная ошибка при работе с CSV файлом: {ex.Message}");
            }

            // Проверяем удаленные записи
            await _passportRepository.UpdateDeletedPassportTasks();
        }  

        public async Task AddPassports(IEnumerable<Passport> newPassports)
        {
            List<Passport> newPassportsList = newPassports.ToList();
            int semaforeCount = 4;
            using var semaphore = new SemaphoreSlim(semaforeCount);
            int batchSize = (newPassportsList.Count() / semaforeCount) + 1;
            var tasks = new List<Task>();

            for(int i = 0; i < newPassports.ToList().Count; i += batchSize)
            {
                var batch = newPassports.Skip(i).Take(batchSize).ToList();

                await semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await AddPassportsInBdTasks(batch);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        public async Task AddPassportsInBdTasks(IEnumerable<Passport> newPassports)
        {
            foreach(Passport passport in newPassports)
            {
                passport.CreatedAt = new List<DateTime> { today };
                passport.DateLastRequest = today;
            }
            List<Passport>? oldPassports = new List<Passport>();
            List<Passport> newPassportsForAdd = new List<Passport>();
            bool notRepeats = await _passportRepository.AddPassportsAsync(newPassports.ToList());
            if(!notRepeats)
            {
                // Если есть дубликаты, находим их и добавляем
                oldPassports = await _passportRepository.GetPassportsThatAreInDbAndInCollection(newPassports);
                newPassportsForAdd = newPassports
                    .Where(p => !oldPassports
                        .Any(ep => ep.Series == p.Series && ep.Number == p.Number))
                    .ToList();
                if(newPassportsForAdd != null && newPassportsForAdd.Any())
                {
                    await _passportRepository.AddPassportsAsync(newPassportsForAdd);
                }
                await AddPassportsThatAreInDb(oldPassports);

            }
        }

        public async Task AddPassportsThatAreInDb(List<Passport> oldPassports)
        {
            foreach(var passport in oldPassports)
            {
                passport.DateLastRequest = today;

                if(passport.RemovedAt != null && passport.RemovedAt.Any())
                {
                    // Проверяем, была ли дата удаления позже даты создания, и при необходимости добавляем текущую дату
                    if(passport.RemovedAt.Max() > passport.CreatedAt.Max())
                    {
                        passport.CreatedAt.Add(today);
                    }
                }
            }
            await _passportRepository.UpdatePassports(oldPassports);
        }
    }
}
