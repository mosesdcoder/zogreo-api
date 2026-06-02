using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Notifications;

public class AfricasTalkingSmsSender(HttpClient http, IConfiguration config, ILogger<AfricasTalkingSmsSender> logger)
    : ISmsSender
{
    public async Task SendAsync(string to, string message, CancellationToken ct = default)
    {
        var sanitized = new string(message.Where(c => c < 128).ToArray());
        if (sanitized.Length > 160) sanitized = sanitized[..160];

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = config["AfricasTalking:Username"]!,
            ["to"] = to,
            ["message"] = sanitized,
            ["from"] = config["AfricasTalking:SenderId"] ?? "Zogreo",
        });

        try
        {
            var resp = await http.PostAsync("version1/messaging", form, ct);
            if (!resp.IsSuccessStatusCode)
                logger.LogWarning("AT SMS failed {Status} to {To}", resp.StatusCode, to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AT SMS exception to {To}", to);
        }
    }

    public static void Configure(IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<ISmsSender, AfricasTalkingSmsSender>(c =>
        {
            c.BaseAddress = new Uri("https://api.africastalking.com/");
            c.DefaultRequestHeaders.Add("apiKey", config["AfricasTalking:ApiKey"]);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
        });
    }
}
