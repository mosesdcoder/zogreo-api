using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Zogreo.Infrastructure.Persistence.Interceptors;

namespace Zogreo.Infrastructure.Persistence;

// Used by `dotnet ef` at design time only — not registered in DI.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Zogreo.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            config.GetConnectionString("Postgres"),
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        // Stub tenant provider for migrations (no real tenant needed)
        var tenantStub = new StubTenantProvider();
        return new ApplicationDbContext(optionsBuilder.Options, tenantStub);
    }

    private class StubTenantProvider : Application.Common.Interfaces.ITenantProvider
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid? UserId => null;
        public string? UserRole => null;
        public void SetTenant(Guid organizationId, Guid? userId, string? userRole) { }
    }
}
