using ApiMonitor.Application.DTOs;
using ApiMonitor.Application.Validators;

namespace ApiMonitor.Tests.Validators;

public class EndpointRequestValidatorTests
{
    private readonly EndpointRequestValidator _validator = new();

    private static EndpointRequest Valid() => new("GitHub API", "https://api.github.com");

    [Fact]
    public void Valid_request_passes() => Assert.True(_validator.Validate(Valid()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    [InlineData("/relative/path")]
    public void Rejects_non_http_urls(string url) =>
        AssertInvalid(Valid() with { Url = url }, nameof(EndpointRequest.Url));

    [Fact]
    public void Rejects_empty_name() =>
        AssertInvalid(Valid() with { Name = " " }, nameof(EndpointRequest.Name));

    [Theory]
    [InlineData(9)]
    [InlineData(86_401)]
    public void Rejects_interval_out_of_range(int seconds) =>
        AssertInvalid(Valid() with { IntervalSeconds = seconds }, nameof(EndpointRequest.IntervalSeconds));

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void Rejects_invalid_status_code(int code) =>
        AssertInvalid(Valid() with { ExpectedStatusCode = code }, nameof(EndpointRequest.ExpectedStatusCode));

    [Fact]
    public void Rejects_timeout_below_minimum() =>
        AssertInvalid(Valid() with { TimeoutMilliseconds = 99 }, nameof(EndpointRequest.TimeoutMilliseconds));

    [Fact]
    public void Rejects_timeout_longer_than_interval() =>
        AssertInvalid(Valid() with { IntervalSeconds = 10, TimeoutMilliseconds = 10_001 },
            nameof(EndpointRequest.TimeoutMilliseconds));

    private void AssertInvalid(EndpointRequest request, string property)
    {
        var result = _validator.Validate(request);
        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }
}
