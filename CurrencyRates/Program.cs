using Microsoft.EntityFrameworkCore;
using CurrencyRates.Data;
using CurrencyRates.Services;
using CurrencyRates.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Currency Rates API",
        Version = "v1",
        Description = "API do pobierania kursów walut z NBP"
    });
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for NBP Service
builder.Services.AddHttpClient<INbpService, NbpService>();
builder.Services.AddScoped<INbpService, NbpService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder
            .WithOrigins("http://localhost:3000") // To jest domyœlny port dla aplikacji React
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Jeœli bêdziemy u¿ywaæ ciasteczek lub autoryzacji
    });
});

builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();