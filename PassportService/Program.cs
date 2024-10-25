using PassportService.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPassportServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<PassportUpdateTimeSettings>(builder.Configuration.GetSection("PassportUpdate"));
builder.Services.Configure<CsvFileSettings>(builder.Configuration.GetSection("CsvFileSettings"));

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

