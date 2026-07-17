using CMS.Application.Common.Abstractions;
using CMS.Infrastructure.Email;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Multitenancy;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Interceptors;
using CMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer: EF Core persistence with audit/soft-delete
/// and domain-event interceptors, multi-tenancy, and the ambient services the
/// Application layer depends on. Identity, email, storage, caching and jobs are
/// layered on in their respective phases.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(configuration.GetSection(MultiTenancyOptions.SectionName));

        // Ambient services. The Web layer overrides ICurrentUser/ITenantContext with
        // HttpContext-backed implementations; these are the non-request defaults.
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ITenantContext, DefaultTenantContext>();
        services.AddScoped<ICurrentUser, SystemCurrentUser>();

        // SaveChanges interceptors (registered so they can be resolved with their deps).
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure();
            });

            // Benign here: soft-deletable principals (e.g. ApplicationUser) with
            // non-filtered dependents. We never rely on filtered principals being
            // present for these navigations.
            options.ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<DispatchDomainEventsInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<DatabaseInitializer>();

        // Authorization + messaging services (Identity itself is configured by the Web layer).
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();

        // Platform services: caching, settings, feature flags.
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, Caching.MemoryCacheService>();
        services.AddScoped<ISettingService, Configuration.SettingService>();
        services.AddScoped<IFeatureService, Configuration.FeatureService>();

        return services;
    }
}
