using Microsoft.EntityFrameworkCore;
using PassportService.Infrastructure;
using PassportService.Service;

namespace PassportService
{
    public class StartApp
    {
        public static void Start(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<PassportDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IPassportRepository, PassportService.Service.PassportService>();
        }
    }
}
