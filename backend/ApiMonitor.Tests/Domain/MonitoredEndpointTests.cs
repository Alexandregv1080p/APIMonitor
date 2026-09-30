using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Tests.Domain;

public class MonitoredEndpointTests
{
    private static MonitoredEndpoint Endpoint(bool enabled = true) =>
        new("Test", "https://example.com", EndpointHttpMethod.Get, 60, 5000, 200, enabled);

    private static CheckResult Result(bool success, DateTime at) =>
        new(Guid.Empty, at, success, success ? 200 : 500, 10, 0,
            success ? null : CheckErrorType.UnexpectedStatusCode, null);

    [Fact]
    public void New_endpoint_is_unknown_and_due()
    {
        var e = Endpoint();
        Assert.Equal(EndpointStatus.Unknown, e.LastStatus);
        Assert.True(e.IsDue(DateTime.UtcNow));
    }

    [Fact]
    public void Disabled_endpoint_is_never_due() => Assert.False(Endpoint(enabled: false).IsDue(DateTime.UtcNow));

    [Fact]
    public void Due_only_after_interval_elapsed()
    {
        var e = Endpoint();
        var t0 = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        e.RecordCheck(Result(true, t0));

        Assert.False(e.IsDue(t0.AddSeconds(59)));
        Assert.True(e.IsDue(t0.AddSeconds(60)));
    }

    [Fact]
    public void Record_check_tracks_status_changes_and_consecutive_failures()
    {
        var e = Endpoint();
        var now = DateTime.UtcNow;

        Assert.True(e.RecordCheck(Result(true, now)));   // Unknown -> Up
        Assert.False(e.RecordCheck(Result(true, now)));  // Up -> Up
        Assert.True(e.RecordCheck(Result(false, now)));  // Up -> Down
        Assert.False(e.RecordCheck(Result(false, now))); // Down -> Down
        Assert.Equal(EndpointStatus.Down, e.LastStatus);
        Assert.Equal(2, e.ConsecutiveFailures);

        e.RecordCheck(Result(true, now));
        Assert.Equal(EndpointStatus.Up, e.LastStatus);
        Assert.Equal(0, e.ConsecutiveFailures);
        Assert.Equal(now, e.LastCheckedAt);
    }
}
