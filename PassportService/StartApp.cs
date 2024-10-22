using Microsoft.EntityFrameworkCore;
using PassportService.Infrastructure;
using PassportService.Services;

namespace PassportService
{
    public class StartApp
    {
        public static void Start(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<PassportDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IPassportRepository, Services.PassportRepository>();
            builder.Services.AddScoped<ICsvPassportLoaderService, CsvPassportLoaderService>();
            builder.Services.AddHostedService<PassportUpdateService>();
        }
    }
}
