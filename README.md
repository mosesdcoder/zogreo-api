# Zogreo Admissions API

Backend Web API for the Zogreo Bible & Technical Training Institute admissions and payments portal.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- PostgreSQL 14+
- Redis (optional in Development — in-memory cache is used instead)

## Quick start

```bash
# 1. Clone and enter the repo
git clone <repo-url>
cd zogreo_institute

# 2. Set up user secrets (never commit real keys)
cd src/Zogreo.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=zogreo_dev;Username=postgres;Password=YOUR_PW"
dotnet user-secrets set "Jwt:Key" "your-32-char-minimum-secret-key-here"
dotnet user-secrets set "Paystack:SecretKey" "sk_test_..."
dotnet user-secrets set "Seed:AdminPassword" "Admin@1234"

# 3. Apply migrations
dotnet ef database update

# 4. Run
dotnet run

# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger  (Development only)
# Hangfire dashboard at http://localhost:5000/hangfire  (Development only)
```

## Project structure

```
src/Zogreo.Api/
  Program.cs
  appsettings.json / appsettings.Development.json
  Domain/            Enums.cs, Entities.cs
  Data/              AppDbContext.cs, SeedData.cs, Migrations/
  Common/
    Tenancy/         ITenantProvider, TenantProvider, TenantMiddleware
    Errors/          AppException, ExceptionMiddleware
  Features/
    Auth/
    Catalog/
    Applications/
    Documents/
    Payments/
    Admin/
    Students/
    Notifications/
```

## Build

```bash
dotnet build
```

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Zogreo.Api
dotnet ef database update --project src/Zogreo.Api
```
