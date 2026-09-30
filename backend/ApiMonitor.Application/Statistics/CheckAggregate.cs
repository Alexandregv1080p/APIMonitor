namespace ApiMonitor.Application.Statistics;

/// <summary>
/// Somatórios brutos de um conjunto de verificações (calculados no MongoDB).
/// As métricas derivadas ficam aqui, em código puro, para serem testáveis sem banco.
/// </summary>
/// <param name="Responded">Verificações que receberam resposta HTTP (qualquer status). Timeouts e erros de rede não entram na latência.</param>
public record CheckAggregate(
    long Total,
    long Successful,
    long Responded,
    long LatencySumMs,
    long? MinLatencyMs,
    long? MaxLatencyMs)
{
    public static readonly CheckAggregate Empty = new(0, 0, 0, 0, null, null);

    public long Failed => Total - Successful;

    /// <summary>Null quando não há verificações no período (desconhecido ≠ 0%).</summary>
    public double? UptimePercentage => Total == 0 ? null : Math.Round(Successful * 100.0 / Total, 2);

    public double? AverageLatencyMs => Responded == 0 ? null : Math.Round((double)LatencySumMs / Responded, 1);

    public CheckAggregate Combine(CheckAggregate other) => new(
        Total + other.Total,
        Successful + other.Successful,
        Responded + other.Responded,
        LatencySumMs + other.LatencySumMs,
        MinOf(MinLatencyMs, other.MinLatencyMs, Math.Min),
        MinOf(MaxLatencyMs, other.MaxLatencyMs, Math.Max));

    private static long? MinOf(long? a, long? b, Func<long, long, long> pick) =>
        a is null ? b : b is null ? a : pick(a.Value, b.Value);
}
