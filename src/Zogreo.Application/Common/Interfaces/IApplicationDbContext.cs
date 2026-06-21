using Microsoft.EntityFrameworkCore;
using Zogreo.Domain.Entities;
using AppEntity = Zogreo.Domain.Entities.Application;

namespace Zogreo.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<Domain.Entities.Program> Programs { get; }
    DbSet<Intake> Intakes { get; }
    DbSet<AppEntity> Applications { get; }
    DbSet<Document> Documents { get; }
    DbSet<Offer> Offers { get; }
    DbSet<FeeType> FeeTypes { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Student> Students { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<TimetableEntry> TimetableEntries { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<MoodleUser> MoodleUsers { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
