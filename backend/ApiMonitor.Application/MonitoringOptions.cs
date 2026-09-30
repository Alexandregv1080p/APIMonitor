namespace ApiMonitor.Application;

public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>Liga/desliga o worker (útil em testes e em réplicas só de API).</summary>
    public bool WorkerEnabled { get; set; } = true;

    /// <summary>Limite de verificações HTTP simultâneas.</summary>
    public int MaxConcurrentChecks { get; set; } = 10;

    /// <summary>De quanto em quanto tempo o worker procura endpoints vencidos.</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>Falhas consecutivas que disparam um alerta.</summary>
    public int AlertFailureThreshold { get; set; } = 3;
}
