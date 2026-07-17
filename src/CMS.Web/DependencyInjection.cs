using CMS.Application.Common.Abstractions;
using CMS.Application.Identity;
using CMS.Domain.Entities.Identity;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Persistence;
using CMS.Web.Authorization;
using CMS.Web.Email;
using CMS.Web.Identity;
using CMS.Web.Jobs;
using CMS.Web.Realtime;
using CMS.Web.Security;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using IAppAuthenticationService = CMS.Application.Identity.IAuthenticationService;

namespace CMS.Web;

/// <summary>
/// Registers Web-layer services: ASP.NET Core Identity (with the custom sign-in
/// manager and claims factory), the permission-based authorization pipeline, and
/// the HttpContext-backed <see cref="ICurrentUser"/> that supersedes the
/// Infrastructure default during requests.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSignalR();
        services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

        // HttpContext-backed current user overrides the Infrastructure system default.
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAppAuthenticationService, AuthenticationService>();
        services.AddScoped<CMS.Application.Identity.IIdentityService, IdentityService>();
        services.AddScoped<IEncryptionService, DataProtectionEncryptionService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Database-driven, permission-filtered navigation.
        services.AddScoped<CMS.Web.Navigation.INavigationService, CMS.Web.Navigation.NavigationService>();

        // File storage (local by default) and the upload/thumbnail pipeline.
        services.AddScoped<CMS.Application.Common.Abstractions.IFileStorage, CMS.Web.Storage.LocalFileStorage>();
        services.AddScoped<CMS.Web.Storage.MediaUploadService>();

        // Email: queue writer supersedes the Infrastructure logging sender; SMTP dispatcher drains it.
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailSender, QueuedEmailSender>();
        services.AddScoped<SmtpEmailDispatcher>();

        // Background jobs (Hangfire on SQL Server) and the recurring job classes.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.FromSeconds(15),
            }));
        services.AddHangfireServer();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<ScheduledContentPublisher>();
        services.AddScoped<EmailQueueProcessor>();
        services.AddScoped<CleanupJob>();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddSignInManager<AppSignInManager>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "cms.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        // Refresh claims (including permissions) at least this often.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(30));

        // Permission-based authorization: on-demand policies + claim-checking handler.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        return services;
    }
}
