using Microsoft.Extensions.Configuration;
using Zogreo.Application.Common.Interfaces;

namespace Zogreo.Infrastructure.Payments;

public class PaymentSettings(IConfiguration config) : IPaymentSettings
{
    public string SchoolSubaccountCode => config["Paystack:SchoolSubaccountCode"] ?? string.Empty;
}
