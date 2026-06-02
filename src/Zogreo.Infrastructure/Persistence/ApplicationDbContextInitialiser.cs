using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Infrastructure.Persistence;

public class ApplicationDbContextInitialiser(
    ApplicationDbContext db, IConfiguration config,
    ILogger<ApplicationDbContextInitialiser> logger)
{
    public async Task InitialiseAsync()
    {
        try { await db.Database.MigrateAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Migration failed"); throw; }
    }

    public async Task SeedAsync()
    {
        var slug = config["DefaultOrganization:Slug"] ?? "zogreo";
        var org = await db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Slug == slug);

        if (org == null)
        {
            org = new Organization
            {
                Name = config["DefaultOrganization:Name"] ?? "Zogreo Bible & Technical Training Institute",
                Slug = slug,
                AdmissionNumberPrefix = config["DefaultOrganization:AdmissionPrefix"] ?? "ZBTTI",
                Active = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
        }

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@zogreo.ac.ke";
        var admin = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.OrganizationId == org.Id && u.Email == adminEmail);

        if (admin == null)
        {
            admin = new User
            {
                OrganizationId = org.Id,
                FullName = "System Admin",
                Email = adminEmail,
                Phone = config["Seed:AdminPhone"] ?? "+254700000000",
                PhoneVerified = true,
                Role = Role.SuperAdmin,
                Active = true
            };
            admin.PasswordHash = new PasswordHasher<User>()
                .HashPassword(admin, config["Seed:AdminPassword"] ?? "Admin@1234");
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        var now = DateTimeOffset.UtcNow;
        var existingPrograms = await db.Programs.IgnoreQueryFilters()
            .Where(p => p.OrganizationId == org.Id).ToListAsync();

        var programs = new[]
        {
            ("Certificate in Biblical Studies",         ProgramLevel.Certificate,    DeliveryMode.OnCampus, "1 Year"),
            ("Diploma in Theology",                     ProgramLevel.Diploma,         DeliveryMode.OnCampus, "2 Years"),
            ("Advanced Diploma in Christian Ministry",  ProgramLevel.AdvancedDiploma, DeliveryMode.Blended,  "3 Years"),
            ("Bible College Degree",                    ProgramLevel.BibleCollege,    DeliveryMode.OnCampus, "4 Years"),
            ("Certificate in Information Technology",   ProgramLevel.Certificate,    DeliveryMode.OnCampus, "1 Year"),
            ("Diploma in Business Management",          ProgramLevel.Diploma,         DeliveryMode.Blended,  "2 Years"),
        };

        foreach (var (name, level, mode, duration) in programs)
        {
            if (existingPrograms.Any(p => p.Name == name)) continue;
            var prog = new Domain.Entities.Program
            {
                OrganizationId = org.Id, Name = name, Level = level, Mode = mode,
                DurationLabel = duration, Active = true, CreatedAt = now, UpdatedAt = now
            };
            db.Programs.Add(prog);
            await db.SaveChangesAsync();

            db.Intakes.Add(new Intake
            {
                OrganizationId = org.Id, ProgramId = prog.Id,
                Name = $"January {now.Year + 1} Intake",
                OpensAt = now, ClosesAt = now.AddMonths(3), StartsAt = now.AddMonths(4),
                Active = true, CreatedAt = now, UpdatedAt = now
            });
            await db.SaveChangesAsync();
        }

        var existingFees = await db.FeeTypes.IgnoreQueryFilters()
            .Where(f => f.OrganizationId == org.Id).ToListAsync();

        var fees = new[]
        {
            (FeeCode.Application, "Application Fee", 500m,   false),
            (FeeCode.Acceptance,  "Acceptance Fee",  5000m,  false),
            (FeeCode.Admission,   "Admission Fee",   10000m, false),
            (FeeCode.Medicals,    "Medical Fee",     2000m,  false),
            (FeeCode.Technology,  "Technology Fee",  1500m,  false),
        };

        foreach (var (code, name, amount, refundable) in fees)
        {
            if (existingFees.Any(f => f.Code == code)) continue;
            db.FeeTypes.Add(new FeeType
            {
                OrganizationId = org.Id, Code = code, Name = name, Amount = amount,
                Refundable = refundable, Active = true, CreatedAt = now, UpdatedAt = now
            });
        }
        await db.SaveChangesAsync();
    }
}

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}
