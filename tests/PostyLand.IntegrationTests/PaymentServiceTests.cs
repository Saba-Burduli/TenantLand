using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Infrastructure;
using PostyLand.Infrastructure.Payments;

namespace PostyLand.IntegrationTests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task ProcessPaymentAsync_ShouldAlwaysReturnTrue_AndLogSuccessMessage()
    {
        var logger = new TestLogger<PaymentService>();
        var service = new PaymentService(logger);

        var result = await service.ProcessPaymentAsync(129.99m, "USD", Guid.NewGuid());

        Assert.True(result);
        Assert.Contains(
            logger.Messages,
            message => message.Contains("Mock payment processed successfully.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RefundAndVerify_ShouldAlwaysReturnTrue()
    {
        var logger = new TestLogger<PaymentService>();
        var service = new PaymentService(logger);

        var paymentId = Guid.NewGuid();
        var refundResult = await service.RefundPaymentAsync(paymentId);
        var verifyResult = await service.VerifyPaymentAsync(paymentId);

        Assert.True(refundResult);
        Assert.True(verifyResult);
    }

    [Fact]
    public void DependencyInjection_ShouldResolvePaymentService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        Assert.IsType<PaymentService>(service);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
