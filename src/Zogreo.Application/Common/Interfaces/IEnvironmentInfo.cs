namespace Zogreo.Application.Common.Interfaces;

public interface IEnvironmentInfo
{
    bool IsDevelopment { get; }
    /// <summary>Set via App:ExposeOtp=true in config/secrets to return OTP in signup response (for testing without SMS).</summary>
    bool ExposeOtp { get; }
}
