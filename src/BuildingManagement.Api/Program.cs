using BuildingManagement.Api;
using BuildingManagement.Application;
using BuildingManagement.Domain;
using BuildingManagement.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("BuildingManagement");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:BuildingManagement is required.");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(); builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Building Management API",
        Version = "v1",
        Description = "Physical structure APIs for locations, complexes, buildings, and units."
    });
});
var fileStorageOptions = builder.Configuration.GetSection("FileStorage").Get<FileStorageOptions>() ?? new FileStorageOptions();
builder.Services.AddSingleton(fileStorageOptions);
builder.Services.AddSingleton<IFileStorage>(new LocalFileStorage(fileStorageOptions, builder.Environment.ContentRootPath));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PhysicalStructureService>();
builder.Services.AddScoped<PartyOccupancyService>();
builder.Services.AddScoped<FileManagementService>();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddHealthChecks().AddDbContextCheck<BuildingManagementDbContext>("database");

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;
    await next();
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Building Management API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
    });
}
app.MapHealthChecks("/health");
app.MapPhysicalStructureEndpoints();
app.MapFileManagementEndpoints();
app.MapPartyOccupancyEndpoints();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedDevelopmentData"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
    await db.Database.MigrateAsync();
    await DevelopmentSeeder.SeedAsync(db, CancellationToken.None);
}
app.Run();

public partial class Program;
