using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Application.Features.Tenants;
using PostyLand.Application.Features.Tenants.DTOs;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Repositories.Interfaces;

namespace PostyLand.API.Controllers;

[Route("api/tenants")]
public sealed class TenantsController(
    ITenantRegistrationService tenantRegistrationService,
    IBaseRepository<Tenant> tenantRepository,
    ITenantStore tenantStore,
    IValidator<CreateTenantRequest> createTenantValidator,
    IValidator<UpdateTenantRequest> updateTenantValidator) : AdminBaseController
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantRegistrationService.RegisterAsync(request, cancellationToken);
        return Accepted(new
        {
            result.TenantId,
            result.OnboardingJobId,
            result.Status
        });
    }

    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        await createTenantValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureNewTenantIdIsAvailableAsync(request.Id);

        var normalizedSubdomain = NormalizeSubdomain(request.Subdomain);
        await EnsureUniqueSubdomainAsync(normalizedSubdomain, null, cancellationToken);

        var tenant = request.ToEntity(normalizedSubdomain);
        await tenantRepository.AddAsync(tenant);
        await tenantStore.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantResponse>>> GetAll()
    {
        var tenants = await tenantRepository.GetAllAsync();
        return Ok(tenants.Select(x => x.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> GetById(Guid id)
    {
        var tenant = await tenantRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tenant '{id}' does not exist.");

        return Ok(tenant.ToResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> Update(
        Guid id,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        await updateTenantValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (request.Id.HasValue && request.Id.Value != Guid.Empty && request.Id.Value != id)
        {
            throw new ValidationException("Tenant body Id must match the route Id when provided.");
        }

        var tenant = await tenantRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tenant '{id}' does not exist.");

        var normalizedSubdomain = NormalizeSubdomain(request.Subdomain);
        await EnsureUniqueSubdomainAsync(normalizedSubdomain, id, cancellationToken);

        request.ApplyTo(tenant, normalizedSubdomain);
        tenantRepository.Update(tenant);
        await tenantStore.SaveChangesAsync(cancellationToken);

        return Ok(tenant.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tenant '{id}' does not exist.");

        tenantRepository.Delete(tenant);
        await tenantStore.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task EnsureNewTenantIdIsAvailableAsync(Guid? tenantId)
    {
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return;
        }

        var existingTenant = await tenantRepository.GetByIdAsync(tenantId.Value);
        if (existingTenant is not null)
        {
            throw new ValidationException($"Tenant '{tenantId}' already exists.");
        }
    }

    private async Task EnsureUniqueSubdomainAsync(
        string normalizedSubdomain,
        Guid? currentTenantId,
        CancellationToken cancellationToken)
    {
        var existingTenant = await tenantStore.GetBySubdomainAsync(normalizedSubdomain, cancellationToken);
        if (existingTenant is not null && existingTenant.Id != currentTenantId)
        {
            throw new ValidationException($"Subdomain '{normalizedSubdomain}' is already in use.");
        }
    }

    private static string NormalizeSubdomain(string subdomain)
    {
        return subdomain.Trim().ToLowerInvariant();
    }
}
