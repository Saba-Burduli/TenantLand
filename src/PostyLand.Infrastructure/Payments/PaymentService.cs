using Microsoft.Extensions.Logging;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;

namespace PostyLand.Infrastructure.Payments;

public sealed class PaymentService(ILogger<PaymentService> logger) : IPaymentService
{
    public Task<bool> ProcessPaymentAsync(decimal amount, string currency, Guid userId)
    {
        logger.LogInformation(
            "Mock payment processed successfully. Amount={Amount} Currency={Currency} UserId={UserId}",
            amount,
            currency,
            userId);
        return Task.FromResult(true);
    }

    public Task<bool> RefundPaymentAsync(Guid paymentId)
    {
        logger.LogInformation("Mock payment processed successfully. Action=Refund PaymentId={PaymentId}", paymentId);
        return Task.FromResult(true);
    }

    public Task<bool> VerifyPaymentAsync(Guid paymentId)
    {
        logger.LogInformation("Mock payment processed successfully. Action=Verify PaymentId={PaymentId}", paymentId);
        return Task.FromResult(true);
    }
}
