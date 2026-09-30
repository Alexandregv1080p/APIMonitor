using ApiMonitor.Application.DTOs;
using FluentValidation;

namespace ApiMonitor.Application.Validators;

public class CheckHistoryQueryValidator : AbstractValidator<CheckHistoryQuery>
{
    public const int MaxPageSize = 200;

    public CheckHistoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}
