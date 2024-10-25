using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PassportService.Infrastructure;
using PassportService.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPassportServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PassportDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPassportRepository, PassportRepository>();
        services.AddScoped<ICsvPassportLoaderService, CsvPassportLoaderService>();
        services.AddHostedService<PassportUpdateService>();

        return services;
    }
}