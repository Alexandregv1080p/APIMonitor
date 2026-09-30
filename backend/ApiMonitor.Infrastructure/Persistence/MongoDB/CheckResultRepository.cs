using ApiMonitor.Application.Interfaces;
using ApiMonitor.Application.Statistics;
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

    public async Task<CheckAggregate> AggregateAsync(Guid endpointId, DateTime since, CancellationToken ct)
    {
        var rows = await RunGroupAsync(new BsonDocument("endpointId", new BsonBinaryData(endpointId, GuidRepresentation.Standard)),
            since, groupKey: BsonNull.Value, ct);
        return rows.Count == 0 ? CheckAggregate.Empty : ToAggregate(rows[0]);
    }

    public async Task<IReadOnlyList<(DateTime BucketStart, CheckAggregate Aggregate)>> AggregateSeriesAsync(
        Guid endpointId, DateTime since, TimeSpan bucket, CancellationToken ct)
    {
        var (unit, binSize) = bucket.TotalHours % 24 == 0 ? ("day", (int)bucket.TotalDays) : ("hour", (int)bucket.TotalHours);
        var key = new BsonDocument("$dateTrunc", new BsonDocument
        {
            { "date", "$checkedAt" }, { "unit", unit }, { "binSize", binSize }
        });

        var rows = await RunGroupAsync(new BsonDocument("endpointId", new BsonBinaryData(endpointId, GuidRepresentation.Standard)),
            since, key, ct);
        return rows
            .Select(r => (r["_id"].ToUniversalTime(), ToAggregate(r)))
            .OrderBy(r => r.Item1)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, CheckAggregate>> AggregateByEndpointAsync(DateTime since, CancellationToken ct)
    {
        var rows = await RunGroupAsync(new BsonDocument(), since, "$endpointId", ct);
        return rows.ToDictionary(r => r["_id"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard), ToAggregate);
    }

    /// <summary>
    /// Um único $group calcula os somatórios; percentuais e médias ficam em CheckAggregate.
    /// Latência só conta verificações com resposta HTTP ($gt null exclui null e campo ausente).
    /// </summary>
    private Task<List<BsonDocument>> RunGroupAsync(BsonDocument match, DateTime since, BsonValue groupKey, CancellationToken ct)
    {
        match["checkedAt"] = new BsonDocument("$gte", ToUtc(since));
        var responded = new BsonDocument("$gt", new BsonArray { "$statusCode", BsonNull.Value });
        BsonDocument IfResponded(BsonValue then, BsonValue otherwise) =>
            new("$cond", new BsonArray { responded, then, otherwise });

        var group = new BsonDocument
        {
            { "_id", groupKey },
            { "total", new BsonDocument("$sum", 1) },
            { "successful", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { "$success", 1, 0 })) },
            { "responded", new BsonDocument("$sum", IfResponded(1, 0)) },
            { "latencySum", new BsonDocument("$sum", IfResponded("$latencyMs", 0)) },
            { "minLatency", new BsonDocument("$min", IfResponded("$latencyMs", BsonNull.Value)) },
            { "maxLatency", new BsonDocument("$max", IfResponded("$latencyMs", BsonNull.Value)) }
        };

        return collection.Aggregate<BsonDocument>(
            new BsonDocument[] { new("$match", match), new("$group", group) },
            cancellationToken: ct).ToListAsync(ct);
    }

    private static CheckAggregate ToAggregate(BsonDocument r) => new(
        r["total"].ToInt64(),
        r["successful"].ToInt64(),
        r["responded"].ToInt64(),
        r["latencySum"].ToInt64(),
        r["minLatency"].IsBsonNull ? null : r["minLatency"].ToInt64(),
        r["maxLatency"].IsBsonNull ? null : r["maxLatency"].ToInt64());

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
