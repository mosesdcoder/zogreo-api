using Zogreo.Domain.Entities;
using AppEntity = Zogreo.Domain.Entities.Application;

namespace Zogreo.Application.Common.Interfaces;

public interface IOtpService
{
    Task<string> GenerateAndStoreAsync(string phone);
    Task<bool> VerifyAndConsumeAsync(string phone, string code);
}

public interface IJwtTokenService
{
    string Generate(User user);
}

public interface IPaystackClient
{
    Task<PaystackInitResult> InitializeTransactionAsync(PaystackInitRequest req);
    Task<PaystackVerifyResult?> VerifyTransactionAsync(string reference);
}

public record PaystackInitRequest(
    string Email, long Amount, string Reference,
    string[] Channels, string? Subaccount, string? Bearer, string? MobileNumber = null);

public record PaystackInitResult(bool Status, string AuthorizationUrl, string Reference, string AccessCode);
public record PaystackVerifyResult(bool Status, string PaystackRef, string GatewayResponse, long Amount, long Fees, string Channel);

public interface ISmsSender
{
    Task SendAsync(string to, string message, CancellationToken ct = default);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public interface IFileStorage
{
    Task<string> SaveAsync(IFileProxy file, string folder, CancellationToken ct = default);
}

// Minimal file abstraction to avoid referencing IFormFile (HTTP concern) from Application
public interface IFileProxy
{
    string FileName { get; }
    string ContentType { get; }
    long Length { get; }
    Task CopyToAsync(Stream target, CancellationToken ct = default);
}

public interface IOfferLetterGenerator
{
    Task<string> GenerateAsync(AppEntity application, Offer offer, CancellationToken ct = default);
}

public interface INotificationOutbox
{
    Task QueueSmsAsync(Guid? userId, string phone, string template, string body);
    Task QueueEmailAsync(Guid? userId, string email, string template, string subject, string body);
}
