using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Domain.Entities;

public class MonitoredEndpoint
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public EndpointHttpMethod Method { get; private set; }
    public int IntervalSeconds { get; private set; }
    public int TimeoutMilliseconds { get; private set; }
    public int ExpectedStatusCode { get; private set; }
    public bool Enabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastCheckedAt { get; private set; }
    public EndpointStatus LastStatus { get; private set; }
    public int ConsecutiveFailures { get; private set; }

    private MonitoredEndpoint() { } // EF Core

    public MonitoredEndpoint(
        string name, string url, EndpointHttpMethod method, int intervalSeconds,
        int timeoutMilliseconds, int expectedStatusCode, bool enabled)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        LastStatus = EndpointStatus.Unknown;
        Update(name, url, method, intervalSeconds, timeoutMilliseconds, expectedStatusCode, enabled);
    }

    public void Update(
        string name, string url, EndpointHttpMethod method, int intervalSeconds,
        int timeoutMilliseconds, int expectedStatusCode, bool enabled)
    {
        Name = name.Trim();
        Url = url.Trim();
        Method = method;
        IntervalSeconds = intervalSeconds;
        TimeoutMilliseconds = timeoutMilliseconds;
        ExpectedStatusCode = expectedStatusCode;
        Enabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDue(DateTime now) =>
        Enabled && (LastCheckedAt is null || LastCheckedAt.Value.AddSeconds(IntervalSeconds) <= now);

    /// <summary>Aplica o resultado de uma verificação. Retorna true se o status mudou.</summary>
    public bool RecordCheck(CheckResult result)
    {
        var previous = LastStatus;
        LastCheckedAt = result.CheckedAt;
        LastStatus = result.Success ? EndpointStatus.Up : EndpointStatus.Down;
        ConsecutiveFailures = result.Success ? 0 : ConsecutiveFailures + 1;
        return previous != LastStatus;
    }
}
