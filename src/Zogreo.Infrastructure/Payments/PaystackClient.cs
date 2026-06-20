using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Payments;

public class PaystackHttpClient(HttpClient http) : IPaystackClient
{
    public async Task<PaystackInitResult> InitializeTransactionAsync(PaystackInitRequest req)
    {
        var payload = new
        {
            email = req.Email,
            amount = req.Amount,
            reference = req.Reference,
            channels = req.Channels,
            subaccount = req.Subaccount,
            bearer = req.Bearer,
            mobile_number = req.MobileNumber,   // required for M-Pesa STK push
        };
        var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var resp = await http.PostAsync("transaction/initialize", body);
        resp.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        return new PaystackInitResult(
            doc.RootElement.GetProperty("status").GetBoolean(),
            data.GetProperty("authorization_url").GetString()!,
            data.GetProperty("reference").GetString()!,
            data.GetProperty("access_code").GetString()!);
    }

    public async Task<PaystackVerifyResult?> VerifyTransactionAsync(string reference)
    {
        var resp = await http.GetAsync($"transaction/verify/{Uri.EscapeDataString(reference)}");
        if (!resp.IsSuccessStatusCode) return null;
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        if (!doc.RootElement.GetProperty("status").GetBoolean()) return null;
        var data = doc.RootElement.GetProperty("data");
        return new PaystackVerifyResult(
            true,
            data.GetProperty("reference").GetString()!,
            data.GetProperty("gateway_response").GetString()!,
            data.GetProperty("amount").GetInt64(),
            data.TryGetProperty("fees", out var f) ? f.GetInt64() : 0,
            data.GetProperty("channel").GetString()!);
    }

    public static void ConfigureHttpClient(IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IPaystackClient, PaystackHttpClient>(c =>
        {
            c.BaseAddress = new Uri(config["Paystack:BaseUrl"]!.TrimEnd('/') + "/");
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config["Paystack:SecretKey"]);
        });
    }
}
