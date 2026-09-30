using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiMonitor.Infrastructure.Monitoring;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;

namespace ApiMonitor.Tests.Integration;

/// <summary>
/// Sobe a API inteira contra PostgreSQL e MongoDB reais (Testcontainers).
/// Só o destino das verificações HTTP é simulado, para os cenários serem determinísticos.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private readonly MongoDbContainer _mongo = new MongoDbBuilder().WithImage("mongo:7").Build();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    public Task InitializeAsync() => Task.WhenAll(_postgres.StartAsync(), _mongo.StartAsync());

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _mongo.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Mongo", _mongo.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Monitoring:WorkerEnabled", "false"); // testes do worker ligam explicitamente
        builder.UseSetting("Monitoring:PollIntervalSeconds", "1");

        builder.ConfigureTestServices(services =>
            services.AddHttpClient(HttpEndpointChecker.ClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new TargetHandler()));
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

/// <summary>Servidor-alvo simulado: o caminho da URL define o comportamento.</summary>
public class TargetHandler : HttpMessageHandler
{
    public const string BaseUrl = "http://target.test";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        switch (request.RequestUri!.AbsolutePath)
        {
            case "/ok":
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("pong") };
            case "/error":
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            case "/slow":
                await Task.Delay(Timeout.Infinite, ct);
                throw new System.Diagnostics.UnreachableException();
            case "/refused":
                throw new HttpRequestException("Connection refused");
            default:
                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
