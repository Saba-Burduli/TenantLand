using FluentValidation;

namespace PostyLand.Application.Features.BillingHistory.Validators;

public sealed class CreateBillingHistoryRequestValidator : AbstractValidator<CreateBillingHistoryRequest>
{
    public CreateBillingHistoryRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.EntryType)
            .IsInEnum();

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.OccurredAt)
            .NotEqual(default(DateTime));

        RuleFor(x => x.ProviderReference)
            .MaximumLength(200);

        RuleFor(x => x.Note)
            .MaximumLength(1000);
    }
}
