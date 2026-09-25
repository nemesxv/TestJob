using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TestJob.Api.Models;
using TestJob.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true)
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(ProcessPageResponse.Error(ErrorCodes.ValidationError,
            "Некорректное тело JSON-запроса.")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IValidator<ProcessPageRequest>, ProcessPageRequestValidator>();
builder.Services.AddScoped<IPageProcessingService, PageProcessingService>();
builder.Services.AddScoped<IElementRepository, ElementRepository>();
builder.Services.AddSingleton(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Строка подключения 'Postgres' не настроена.");

    return NpgsqlDataSource.Create(connectionString);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TestJob API — версия 1");
});

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/api/swagger"));
app.Run();

public partial class Program;
