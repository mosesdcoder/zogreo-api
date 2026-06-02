using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Infrastructure.Documents;
using Zogreo.Infrastructure.Files;
using Zogreo.Infrastructure.Identity;
using Zogreo.Infrastructure.Jobs;
using Zogreo.Infrastructure.Notifications;
using Zogreo.Infrastructure.Payments;
using Zogreo.Infrastructure.Persistence;
using Zogreo.Infrastructure.Persistence.Interceptors;

namespace Zogreo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        // ── Interceptor (needs ITenantProvider — register first) ──────────────
        services.AddScoped<AuditSaveChangesInterceptor>();

        // ── DbContext ─────────────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(config.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitialiser>();

        // ── Distributed Cache ─────────────────────────────────────────────────
        if (env.IsDevelopment())
            services.AddDistributedMemoryCache();
        else
            services.AddStackExchangeRedisCache(o => o.Configuration = config.GetConnectionString("Redis"));

        // ── Hangfire ──────────────────────────────────────────────────────────
        services.AddHangfire(hf => hf
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(config.GetConnectionString("Postgres"))));
        services.AddHangfireServer();

        // ── Identity ──────────────────────────────────────────────────────────
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOtpService, OtpService>();

        // ── Payments ──────────────────────────────────────────────────────────
        PaystackHttpClient.ConfigureHttpClient(services, config);
        services.AddScoped<IPaymentSettings, PaymentSettings>();

        // ── Notifications ─────────────────────────────────────────────────────
        AfricasTalkingSmsSender.Configure(services, config);
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificationOutbox, NotificationOutbox>();
        services.AddScoped<NotificationDispatchJob>();

        // ── Files + Documents ─────────────────────────────────────────────────
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IOfferLetterGenerator, QuestPdfOfferLetterGenerator>();

        // ── Jobs ──────────────────────────────────────────────────────────────
        services.AddScoped<PaymentSweepJob>();

        return services;
    }
}
