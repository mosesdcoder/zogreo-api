using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Auth.DTOs;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Auth.Commands;

public record SignupCommand(string FullName, string Email, string Phone, string Password)
    : ICommand<SignupResult>;

public class SignupCommandValidator : AbstractValidator<SignupCommand>
{
    public SignupCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Password).MinimumLength(6);
    }
}

public class SignupCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    IOtpService otp,
    INotificationOutbox outbox,
    IEnvironmentInfo envInfo) : ICommandHandler<SignupCommand, SignupResult>
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<SignupResult> Handle(SignupCommand cmd, CancellationToken ct)
    {
        var phone = PhoneNormalizer.Normalize(cmd.Phone);

        var exists = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.OrganizationId == tenant.OrganizationId
                        && (u.Email == cmd.Email.Trim().ToLowerInvariant() || u.Phone == phone), ct);
        if (exists)
            throw AppException.Conflict("A user with that email or phone already exists.");

        var user = new User
        {
            OrganizationId = tenant.OrganizationId,
            FullName = cmd.FullName,
            Email = cmd.Email.Trim().ToLowerInvariant(),
            Phone = phone,
            Role = Role.Applicant,
            Active = true
        };
        user.PasswordHash = _hasher.HashPassword(user, cmd.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var code = await otp.GenerateAndStoreAsync(phone);
        await outbox.QueueSmsAsync(user.Id, phone, "otp",
            $"Your Zogreo verification code is {code}. Valid for 10 minutes.");

        var exposeOtp = envInfo.IsDevelopment || envInfo.ExposeOtp;
        return new SignupResult("OTP sent to your phone.", exposeOtp ? code : null);
    }
}
