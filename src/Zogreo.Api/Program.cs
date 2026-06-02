using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Zogreo.Api.Common.Errors;
using Zogreo.Api.Common.Tenancy;
using Zogreo.Api.Data;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var env = builder.Environment;

// ── QuestPDF license (community) ──────────────────────────────────────────────
QuestPDF.Settings.License = LicenseType.Community;

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger (Dev only) ────────────────────────────────────────────────────────
if (env.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zogreo Admissions API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT token"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });
}

// ── JWT Auth ──────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

// ── EF Core / Postgres ────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("Postgres")));

// ── Distributed Cache ─────────────────────────────────────────────────────────
if (env.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
        options.Configuration = config.GetConnectionString("Redis"));
}

// ── Hangfire ──────────────────────────────────────────────────────────────────
builder.Services.AddHangfire(hf => hf
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(config.GetConnectionString("Postgres"))));
builder.Services.AddHangfireServer();

// ── Tenancy ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// ── CORS (Dev open) ───────────────────────────────────────────────────────────
if (env.IsDevelopment())
{
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
}

// ── TODO: register feature services (Auth, Catalog, Applications, Documents, Payments, Admin, Students, Notifications) ──

var app = builder.Build();

// ── Static file serving for uploads ──────────────────────────────────────────
app.UseStaticFiles();

// ── Error middleware (first to catch everything) ───────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

// ── Swagger ───────────────────────────────────────────────────────────────────
if (env.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zogreo API v1"));
    app.UseCors();
    app.UseHangfireDashboard("/hangfire");
}

// ── Tenant middleware (after routing, before auth) ────────────────────────────
app.UseRouting();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── Health ────────────────────────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "healthy", ts = DateTimeOffset.UtcNow }));

// ── Seed on startup ───────────────────────────────────────────────────────────
// TODO: await SeedData.RunAsync(app.Services, config);

app.Run();
