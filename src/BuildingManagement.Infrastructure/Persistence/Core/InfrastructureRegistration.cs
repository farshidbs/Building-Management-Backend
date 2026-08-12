using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BuildingManagementDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(typeof(BuildingManagementDbContext).Assembly.FullName).EnableRetryOnFailure()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<BuildingManagementDbContext>());
        return services;
    }
}
