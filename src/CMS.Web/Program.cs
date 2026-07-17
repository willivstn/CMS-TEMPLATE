using CMS.Application;
using CMS.Infrastructure;
using CMS.Infrastructure.Persistence;
using CMS.Web;
using CMS.Web.Jobs;
using CMS.Web.Logging;
using CMS.Web.Realtime;
using CMS.Web.Security;
using Hangfire;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: console + rolling file for everything, plus a database sink
// (warnings and above) that powers the in-app log viewer.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .WriteTo.File("logs/cms-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14,
        formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .WriteTo.Sink(new DatabaseLogSink(services.GetRequiredService<IServiceScopeFactory>())));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the Clean Architecture layers. Each layer owns its own registration
// so the composition root stays a thin, readable list of capabilities.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

// Apply migrations and seed on startup. Convenient for a starter template; gate
// behind configuration or a CLI step for production deployments if preferred.
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard, restricted to job managers.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthFilter()],
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Attribute-routed REST API controllers (api/v{version}/...).
app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");

// Recurring background jobs.
RecurringJob.AddOrUpdate<ScheduledContentPublisher>("scheduled-content", j => j.PublishDueAsync(), Cron.Minutely);
RecurringJob.AddOrUpdate<EmailQueueProcessor>("email-queue", j => j.ProcessAsync(), Cron.Minutely);
RecurringJob.AddOrUpdate<CleanupJob>("cleanup", j => j.RunAsync(), Cron.Daily);

app.Run();
