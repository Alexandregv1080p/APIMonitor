using ApiMonitor.Application;
using ApiMonitor.Application.Interfaces;
using ApiMonitor.Infrastructure.Monitoring;
using ApiMonitor.Infrastructure.Persistence.MongoDB;
using ApiMonitor.Infrastructure.Persistence.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ApiMonitor.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(RequiredConnectionString(configuration, "Postgres")));
        services.AddScoped<IEndpointRepository, EndpointRepository>();

        AddMongo(services, RequiredConnectionString(configuration, "Mongo"));

        services.AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName))
            .Validate(o => o.MaxConcurrentChecks > 0 && o.PollIntervalSeconds > 0 && o.AlertFailureThreshold > 0,
                "Monitoring options must be positive.")
            .ValidateOnStart();

        services.AddHttpClient(HttpEndpointChecker.ClientName, c =>
        {
            c.Timeout = Timeout.InfiniteTimeSpan; // o timeout é por endpoint, controlado no checker
            c.DefaultRequestHeaders.UserAgent.ParseAdd("ApiMonitor/1.0");
        });
        services.AddSingleton<IEndpointChecker, HttpEndpointChecker>();
        services.AddHostedService<MonitoringWorker>();

        // Tag "ready": dependências externas, consultadas só em /health/ready.
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgresql", tags: [ReadyTag])
            .AddCheck<MongoHealthCheck>("mongodb", tags: [ReadyTag], timeout: TimeSpan.FromSeconds(3));

        return services;
    }

    private static void AddMongo(IServiceCollection services, string connectionString)
    {
        // Documentos em camelCase e enums como texto, como no modelo da especificação.
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        ConventionRegistry.Register("ApiMonitor", new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreExtraElementsConvention(true)
        }, t => t.Namespace?.StartsWith("ApiMonitor") == true);

        var url = MongoUrl.Create(connectionString);
        services.AddSingleton<IMongoClient>(new MongoClient(url));
        services.AddSingleton(sp =>
        {
            var collection = sp.GetRequiredService<IMongoClient>()
                .GetDatabase(url.DatabaseName ?? "apimonitor")
                .GetCollection<CheckResultDocument>(CheckResultRepository.CollectionName);
            CheckResultRepository.EnsureIndexes(collection); // idempotente
            return collection;
        });
        services.AddScoped<ICheckResultRepository, CheckResultRepository>();
    }

    private static string RequiredConnectionString(IConfiguration configuration, string name)
    {
        var value = configuration.GetConnectionString(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Connection string '{name}' is not configured.")
            : value;
    }
}
