namespace ApiMonitor.Application.Statistics;

/// <summary>Janela de estatísticas e granularidade da série temporal correspondente.</summary>
public record StatisticsPeriod(string Name, TimeSpan Window, TimeSpan Bucket)
{
    public static readonly StatisticsPeriod Last24Hours = new("24h", TimeSpan.FromHours(24), TimeSpan.FromHours(1));
    public static readonly StatisticsPeriod Last7Days = new("7d", TimeSpan.FromDays(7), TimeSpan.FromHours(6));
    public static readonly StatisticsPeriod Last30Days = new("30d", TimeSpan.FromDays(30), TimeSpan.FromDays(1));

    public static readonly IReadOnlyList<StatisticsPeriod> All = [Last24Hours, Last7Days, Last30Days];

    public static StatisticsPeriod? Parse(string? value) =>
        All.FirstOrDefault(p => string.Equals(p.Name, value ?? Last24Hours.Name, StringComparison.OrdinalIgnoreCase));
}
