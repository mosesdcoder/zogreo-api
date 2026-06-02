# ARCHITECTURE.md — Clean Architecture + CQRS (lite)

> This supersedes the "Architecture" and "Conventions" sections of `CLAUDE.md`. Everything else in `CLAUDE.md` (domain model, **Hard Rules**, state machine, payment rules, API surface, out-of-scope) is layer-independent and still holds — obey it. When refactoring, **preserve every Hard Rule**; this document only changes *where code lives and how requests flow*, not the behaviour.

## Decision & scope

- **Clean Architecture** (4 layers, dependencies point inward toward the Domain).
- **CQRS-lite:** every use case is a **Command** (writes) or **Query** (reads) with its own handler. Controllers do nothing but dispatch.
- **Deliberately NOT doing** (over-engineering at this scale): separate read/write databases, event sourcing, a distributed message bus. One PostgreSQL, in-process dispatch. Revisit only if scale ever demands it.

## Mediator choice (no MediatR dependency)

MediatR's newer versions are commercially licensed, so we use a **tiny hand-rolled dispatcher** (zero NuGet, no licensing). Define this in the Application layer:

```csharp
public interface ICommand<TResult> { }
public interface IQuery<TResult> { }
public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{ Task<TResult> Handle(TCommand command, CancellationToken ct); }
public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{ Task<TResult> Handle(TQuery query, CancellationToken ct); }

public interface ISender
{ Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default);
  Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default); }
```

