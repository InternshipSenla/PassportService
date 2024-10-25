using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PassportService.Infrastructure;
using PassportService.Services;
using System.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPassportServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PassportDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPassportRepository, PassportRepository>();
        services.AddScoped<ICsvPassportLoaderService, CsvPassportLoaderService>();
        services.AddHostedService<PassportUpdateService>();
        ApplyMigrations(services);
        return services;
    }

    private static void ApplyMigrations(IServiceCollection services)
    { 
        var serviceProvider = services.BuildServiceProvider();

        using(var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PassportDbContext>();              
            dbContext.Database.EnsureCreated();
        }
    }
}