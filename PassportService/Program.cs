using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PassportService.Infrastructure;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //builder.Services.AddDbContext<Pas>(options =>
        //     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<PassportDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if(app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        bool firstStart = true;
        app.MapGet("/", async (PassportDbContext db) =>
        {            
                string info = "Данные:\n";
                var passports = await db.Passports.ToListAsync();
                foreach(var passport in passports)
                {
                
                    info += String.Format("|{0,-30}|{1,-10}|", passport.Series, passport.Number) + "\n";
                }
                return info;            
        });

        app.Run();
    }
}