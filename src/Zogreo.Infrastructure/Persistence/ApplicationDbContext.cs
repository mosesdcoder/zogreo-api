using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Domain.Common;
using Zogreo.Domain.Entities;
using AppEntity = Zogreo.Domain.Entities.Application;

namespace Zogreo.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenant)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Domain.Entities.Program> Programs => Set<Domain.Entities.Program>();
    public DbSet<Intake> Intakes => Set<Intake>();
    public DbSet<AppEntity> Applications => Set<AppEntity>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<FeeType> FeeTypes => Set<FeeType>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // Apply entity configurations from this assembly
        m.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters
        m.Entity<User>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Domain.Entities.Program>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Intake>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<AppEntity>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Document>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Offer>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<FeeType>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Invoice>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Payment>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Student>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);
        m.Entity<Notification>().HasQueryFilter(e => e.OrganizationId == tenant.OrganizationId);

        // Application entity: Status has private setter — map via backing field
        m.Entity<AppEntity>()
            .Property(a => a.Status)
            .HasColumnName("Status");
        m.Entity<AppEntity>()
            .Property(a => a.SubmittedAt)
            .HasColumnName("SubmittedAt");
        m.Entity<AppEntity>()
            .Property(a => a.DecidedAt)
            .HasColumnName("DecidedAt");

        // Organization
        m.Entity<Organization>().HasIndex(o => o.Slug).IsUnique();

        // User
        m.Entity<User>().HasIndex(u => new { u.OrganizationId, u.Email }).IsUnique();

        // FeeType
        m.Entity<FeeType>().HasIndex(f => new { f.OrganizationId, f.Code }).IsUnique();
        m.Entity<FeeType>().Property(f => f.Amount).HasColumnType("numeric(12,2)");

        // Invoice
        m.Entity<Invoice>().Property(i => i.Amount).HasColumnType("numeric(12,2)");
        m.Entity<Invoice>().Property(i => i.AmountPaid).HasColumnType("numeric(12,2)");

        // Payment
        m.Entity<Payment>().HasIndex(p => p.Reference).IsUnique();
        m.Entity<Payment>().Property(p => p.AmountGross).HasColumnType("numeric(12,2)");
        m.Entity<Payment>().Property(p => p.ProviderFee).HasColumnType("numeric(12,2)");
        m.Entity<Payment>().Property(p => p.TechnologyFee).HasColumnType("numeric(12,2)");
        m.Entity<Payment>().Property(p => p.AmountNetToSchool).HasColumnType("numeric(12,2)");

        // Student
        m.Entity<Student>().HasIndex(s => new { s.OrganizationId, s.AdmissionNumber }).IsUnique();

        // AuditLog
        m.Entity<AuditLog>().HasIndex(a => new { a.OrganizationId, a.At });
    }
}
