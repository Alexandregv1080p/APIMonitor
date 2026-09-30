using ApiMonitor.Application.Interfaces;
using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ApiMonitor.Infrastructure.Persistence.MongoDB;

public class CheckResultRepository(IMongoCollection<CheckResultDocument> collection) : ICheckResultRepository
{
    public const string CollectionName = "check_results";

    public Task AddAsync(CheckResult result, CancellationToken ct) =>
        collection.InsertOneAsync(CheckResultDocument.From(result), cancellationToken: ct);

    public async Task<(IReadOnlyList<CheckResult> Items, long Total)> GetPageAsync(
        Guid endpointId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var f = Builders<CheckResultDocument>.Filter;
        var filter = f.Eq(d => d.EndpointId, endpointId);
        if (from is not null) filter &= f.Gte(d => d.CheckedAt, ToUtc(from.Value));
        if (to is not null) filter &= f.Lte(d => d.CheckedAt, ToUtc(to.Value));

        var totalTask = collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var docs = await collection.Find(filter)
            .SortByDescending(d => d.CheckedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return (docs.Select(d => d.ToDomain()).ToList(), await totalTask);
    }

    public Task DeleteByEndpointAsync(Guid endpointId, CancellationToken ct) =>
        collection.DeleteManyAsync(d => d.EndpointId == endpointId, ct);

    /// <summary>Índice que atende histórico e estatísticas: filtro por endpoint, ordenação por data.</summary>
    public static void EnsureIndexes(IMongoCollection<CheckResultDocument> collection) =>
        collection.Indexes.CreateOne(new CreateIndexModel<CheckResultDocument>(
            Builders<CheckResultDocument>.IndexKeys.Ascending(d => d.EndpointId).Descending(d => d.CheckedAt)));

    // Datas sem fuso (ex.: ?from=2026-09-29) são tratadas como UTC, não como horário local do servidor.
    private static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
}

public class CheckResultDocument
{
    public ObjectId Id { get; set; }
    public Guid EndpointId { get; set; }
    public DateTime CheckedAt { get; set; }
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public long? ResponseSizeBytes { get; set; }
    public CheckErrorType? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }

    public static CheckResultDocument From(CheckResult r) => new()
    {
        EndpointId = r.EndpointId,
        CheckedAt = r.CheckedAt,
        Success = r.Success,
        StatusCode = r.StatusCode,
        LatencyMs = r.LatencyMs,
        ResponseSizeBytes = r.ResponseSizeBytes,
        ErrorType = r.ErrorType,
        ErrorMessage = r.ErrorMessage
    };

    public CheckResult ToDomain() =>
        new(EndpointId, CheckedAt, Success, StatusCode, LatencyMs, ResponseSizeBytes, ErrorType, ErrorMessage);
}
