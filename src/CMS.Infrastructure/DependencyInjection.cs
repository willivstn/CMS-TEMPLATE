using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer: EF Core DbContext, Identity, caching,
/// email, file storage, background jobs and other external-facing services.
/// Concrete implementations here are bound to Application-layer interfaces so
/// the rest of the system depends only on abstractions.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Persistence, Identity, caching, email, storage and jobs are registered
        // here as their respective phases land. Kept as a single entry point so
        // the Web composition root has one call: services.AddInfrastructure(config).
        return services;
    }
}
