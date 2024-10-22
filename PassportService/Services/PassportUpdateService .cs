namespace PassportService.Services
{
    public class PassportUpdateService :BackgroundService
    {
        private readonly ILogger<PassportUpdateService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public PassportUpdateService(ILogger<PassportUpdateService> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                var updateTime = _configuration.GetValue<TimeSpan>("PassportUpdate:TimeOfDay");
                var currentTime = DateTime.UtcNow.TimeOfDay;

                // Проверяем, что текущее время равно или на 1 минуту больше, чем заданое время обновления
                if(currentTime >= updateTime && currentTime < updateTime.Add(TimeSpan.FromMinutes(1)))
                {
                    using(var scope = _scopeFactory.CreateScope())
                    {
                        // Получаем зависимость сервиса для обновления паспортов
                        var passportLoaderService = scope.ServiceProvider.GetRequiredService<ICsvPassportLoaderService>();

                        try
                        {
                            // Выполняем обновление паспортов
                            _logger.LogInformation("Запуск обновления паспортов.");
                            await passportLoaderService.LoadPassportsFromCsvAsync();
                            _logger.LogInformation("Обновление паспортов завершено.");
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, "Ошибка при обновлении паспортов.");
                        }
                    }
                }

                // Ждем 1 минуту перед следующей проверкой, чтобы попасть в нужный нам промежуток времени и выполнить задачу 1 раз
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

}
