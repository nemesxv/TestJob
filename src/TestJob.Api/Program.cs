using FluentValidation;
using Npgsql;
using TestJob.Api.Data;
using TestJob.Api.Models;
using TestJob.Api.Services;
using TestJob.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IValidator<ProcessPageRequest>, ProcessPageRequestValidator>();
builder.Services.AddScoped<IPageProcessingService, PageProcessingService>();
builder.Services.AddScoped<IElementRepository, ElementRepository>();
builder.Services.AddSingleton(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

    return NpgsqlDataSource.Create(connectionString);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TestJob API v1");
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.Run();

public partial class Program;
