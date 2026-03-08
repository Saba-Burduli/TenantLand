namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(decimal amount, string currency, Guid userId);
    Task<bool> RefundPaymentAsync(Guid paymentId);
    Task<bool> VerifyPaymentAsync(Guid paymentId);
}
