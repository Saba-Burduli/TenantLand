using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PostyLand.Domain.Enums;
using PostyLand.IntegrationTests.Infrastructure;

namespace PostyLand.IntegrationTests;

[Collection(IntegrationCollection.Name)]
public sealed class PaymentEndpointsTests(PostyLandIntegrationFixture fixture)
{
    [Fact]
    public async Task ProcessPayment_ShouldReturnOk_WhenRequestIsValid()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/process");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreatePlatformAdminToken());
        request.Content = JsonContent.Create(new
        {
            amount = 59.99m,
            currency = "usd",
            userId = Guid.NewGuid()
        });

        var response = await fixture.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("Mock payment processed successfully.", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ProcessPayment_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/process");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreatePlatformAdminToken());
        request.Content = JsonContent.Create(new
        {
            amount = 0m,
            currency = "US",
            userId = Guid.Empty
        });

        var response = await fixture.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPaymentById_ShouldReturnMockDetails()
    {
        var paymentId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/payments/{paymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreatePlatformAdminToken());

        var response = await fixture.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(paymentId, doc.RootElement.GetProperty("id").GetGuid());
        Assert.True(doc.RootElement.GetProperty("isSuccessful").GetBoolean());
        Assert.Equal("USD", doc.RootElement.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task RefundPayment_ShouldReturnOk_WhenRequestIsValid()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/refund");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreatePlatformAdminToken());
        request.Content = JsonContent.Create(new
        {
            paymentId = Guid.NewGuid()
        });

        var response = await fixture.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Health_ShouldReturnTrue()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/payments/health");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreatePlatformAdminToken());

        var response = await fixture.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("isHealthy").GetBoolean());
    }

    [Fact]
    public async Task OpenApi_ShouldContainPaymentEndpoints()
    {
        var response = await fixture.Client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"/api/payments/process\"", body, StringComparison.Ordinal);
        Assert.Contains("\"/api/payments/{id}\"", body, StringComparison.Ordinal);
        Assert.Contains("\"/api/payments/refund\"", body, StringComparison.Ordinal);
        Assert.Contains("\"/api/payments/health\"", body, StringComparison.Ordinal);
    }

    private string CreatePlatformAdminToken()
    {
        return JwtTokenFactory.Create(
            fixture.GetRuntimeConfigurationValue("Jwt:SigningKey"),
            fixture.GetRuntimeConfigurationValue("Jwt:Issuer"),
            fixture.GetRuntimeConfigurationValue("Jwt:Audience"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            RoleStatus.PlatformAdmin,
            "platform.admin");
    }
}
