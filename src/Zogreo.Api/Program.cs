using System.Reflection;
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Zogreo.Api.CurrentUser;
using Zogreo.Api.Filters;
using Zogreo.Api.Middleware;
using Zogreo.Application;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Infrastructure;
using Zogreo.Infrastructure.Jobs;
using Zogreo.Infrastructure.Notifications;
using Zogreo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var env = builder.Environment;

QuestPDF.Settings.License = LicenseType.Community;

// ── Controllers (with global response filter) ─────────────────────────────────
builder.Services.AddControllers(options =>
    options.Filters.Add<ApiResponseFilter>());
builder.Services.AddHttpContextAccessor();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Zogreo Admissions API",
        Version     = "v1",
        Description = "Admissions, documents, payments and student management API for " +
                      "Zogreo Bible & Technical Training Institute (Nairobi).\n\n" +
                      "## Authentication\n" +
                      "1. `POST /auth/signup` → receive `devOtp` in Development\n" +
                      "2. `POST /auth/verify-otp` → receive JWT token\n" +
                      "3. Click **Authorize** above, paste the token\n\n" +
                      "## Response format\n" +
                      "Every response follows the same envelope:\n" +
                      "```json\n{ \"success\": true, \"statusCode\": 200, \"message\": \"OK\", \"data\": { ... }, \"errors\": null }\n```",
        Contact = new OpenApiContact
        {
            Name  = "Zogreo Institute",
            Email = "dev@zogreo.ac.ke"
        }
    });

    // JWT bearer auth
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Paste the JWT token from `POST /auth/verify-otp` or `POST /auth/login`.\n\nFormat: **Bearer {token}**"
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

    // Include XML doc comments from the Web project
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    // Group endpoints by controller tag
    c.TagActionsBy(api => [api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Other"]);
    c.DocInclusionPredicate((_, _) => true);
});

// ── JWT Auth ──────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = config["Jwt:Issuer"],
            ValidAudience            = config["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("https://zogreo.online", "https://www.zogreo.online")
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

// ── Tenancy / CurrentUser ─────────────────────────────────────────────────────
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// ── Application + Infrastructure layers ──────────────────────────────────────
builder.Services.AddApplication(env.IsDevelopment(), config);
builder.Services.AddInfrastructure(config, env);

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
// Trust X-Forwarded-* headers from Nginx (must come before everything else)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Error handler must be first so it catches everything below
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zogreo API v1");
    c.RoutePrefix  = "swagger";           // http://localhost:5000/swagger
    c.DocumentTitle = "Zogreo API";
    c.DefaultModelsExpandDepth(-1);       // Collapse schema models by default
    c.DisplayRequestDuration();           // Show request timing in UI
});

app.UseCors();

if (env.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.UseHangfireServer();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();   // ← JWT decoded first, HttpContext.User is populated
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>(); // ← reads org/userId from the decoded JWT

app.MapControllers();

// Health check — manually envelope since it's a minimal-API endpoint (not a controller)
app.MapGet("/health", () => Results.Ok(new
{
    success = true, statusCode = 200, message = "OK",
    data = new { status = "healthy", ts = DateTimeOffset.UtcNow },
    errors = (object?)null
})).WithTags("Health").WithSummary("Health check");

// ── Seed database on startup ──────────────────────────────────────────────────
await app.Services.InitialiseDatabaseAsync();

// ── Hangfire recurring jobs ───────────────────────────────────────────────────
try
{
    RecurringJob.AddOrUpdate<PaymentSweepJob>(
        "payment-sweep", j => j.RunAsync(), "*/5 * * * *");
    RecurringJob.AddOrUpdate<NotificationDispatchJob>(
        "notification-dispatch", j => j.RunAsync(), "*/1 * * * *");
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not register Hangfire recurring jobs on startup — will retry on next deploy");
}

app.Run();
