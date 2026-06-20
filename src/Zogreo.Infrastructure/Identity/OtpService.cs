using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Identity;

public class OtpService(IDistributedCache cache, IConfiguration config, IHostEnvironment hostEnv) : IOtpService
{
    private static readonly Random _rng = new();
    private const string DevOtp = "123456";

    public async Task<string> GenerateAndStoreAsync(string phone)
    {
        // In Development, always use the fixed OTP so testing doesn't require SMS.
        var code = hostEnv.IsDevelopment() ? DevOtp : _rng.Next(100000, 999999).ToString();
        var ttl = int.Parse(config["Otp:TtlMinutes"] ?? "10");
        await cache.SetStringAsync(CacheKey(phone), Hash(code),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttl) });
        return code;
    }

    public async Task<bool> VerifyAndConsumeAsync(string phone, string code)
    {
        // In Development, accept the fixed OTP without touching the cache.
        if (hostEnv.IsDevelopment() && code == DevOtp) return true;

        var stored = await cache.GetStringAsync(CacheKey(phone));
        if (stored == null || stored != Hash(code)) return false;
        await cache.RemoveAsync(CacheKey(phone));
        return true;
    }

    private static string CacheKey(string phone) => $"otp:{phone}";
    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
