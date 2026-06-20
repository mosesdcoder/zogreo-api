using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Payments;

/// <summary>
/// Dev-only IPaystackClient that never calls Paystack.
/// Registered when Paystack:Simulate = true in configuration.
/// InitializeTransaction returns a fake redirect URL so the /payments/initiate
/// endpoint works without real API keys. Actual confirmation is triggered via
/// POST /dev/payments/{reference}/simulate which calls ApplyPaymentConfirmationCommand
/// with the amount read from the invoice (no verify round-trip needed).
/// </summary>
public class SimulatedPaystackClient : IPaystackClient
{
    public Task<PaystackInitResult> InitializeTransactionAsync(PaystackInitRequest req)
    {
        var result = new PaystackInitResult(
            Status: true,
            AuthorizationUrl: $"https://simulated.paystack.local/pay/{req.Reference}",
            Reference: req.Reference,
            AccessCode: "sim_" + req.Reference[..8]);

        return Task.FromResult(result);
    }

    public Task<PaystackVerifyResult?> VerifyTransactionAsync(string reference)
    {
        // Returns success; amount is intentionally 0 here —
        // SimulatePaymentCommand reads the real amount from the invoice instead.
        var result = new PaystackVerifyResult(
            Status: true,
            PaystackRef: reference,
            GatewayResponse: "Successful",
            Amount: 0,
            Fees: 0,
            Channel: "simulated");

        return Task.FromResult<PaystackVerifyResult?>(result);
    }
}
