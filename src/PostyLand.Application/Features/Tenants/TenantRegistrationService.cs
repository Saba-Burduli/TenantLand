using FluentValidation;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Entities;
using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.Tenants;

public sealed class TenantRegistrationService(
    IValidator<RegisterTenantRequest> validator,
    ITenantStore tenantStore,
    ISubscriptionStore subscriptionStore,
    IAdminUserStore adminUserStore,
    ITenantConnectionStringBuilder connectionStringBuilder,
    ITenantConnectionResolver tenantConnectionResolver,
    IPasswordHasher passwordHasher,
    ITenantOnboardingJobClient onboardingJobClient) : ITenantRegistrationService
{
    public async Task<RegisterTenantResponse> RegisterAsync(RegisterTenantRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedSubdomain = request.Subdomain.Trim().ToLowerInvariant();
        var exists = await tenantStore.ExistsBySubdomainAsync(normalizedSubdomain, cancellationToken);
        if (exists)
        {
            throw new ValidationException($"Subdomain '{normalizedSubdomain}' is already in use.");
        }

        var connectionString = connectionStringBuilder.Build(normalizedSubdomain);
        var encryptedConnectionString = tenantConnectionResolver.Encrypt(connectionString);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Subdomain = normalizedSubdomain,
            EncryptedConnectionString = encryptedConnectionString,
            Status = TenantOnboardingStatus.Pending,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Plan = request.Plan.Trim(),
            EmployeeLimit = request.EmployeeLimit,
            BillingStatus = BillingStatus.Active,
            RenewalDate = request.RenewalDate.ToUniversalTime(),
            IsActive = true
        };

        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            Email = request.AdminEmail.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(request.AdminPassword),
            Role = RoleStatus.Owner
        };

        await tenantStore.AddAsync(tenant, cancellationToken);
        await subscriptionStore.AddAsync(subscription, cancellationToken);
        await adminUserStore.AddAsync(adminUser, cancellationToken);
        await tenantStore.SaveChangesAsync(cancellationToken);

        var jobId = onboardingJobClient.Enqueue(tenant.Id);
        return new RegisterTenantResponse
        {
            TenantId = tenant.Id,
            OnboardingJobId = jobId,
            Status = tenant.Status
        };
    }
}


