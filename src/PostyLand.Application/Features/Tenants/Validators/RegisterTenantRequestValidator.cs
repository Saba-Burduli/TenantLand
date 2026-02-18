using FluentValidation;

namespace PostyLand.Application.Features.Tenants.Validators;

public sealed class RegisterTenantRequestValidator : AbstractValidator<RegisterTenantRequest>
{
    public RegisterTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$")
            .MaximumLength(63);

        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.AdminPassword)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.Plan)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.EmployeeLimit)
            .GreaterThan(0);

        RuleFor(x => x.RenewalDate)
            .Must(x => x > DateTime.UtcNow)
            .WithMessage("Renewal date must be in the future.");
    }
}
