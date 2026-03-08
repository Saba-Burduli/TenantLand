using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;

namespace PostyLand.API.Controllers;

[Route("api/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : AdminBaseController
{
    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] ProcessPaymentRequest request, CancellationToken cancellationToken)
    {
        var success = await paymentService.ProcessPaymentAsync(
            request.Amount,
            request.Currency.Trim().ToUpperInvariant(),
            request.UserId);

        return success
            ? Ok(new PaymentOperationResponse { Success = true, Message = "Mock payment processed successfully." })
            : BadRequest(new PaymentOperationResponse { Success = false, Message = "Payment processing failed." });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var verified = await paymentService.VerifyPaymentAsync(id);
        if (!verified)
        {
            return BadRequest(new PaymentOperationResponse { Success = false, Message = "Payment verification failed." });
        }

        return Ok(new PaymentDetailsResponse
        {
            Id = id,
            Amount = 100m,
            Currency = "USD",
            UserId = Guid.Empty,
            IsSuccessful = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] RefundPaymentRequest request, CancellationToken cancellationToken)
    {
        var success = await paymentService.RefundPaymentAsync(request.PaymentId);

        return success
            ? Ok(new PaymentOperationResponse { Success = true, Message = "Mock payment processed successfully." })
            : BadRequest(new PaymentOperationResponse { Success = false, Message = "Refund failed." });
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var isHealthy = await paymentService.VerifyPaymentAsync(Guid.NewGuid());
        return Ok(new PaymentHealthResponse { IsHealthy = isHealthy });
    }
}

public sealed class ProcessPaymentRequest : IValidatableObject
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; init; }

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public string Currency { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserId == Guid.Empty)
        {
            yield return new ValidationResult("UserId is required.", [nameof(UserId)]);
        }
    }
}

public sealed class RefundPaymentRequest : IValidatableObject
{
    public Guid PaymentId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaymentId == Guid.Empty)
        {
            yield return new ValidationResult("PaymentId is required.", [nameof(PaymentId)]);
        }
    }
}

public sealed class PaymentOperationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class PaymentDetailsResponse
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public bool IsSuccessful { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class PaymentHealthResponse
{
    public bool IsHealthy { get; init; }
}
