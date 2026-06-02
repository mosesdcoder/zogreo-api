# Zogreo Admissions API

Backend API for the Zogreo Bible & Technical Training Institute admissions and payments portal.

Built with .NET 8, Clean Architecture + CQRS, PostgreSQL, Hangfire, and Paystack.

---

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 8.x | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8) |
| PostgreSQL | 14+ | [postgresql.org](https://www.postgresql.org/download/) |
| Redis | — | **Not needed in Development** — in-memory cache is used automatically |

---

## Quick start (3 steps)

### 1. Point at your local Postgres

Open `src/Zogreo.Api/appsettings.Development.json` and set your Postgres password:

```json
"ConnectionStrings": {
  "Postgres": "Host=localhost;Port=5432;Database=zogreo_dev;Username=postgres;Password=YOUR_PW"
}
```

That's the only change needed to run locally. Everything else — JWT key, seed admin password, file storage path — already has working dev defaults in that file.

### 2. Apply migrations

Install the EF CLI tool once per machine:

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"   # add to ~/.zprofile to make permanent
```

Then apply:

```bash
dotnet ef database update -p src/Zogreo.Infrastructure -s src/Zogreo.Api
```

### 3. Run

```bash
dotnet run --project src/Zogreo.Api
```

Open **http://localhost:5000/swagger** — the API is live.

---

## Config layering

| What | Where | Committed? |
|------|-------|-----------|
| Local DB password, JWT key, admin seed password | `appsettings.Development.json` | ✅ Yes — dev-only values, no production risk |
| Real Paystack / AfricasTalking / SMTP credentials | `dotnet user-secrets` or env vars | ❌ Never commit |
| Production everything | Environment variables | ❌ Never in files |

To override any value without editing files:

```bash
# Option A: user-secrets (local machine, dev only)
cd src/Zogreo.Api
dotnet user-secrets set "Paystack:SecretKey" "sk_test_..."

# Option B: environment variable (any environment)
export Paystack__SecretKey="sk_test_..."   # double underscore = colon in config
```

---

## After first run

| URL | What you get |
|-----|-------------|
| `http://localhost:5000/swagger` | Full interactive API docs with JWT auth |
| `http://localhost:5000/health` | Health check |
| `http://localhost:5000/hangfire` | Background job dashboard |

### Try it in Swagger

1. **POST** `/auth/signup` — enter any name, email, phone, password
2. Response includes `data.devOtp` — copy that code
3. **POST** `/auth/verify-otp` — enter phone + code
4. Copy `data.token` from the response
5. Click **Authorize** (top right), paste the token
6. All secured endpoints are now accessible

### Seeded admin account

| Field | Value |
|-------|-------|
| Email | `admin@zogreo.ac.ke` |
| Password | `Admin@1234` |

Use `POST /auth/login` to get an admin JWT, then authorize in Swagger to access `/admin/*` endpoints.

---

## Migrations (when the model changes)

```bash
# Add a new migration
dotnet ef migrations add <Name> -p src/Zogreo.Infrastructure -s src/Zogreo.Api

# Apply
dotnet ef database update -p src/Zogreo.Infrastructure -s src/Zogreo.Api

# List
dotnet ef migrations list -p src/Zogreo.Infrastructure -s src/Zogreo.Api
```

---

## Simulating a Paystack webhook locally

```bash
PAYLOAD='{"event":"charge.success","data":{"reference":"YOUR_REFERENCE","amount":50000,"fees":1500,"channel":"mobile_money","gateway_response":"Successful"}}'

SIG=$(echo -n "$PAYLOAD" | openssl dgst -sha512 -hmac "sk_test_YOUR_KEY" | awk '{print $2}')

curl -X POST http://localhost:5000/webhooks/paystack \
  -H "Content-Type: application/json" \
  -H "x-paystack-signature: $SIG" \
  -d "$PAYLOAD"
```

Post the same payload twice → only one credit (idempotent by design).

---

## Project layout

```
src/
  Zogreo.Domain/          Entities, Enums, domain exceptions — no framework deps
  Zogreo.Application/     Commands, Queries, Handlers, Interfaces — no EF, no HTTP
  Zogreo.Infrastructure/  EF Core, Paystack, SMS, Email, File storage, PDF, Jobs
  Zogreo.Api/             Thin controllers, Middleware, Swagger, Program.cs
```
