using FluentValidation;
using PostyLand.Application.Features.Tenants.DTOs;

namespace PostyLand.Application.Features.Tenants.Validators;

public sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .When(x => x.Id.HasValue);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .MaximumLength(63);

        RuleFor(x => x.EncryptedConnectionString)
            .NotEmpty();
    }
}
