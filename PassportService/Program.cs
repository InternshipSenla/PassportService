using Microsoft.AspNetCore.Builder;
using PassportService;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        StartApp.Start(builder);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if(app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.DisplayRequestDuration();
            });
        }     

        app.UseHttpsRedirection();
       
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}