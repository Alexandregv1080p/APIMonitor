using ApiMonitor.Application.Statistics;

namespace ApiMonitor.Tests.Statistics;

public class CheckAggregateTests
{
    [Fact]
    public void Empty_has_unknown_uptime_and_latency()
    {
        Assert.Null(CheckAggregate.Empty.UptimePercentage);
        Assert.Null(CheckAggregate.Empty.AverageLatencyMs);
        Assert.Equal(0, CheckAggregate.Empty.Failed);
    }

    [Fact]
    public void Uptime_is_rounded_percentage_of_successful_checks()
    {
        var a = new CheckAggregate(Total: 3, Successful: 2, Responded: 3, LatencySumMs: 300, MinLatencyMs: 50, MaxLatencyMs: 150);

        Assert.Equal(66.67, a.UptimePercentage);
        Assert.Equal(1, a.Failed);
    }

    [Fact]
    public void Average_latency_uses_only_checks_with_a_response()
    {
        // 4 verificações, 1 timeout: média sobre as 3 que responderam.
        var a = new CheckAggregate(Total: 4, Successful: 3, Responded: 3, LatencySumMs: 100, MinLatencyMs: 20, MaxLatencyMs: 50);

        Assert.Equal(33.3, a.AverageLatencyMs);
    }

    [Fact]
    public void All_failed_without_response_has_zero_uptime_and_no_latency()
    {
        var a = new CheckAggregate(Total: 5, Successful: 0, Responded: 0, LatencySumMs: 0, MinLatencyMs: null, MaxLatencyMs: null);

        Assert.Equal(0, a.UptimePercentage);
        Assert.Null(a.AverageLatencyMs);
    }

    [Fact]
    public void Combine_sums_counts_and_keeps_extremes()
    {
        var a = new CheckAggregate(10, 9, 10, 1000, 40, 300);
        var b = new CheckAggregate(10, 10, 10, 500, 20, 90);

        var c = a.Combine(b);

        Assert.Equal(new CheckAggregate(20, 19, 20, 1500, 20, 300), c);
        Assert.Equal(95, c.UptimePercentage);
        Assert.Equal(75, c.AverageLatencyMs);
    }

    [Fact]
    public void Combine_with_empty_keeps_values()
    {
        var a = new CheckAggregate(2, 1, 1, 80, 80, 80);
        Assert.Equal(a, a.Combine(CheckAggregate.Empty));
        Assert.Equal(a, CheckAggregate.Empty.Combine(a));
    }

    [Theory]
    [InlineData(null, "24h")]
    [InlineData("24h", "24h")]
    [InlineData("7D", "7d")]
    [InlineData("30d", "30d")]
    public void Parses_known_periods(string? input, string expected) =>
        Assert.Equal(expected, StatisticsPeriod.Parse(input)!.Name);

    [Fact]
    public void Rejects_unknown_period() => Assert.Null(StatisticsPeriod.Parse("1y"));
}
