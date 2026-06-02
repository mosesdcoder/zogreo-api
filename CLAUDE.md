# CLAUDE.md — Zogreo Admissions API

> This file is the source of truth for building this project. Read it fully before writing code, and obey the **Hard Rules** without exception. Detailed functional specs live in `/docs/SPEC.md` (full system) and `/docs/PHASE1-BUILD-SPEC.md` (what we build now). When this file and a prompt disagree, ask before proceeding.

## What we are building (Phase 1 only)

The **admissions + payments API** for Zogreo Bible & Technical Training Institute (Nairobi). It takes a person from "Apply Now" through application, document verification, a gated admission process, fee payments, and conversion into an enrolled student record. SIS depth, LMS, and finance dashboards are **later phases** — do not build them now, but leave the data model able to grow into them.

This is a backend Web API only. No frontend in this repo.

## Tech stack (use these exact choices and versions)

- **.NET 8** (`net8.0`), C#, ASP.NET Core Web API, controllers (not minimal APIs), `Nullable` enabled, `ImplicitUsings` enabled, file-scoped namespaces.
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x + EF Core 8.x (code-first migrations).
- **Auth:** JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 8.x). Passwords hashed with `PasswordHasher<T>` from `Microsoft.AspNetCore.Identity`.
- **OTP store / cache:** `IDistributedCache` — in-memory in Development, Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`) in Production.
- **Background jobs:** Hangfire (`Hangfire.AspNetCore` + `Hangfire.PostgreSql`).
- **PDF (offer letters):** QuestPDF.
- **API docs:** Swashbuckle (Swagger UI in Development).
- **Payments:** Paystack (M-Pesa STK + card) via typed `HttpClient`. M-Pesa direct Daraja is **not** used in Phase 1 — Paystack handles M-Pesa so we get automatic split payments.

## Architecture

A **modular monolith** — one project, organized by feature. Do **not** create microservices.

```
src/Zogreo.Api/
  Program.cs
  appsettings.json / appsettings.Development.json
  Domain/            Enums.cs, Entities.cs (POCOs, no logic)
  Data/              AppDbContext.cs, SeedData.cs, Migrations/
  Common/
    Tenancy/         ITenantProvider, TenantProvider, TenantMiddleware
    Errors/          AppException, ExceptionMiddleware
  Features/
    Auth/            controller, service, JwtTokenService, OtpService, DTOs
    Catalog/         programs + intakes (read)
    Applications/    controller, service, ApplicationStateMachine, DTOs
    Documents/       controller, service, IFileStorage + LocalFileStorage
    Payments/        controller, PaymentService, PaystackClient, WebhookController, PaymentSweepJob, DTOs
    Admin/           AdminApplications, AdminPayments, OfferService, IOfferLetterGenerator, DTOs
    Students/        StudentService
    Notifications/   NotificationOutbox, NotificationDispatchJob, ISmsSender (AfricasTalking), IEmailSender, Templates
