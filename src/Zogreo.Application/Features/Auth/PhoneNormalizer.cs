namespace Zogreo.Application.Features.Auth;

internal static class PhoneNormalizer
{
    internal static string Normalize(string raw)
    {
        raw = raw.Trim().Replace(" ", "").Replace("-", "");
        if (raw.StartsWith("0") && raw.Length == 10) raw = "+254" + raw[1..];
        else if (raw.StartsWith("254") && !raw.StartsWith("+")) raw = "+" + raw;
        return raw;
    }
}
