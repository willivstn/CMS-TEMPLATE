using CMS.Application.Common.Abstractions;
using CMS.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CMS.Infrastructure.Persistence;

/// <summary>
/// Enables the EF Core tools (<c>dotnet ef migrations</c>) to construct the
/// context at design time without booting the web host. The connection string is
/// only needed for <c>database update</c>; migration scaffolding does not connect.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=CmsTemplate;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options, new DesignTimeTenantContext());
    }

    /// <summary>Fixed single-tenant context used only for design-time model building.</summary>
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid CurrentTenantId => DomainDefaults.DefaultTenantId;
        public bool IsMultiTenantEnabled => false;
    }
}