```

### Conventions
- Controllers are **thin** — they call a feature service and return its result. Business logic lives in services.
- DTOs are `record` types, defined next to the controller that uses them. Never expose entities directly.
- All services are `Scoped`. Inject `ITenantProvider` for org/user context.
- Throw `AppException` for expected business failures (it maps to the right 4xx). `ExceptionMiddleware` handles all errors → clean JSON `{ "error": "..." }`.
- Money is `decimal`, `numeric(12,2)` in Postgres, in KES.
- Secrets come from configuration / environment / user-secrets — **never hard-code keys**.

## Domain model (build exactly this)

Enums (`Domain/Enums.cs`):
- `Role`: Applicant, Registrar, Bursar, SuperAdmin
- `ApplicationStatus`: Draft, Submitted, UnderReview, NeedsInfo, DocsVerified, OfferMade, OfferAccepted, FeesPaid, MedicalsCleared, Enrolled, Rejected, Withdrawn
- `DocumentStatus`: Pending, Verified, Rejected, NeedsResubmission
- `DocumentType`: NationalIdOrPassport, AcademicCertificate, PassportPhoto, MedicalReport, Other
- `OfferStatus`: Issued, Accepted, Declined, Expired
- `ProgramLevel`: Certificate, Diploma, AdvancedDiploma, BibleCollege
- `DeliveryMode`: Online, OnCampus, Blended
- `FeeCode`: Application, Acceptance, Admission, Medicals, Technology, Tuition(reserved)
- `InvoiceStatus`: Unpaid, PartiallyPaid, Paid, Void
- `PaymentStatus`: Pending, Success, Failed
- `PaymentChannel`: Mpesa, Card, Other
- `NotificationChannel`: Sms, Email — `NotificationStatus`: Queued, Sent, Failed
- `StudentStatus`: Active, Deferred, Graduated, Withdrawn

Entities (`Domain/Entities.cs`). Every tenant-scoped entity derives from `TenantEntity { Guid Id; Guid OrganizationId; DateTimeOffset CreatedAt; UpdatedAt; }`:
- **Organization** (tenancy root; NOT tenant-scoped): Id, Name, Slug(unique), PaystackSubaccountCode?, AdmissionNumberPrefix, Active, CreatedAt
- **User**: Role, Email, Phone, PhoneVerified, PasswordHash, FullName, Active
- **Program**: Name, Level, Mode, DurationLabel, Description?, Active
- **Intake**: ProgramId, Name, OpensAt, ClosesAt, StartsAt, Capacity?, Active
- **Application**: UserId, ProgramId, IntakeId, Status, PersonalJson, EducationHistoryJson, NextOfKinJson, HowDidYouHear?, SubmittedAt?, DecidedByUserId?, DecidedAt?, DecisionReason?
- **Document**: ApplicationId, Type, FileUrl, OriginalFileName, Status, ReviewedByUserId?, ReviewedAt?, ReviewReason?
- **Offer**: ApplicationId, Status, LetterUrl?, Conditions?, IssuedAt, ExpiresAt, AcceptedAt?
- **FeeType**: Code, Name, Amount, Refundable, Active (unique per (Org, Code))
- **Invoice**: ApplicationId, FeeTypeId, FeeCode, Amount, AmountPaid, Status, DueAt?
- **Payment**: InvoiceId, Reference(unique), Provider, Channel, Status, AmountGross, ProviderFee, TechnologyFee, AmountNetToSchool, ProviderRef?, AuthorizationUrl?, RawPayload?, CompletedAt?
- **Student**: ApplicationId, UserId, AdmissionNumber(unique per Org), Status, EnrolledAt
- **AuditLog**: ActorUserId?, Action, Entity, EntityId, Before?, After?, At
- **Notification**: UserId?, Channel, To, Template, Subject, Body, Status, Error?, SentAt?

## Application state machine + gates (server-enforced)

Allowed transitions only (a central `ApplicationStateMachine` validates every change):
```
Draft -> Submitted        (requires Application fee paid)
Submitted -> UnderReview
UnderReview -> NeedsInfo | DocsVerified
NeedsInfo -> UnderReview
UnderReview/DocsVerified -> OfferMade (registrar) | Rejected (registrar, +reason)
OfferMade -> OfferAccepted (applicant accepts AND Acceptance fee paid)
OfferAccepted -> FeesPaid  (Admission + Technology fees paid)
FeesPaid -> MedicalsCleared (Medicals fee paid AND MedicalReport doc Verified)
MedicalsCleared -> Enrolled (Student record + admission number created)
```
Gates the API must refuse to skip: no Submitted without application fee; acceptance/admission/medicals/technology invoices only exist after OfferMade; no Student created before MedicalsCleared. Every transition writes an AuditLog row.

## Payments — the rules that must not break

1. **Unique reference** per payment = `{orgId}:{invoiceId}:{8-char nonce}`. It is the reconciliation anchor.
2. **Idempotent webhook** (`POST /webhooks/paystack`): verify the `x-paystack-signature` (HMAC-SHA512 of the raw body with the secret key); look up Payment by reference; **if already Success, return 200 and do nothing**. Never double-credit or double-advance state. The webhook resolves its own tenant from the Payment row (bypass tenant filter via `IgnoreQueryFilters`).
3. **Split routing by fee code:** when paying a `Technology` fee, settle to the **platform/main Paystack account** (your revenue). For all **school fees** (Application/Acceptance/Admission/Medicals), settle to the **school's Paystack subaccount** (`Organization.PaystackSubaccountCode`), with the school bearing the Paystack fee (`bearer=subaccount`). Capture gross / provider fee / technology fee / net-to-school on every Payment row. (The mechanism must also support a capped-% per-transaction model later via Paystack `transaction_charge` — leave a clear hook, but default to the flat-Technology-fee model now.)
4. **Poll + sweep fallback:** expose `GET /payments/{reference}/status` (verifies against Paystack) and a Hangfire recurring job that re-checks any `Pending` payment every few minutes and reconciles. No payment is ever silently lost.
5. **Never store or log card data.** Use Paystack hosted checkout / its M-Pesa STK flow. We only handle phone numbers and references.

## API surface (Phase 1)

- Auth: `POST /auth/signup`, `/auth/verify-otp`, `/auth/resend-otp`, `/auth/login`
- Catalog: `GET /programs`, `GET /programs/{id}`, `GET /intakes?programId=`
- Applications: `POST /applications`, `PATCH /applications/{id}`, `POST /applications/{id}/submit`, `GET /applications/me`, `GET /applications/{id}`
- Documents: `POST /applications/{id}/documents`, `GET /applications/{id}/documents`
- Payments: `GET /applications/{id}/invoices`, `POST /payments/initiate`, `GET /payments/{reference}/status`, `POST /webhooks/paystack`
- Admin: `GET /admin/applications`, `GET /admin/applications/{id}`, `PATCH /admin/documents/{id}`, `POST /admin/applications/{id}/request-info`, `POST /admin/applications/{id}/offer`, `POST /admin/applications/{id}/reject`, `GET /admin/payments`, `GET /admin/students`, `GET /admin/fee-types`, `PATCH /admin/fee-types/{id}`, `GET /admin/audit`

Admin endpoints require role Registrar/Bursar/SuperAdmin (use `[Authorize(Roles=...)]`). Applicant endpoints require an authenticated, phone-verified user who owns the application.

## Tenancy (multi-tenant-ready, single-tenant launch)

Every read is org-scoped by an EF Core **global query filter** on `OrganizationId == _tenant.OrganizationId`. `TenantMiddleware` resolves the tenant per request: JWT `org` claim → else `X-Org-Slug` header → else the seeded default org. On save, `OrganizationId` and timestamps are stamped automatically in `AppDbContext.SaveChangesAsync`, which also writes AuditLog rows for audited entities. Zogreo is seeded as org #1 (`DefaultOrganization:Slug = "zogreo"`).

## Build order (one slice at a time; each must run before the next)

0. Scaffold: solution, project, dependencies, `Program.cs` wiring, appsettings, docs.
1. Skeleton: entities, DbContext + tenancy + audit, auth + OTP, migrations, seed, health check.
2. Applications + Documents + Catalog.
3. Payments: Paystack client, initiate, idempotent webhook, status, sweep job.
4. Admin review + document verification + Notifications (outbox + SMS + email).
5. Offers + offer-letter PDF + fee gates + Student conversion + reconciliation/audit endpoints.

## How to verify (run after every slice)
- `dotnet build` must succeed with no errors.
- `dotnet ef migrations add <Name>` then `dotnet ef database update` must apply cleanly.
- `dotnet run`, open Swagger at `/swagger`, exercise the slice's endpoints.
- For Slice 3: prove the webhook is idempotent (POST the same event twice → one credit) and that a missed webhook is recovered by the sweep job.

## Out of scope for Phase 1 (do not build)
Tuition / instalment plans / scholarships, full finance dashboards, courses / enrolment-per-term / transcripts / ID cards, LMS / Moodle, attendance / timetabling, mobile app, certificates, analytics.
