using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Notifications;

public class SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(config["Email:SmtpHost"])
            {
                Port = int.Parse(config["Email:SmtpPort"] ?? "587"),
                Credentials = new NetworkCredential(config["Email:SmtpUser"], config["Email:SmtpPass"]),
                EnableSsl = true,
            };
            var from = new MailAddress(config["Email:FromAddress"]!, config["Email:FromName"]);
            using var msg = new MailMessage(from, new MailAddress(to))
            {
                Subject = subject, Body = body, IsBodyHtml = false
            };
            await client.SendMailAsync(msg, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP send failed to {To}", to);
        }
    }
}
