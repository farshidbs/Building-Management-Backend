using BuildingManagement.Api;
using BuildingManagement.Application;
using BuildingManagement.Domain;
using BuildingManagement.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("BuildingManagement");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:BuildingManagement is required.");

builder.Services.AddProblemDetails();
var iamOptions = builder.Configuration.GetSection("Iam").Get<IamOptions>() ?? new IamOptions();
var iamSecret = builder.Configuration["Iam:Secret"];
if (string.IsNullOrWhiteSpace(iamSecret) && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Iam:Secret is required outside Development.");
iamSecret ??= "development-only-iam-secret-change-before-production";
builder.Services.AddSingleton(iamOptions);
builder.Services.AddSingleton<IIamSecretProtector>(new HmacIamSecretProtector(iamSecret));
builder.Services.AddSingleton<IOtpDelivery, UnconfiguredOtpDelivery>();
builder.Services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, DatabaseBearerHandler>("Bearer", null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(IamEndpoints.CustomerUserPolicy, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("actor_type", "user"))
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
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
builder.Services.AddScoped<ReferenceDataService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<ComplexService>();
builder.Services.AddScoped<BuildingService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddScoped<PartyService>();
builder.Services.AddScoped<PartyContactService>();
builder.Services.AddScoped<UnitPartyRelationService>();
builder.Services.AddScoped<UnitOccupancyService>();
builder.Services.AddScoped<StoredFileService>();
builder.Services.AddScoped<BuildingComplexGalleryService>();
builder.Services.AddScoped<BuildingComplexDocumentService>();
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<AssetEventService>();
builder.Services.AddScoped<AssetFileService>();
builder.Services.AddScoped<FinancialAccountService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<DemandService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ITrustedPaymentResultProcessor>(sp => sp.GetRequiredService<PaymentService>());
builder.Services.AddScoped<FinancialFileService>();
builder.Services.AddScoped<UnitCreditSettlementService>();
builder.Services.AddScoped<IamService>();
builder.Services.AddScoped<RecoveryService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<AccessAuthorizationService>();
builder.Services.AddScoped<ResourceAuthorization>();
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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Building Management API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
    });
}
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;
    await next();
});
app.MapHealthChecks("/health").AllowAnonymous();
app.MapApiEndpoints();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedDevelopmentData"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
    await db.Database.MigrateAsync();
    await DevelopmentSeeder.SeedAsync(db, CancellationToken.None);
    await AssetDevelopmentSeeder.SeedAsync(db, CancellationToken.None);
    await FinancialDevelopmentSeeder.SeedAsync(db, CancellationToken.None);
}
app.Run();

public partial class Program;
