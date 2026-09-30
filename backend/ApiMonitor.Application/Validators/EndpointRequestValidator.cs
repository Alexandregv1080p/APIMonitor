using ApiMonitor.Application.DTOs;
using FluentValidation;

namespace ApiMonitor.Application.Validators;

public class EndpointRequestValidator : AbstractValidator<EndpointRequest>
{
    public const int MinIntervalSeconds = 10;
    public const int MaxIntervalSeconds = 86_400;
    public const int MinTimeoutMilliseconds = 100;
    public const int MaxTimeoutMilliseconds = 60_000;

    public EndpointRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeHttpUrl).WithMessage("'Url' must be an absolute http or https URL.");

        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.IntervalSeconds).InclusiveBetween(MinIntervalSeconds, MaxIntervalSeconds);
        RuleFor(x => x.TimeoutMilliseconds).InclusiveBetween(MinTimeoutMilliseconds, MaxTimeoutMilliseconds);
        RuleFor(x => x.ExpectedStatusCode).InclusiveBetween(100, 599);

        // Timeout maior que o intervalo faria checagens do mesmo endpoint se sobreporem.
        RuleFor(x => x.TimeoutMilliseconds)
            .Must((req, timeout) => timeout <= req.IntervalSeconds * 1000)
            .WithMessage("'Timeout Milliseconds' must not exceed the check interval.");
    }

    private static bool BeHttpUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
