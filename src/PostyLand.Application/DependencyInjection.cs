using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostyLand.Application.Features.Tenants;
using PostyLand.Application.Features.Tenants.Validators;

namespace PostyLand.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterTenantRequestValidator>();
        services.AddScoped<ITenantRegistrationService, TenantRegistrationService>();
        services.AddScoped<ITenantResolverService, TenantResolverService>();
        services.AddScoped<ITenantOnboardingOrchestrator, TenantOnboardingOrchestrator>();
        return services;
    }
}