`Sender` resolves the handler from DI by closed generic type and invokes it, running a **validation behavior** first (FluentValidation if a validator is registered for the request). Register handlers + validators by assembly scan in DI. (If you'd rather not hand-roll, **Cortex.Mediator** (MIT) is a free drop-in — but the hand-rolled version is ~1 file and fully yours.)

## The four projects (dependencies point inward)

```
Zogreo.Domain          → no project references
Zogreo.Application     → Domain                       (+ FluentValidation)
Zogreo.Infrastructure  → Application, Domain           (EF Core, Npgsql, Hangfire, QuestPDF, Redis, JWT, Paystack HttpClient)
Zogreo.Api  (Web)      → Application, Infrastructure   (composition root only; Swashbuckle)
```

Rule: **Domain knows nothing. Application knows only Domain. Infrastructure and Web depend on Application.** Web references Infrastructure *only* to register services in `Program.cs` — controllers never touch Infrastructure types directly, only Application abstractions + the `ISender`.

### Zogreo.Domain
Pure business model, no framework code.
```
Entities/        (Organization, User, Application, Document, Offer, FeeType, Invoice, Payment, Student, AuditLog, Notification)
Enums/
Exceptions/      (DomainException, InvalidStateTransitionException)
Common/          (TenantEntity base, value objects e.g. AdmissionNumber — optional)
```
**Rich domain:** the application state machine moves *into* the `Application` entity as guarded methods — `Submit()`, `MoveToReview()`, `MarkDocsVerified()`, `MakeOffer()`, `AcceptOffer()`, `MarkFeesPaid()`, `ClearMedicals()`, `Reject(reason)`. Each validates the current `Status` and throws `InvalidStateTransitionException` on an illegal move, then sets the new status. This is the single enforcement point for the ladder + gates in CLAUDE.md. (The old `ApplicationStateMachine` service collapses into these methods.)

### Zogreo.Application
Use cases + abstractions. No EF, no HTTP, no Paystack SDK — only interfaces.
```
Common/
  Behaviors/           (ValidationBehavior)
  Interfaces/          (IApplicationDbContext, ICurrentUser, ITenantProvider, IPaystackClient,
                        ISmsSender, IEmailSender, IFileStorage, IOfferLetterGenerator,
                        IOtpService, IJwtTokenService, INotificationOutbox)
  Exceptions/          (AppException + NotFound/Forbidden/Conflict)
  Mediator/            (ICommand/IQuery/handlers/ISender contracts above)
Features/
  Auth/        Commands/ (Signup, VerifyOtp, ResendOtp, Login)        + Validators
  Applications/Commands/ (CreateDraft, SaveApplicationStep, SubmitApplication)
               Queries/  (GetMyApplications, GetApplicationById)
  Documents/   Commands/ (UploadDocument)  Queries/ (GetDocuments)
  Catalog/     Queries/  (GetPrograms, GetProgram, GetIntakes)
  Payments/    Commands/ (InitiatePayment, ApplyPaymentConfirmation)  Queries/ (GetInvoices, GetPaymentStatus)
  Admin/       Commands/ (ReviewDocument, RequestInfo, MakeOffer, RejectApplication, UpdateFeeType)
               Queries/  (GetApplicationsQueue, GetApplicationDetail, GetReconciliation, GetStudents, GetFeeTypes, GetAudit)
  Offers/      Commands/ (AcceptOffer)  Queries/ (GetOffer)
```
`IApplicationDbContext` exposes the `DbSet`s + `SaveChangesAsync` so handlers query/write without referencing the concrete DbContext.

### Zogreo.Infrastructure
All the "how". Implements every Application interface.
```
Persistence/
  ApplicationDbContext   (implements IApplicationDbContext; tenant global query filters;
                          SaveChanges stamping; AuditSaveChangesInterceptor)
  Configurations/        (IEntityTypeConfiguration<T> per entity)
  Interceptors/          (AuditSaveChangesInterceptor)
  Migrations/            (moved here from the old project)
  ApplicationDbContextInitialiser (the old SeedData)
Identity/                (JwtTokenService, OtpService over IDistributedCache)
Payments/                (PaystackClient : IPaystackClient)
Notifications/           (AfricasTalkingSmsSender, SmtpEmailSender, NotificationOutbox, NotificationDispatchJob)
Files/                   (LocalFileStorage : IFileStorage — seam for S3/Spaces)
Documents/               (QuestPdfOfferLetterGenerator : IOfferLetterGenerator)
Jobs/                    (PaymentSweepJob)
DependencyInjection.cs   (AddInfrastructure extension)
```

### Zogreo.Api (Web / Presentation)
```
Controllers/             (thin — each action builds a Command/Query and calls ISender)
Middleware/              (ExceptionMiddleware, TenantMiddleware)
CurrentUser/             (HttpContextCurrentUser : ICurrentUser, ITenantProvider — reads JWT/headers)
Program.cs               (AddApplication() + AddInfrastructure() + web wiring)
appsettings*.json
```

## CQRS conventions

- One command/query = one file = one handler. Name by intent: `SubmitApplicationCommand` + `SubmitApplicationCommandHandler`.
- Commands return a small result (an id, a DTO) — not an entity.
- Queries are read-only, return DTOs/projections, and should use `AsNoTracking()`.
- Validation lives in a FluentValidation validator next to the command; the `ValidationBehavior` runs it before the handler. Business-rule failures still throw `AppException`; illegal state transitions throw `InvalidStateTransitionException` (mapped to 409 by the middleware).
- Controllers: `var result = await _sender.Send(new SubmitApplicationCommand(id), ct); return Ok(result);` — nothing more.

## Where the Hard Rules live now (must stay intact)

- **Multi-tenancy:** `ITenantProvider`/`ICurrentUser` interfaces in Application; `HttpContextCurrentUser` + `TenantMiddleware` in Web; the EF global query filters + `OrganizationId` stamping in the Infrastructure DbContext. Unchanged behaviour.
- **Audit log:** an `AuditSaveChangesInterceptor` in Infrastructure (cleaner than overriding SaveChanges) — same audited entity set, same skip of PasswordHash/RawPayload.
- **Application state machine + gates:** now the guarded methods on the `Application` domain entity. Handlers call `application.Submit()` etc.; they never set `Status` directly.
- **Idempotent webhook:** becomes the `ApplyPaymentConfirmationCommand` handler — still idempotent (no-op if Payment already Success), still resolves tenant from the Payment row via `IgnoreQueryFilters`. The webhook controller and the `PaymentSweepJob` both dispatch this *same* command, so the idempotency logic exists once.
- **Payment split routing by fee code, poll+sweep fallback, never store card data:** in `PaystackClient` (Infrastructure) + the payment command handlers (Application). Unchanged.

## Old → new mapping (for the refactor)

| Generated now (modular monolith) | Moves to |
|---|---|
| `Domain/Entities.cs`, `Enums.cs` | **Domain** (split into files; add guarded transition methods + domain exceptions) |
| `Features/Applications/ApplicationStateMachine.cs` | **Domain** — folded into `Application` entity methods |
| `Common/Errors/AppException` | **Application/Common/Exceptions**; `ExceptionMiddleware` → **Web/Middleware** |
| `Common/Tenancy/ITenantProvider` | interface → **Application**; resolver → **Web/CurrentUser**; query filter → **Infrastructure** |
| `Data/AppDbContext` | **Infrastructure/Persistence** (+ `IApplicationDbContext` in Application); audit → interceptor |
| `Data/SeedData` | **Infrastructure** `ApplicationDbContextInitialiser` |
| `Features/*/Service.cs` business logic | **Application** Commands/Queries + handlers (split read vs write) |
| `Features/Auth/JwtTokenService`, `OtpService` | **Infrastructure/Identity** (interfaces in Application) |
| `Features/Payments/PaystackClient` | **Infrastructure/Payments** (`IPaystackClient` in Application) |
| `Features/Documents/LocalFileStorage` | **Infrastructure/Files** (`IFileStorage` in Application) |
| `Features/Admin/QuestPdfOfferLetterGenerator` | **Infrastructure/Documents** (`IOfferLetterGenerator` in Application) |
| `Features/Notifications/*` senders + dispatch job | **Infrastructure/Notifications** (`ISmsSender`/`IEmailSender`/`INotificationOutbox` in Application) |
| `Features/*/Controller.cs` | **Web/Controllers** — thinned to dispatch `ISender` |
| `Program.cs` | **Web** — composes `AddApplication()` + `AddInfrastructure()` |

## Migrations across assemblies (don't lose your schema)

The DbContext now lives in Infrastructure, so:
- **Move** the existing `Migrations/` folder into `Zogreo.Infrastructure/Persistence/Migrations` and update the namespace in those files + the `ModelSnapshot`.
- EF commands now specify the project + startup project:
  `dotnet ef migrations add <Name> -p src/Zogreo.Infrastructure -s src/Zogreo.Api`
  `dotnet ef database update -p src/Zogreo.Infrastructure -s src/Zogreo.Api`
- After moving, run `dotnet ef migrations list` and confirm **no pending model changes** (the refactor should be behaviour-preserving). Only add a new migration if the model genuinely changed (e.g., you split an entity). Goal: **no destructive migration**, the existing database keeps working.
```
