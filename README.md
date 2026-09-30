# API Monitor

Plataforma de monitoramento de APIs HTTP: cadastro de endpoints, verificações periódicas, latência, disponibilidade e dashboard. Especificação completa em [api-monitor-claude.md](api-monitor-claude.md).

> **Status:** em desenvolvimento — CRUD de endpoints (PostgreSQL), worker de monitoramento, histórico (MongoDB), estatísticas e dashboard React prontos. Próximos: testes de integração, health checks, Docker completo e GitLab CI.

## Stack

- **Backend:** C# / ASP.NET Core 8, EF Core + PostgreSQL, MongoDB.Driver, BackgroundService, IHttpClientFactory, FluentValidation, Swagger, xUnit
- **Frontend:** React 19, TypeScript, Vite, React Router, Axios, Recharts
- **Infra:** Docker Compose

## Estrutura

```text
backend/
├── ApiMonitor.Api             # controllers, tratamento global de erros (ProblemDetails), Program.cs
├── ApiMonitor.Application     # DTOs, serviços, validadores, interfaces de repositório
├── ApiMonitor.Domain          # entidades e enums
├── ApiMonitor.Infrastructure  # EF Core (PostgreSQL), MongoDB, checker HTTP, MonitoringWorker
└── ApiMonitor.Tests

frontend/api-monitor-web/     # React + Vite (components, pages, services, hooks, utils)
```

## Executando localmente

Requisitos: .NET 8 SDK e Docker.

```bash
docker compose up -d                                        # PostgreSQL :5437, MongoDB :27019
dotnet run --project backend/ApiMonitor.Api --launch-profile http
```

Frontend (em outro terminal):

```bash
cd frontend/api-monitor-web
npm install
npm run dev
```

Dashboard: http://localhost:5173 (o Vite faz proxy de `/api` para a API). Swagger: http://localhost:5277/swagger. Em Development as migrations são aplicadas no startup.

Variáveis do compose podem ser sobrescritas copiando `.env.example` para `.env`.

## API

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/endpoints?status=UP&enabled=true` | Lista (filtros opcionais) |
| GET | `/api/endpoints/{id}` | Detalhe |
| POST | `/api/endpoints` | Cria |
| PUT | `/api/endpoints/{id}` | Substitui configuração |
| PATCH | `/api/endpoints/{id}/status` | Ativa/desativa (`{"enabled": false}`) |
| DELETE | `/api/endpoints/{id}` | Remove (e apaga o histórico no MongoDB) |
| POST | `/api/endpoints/{id}/check` | Verificação imediata |
| GET | `/api/endpoints/{id}/checks?from=&to=&page=1&pageSize=50` | Histórico paginado, mais recentes primeiro |
| GET | `/api/endpoints/{id}/statistics?period=24h\|7d\|30d` | Uptime, latência (média/mín/máx) e série temporal |
| GET | `/api/dashboard` | Totais, métricas das últimas 24h e resumo por endpoint |

Erros seguem `ProblemDetails` (RFC 7807). Regras de validação: URL http/https absoluta, `intervalSeconds` 10–86400, `timeoutMilliseconds` 100–60000 e não maior que o intervalo, `expectedStatusCode` 100–599.

## Como o monitoramento funciona

1. O `MonitoringWorker` (BackgroundService) acorda a cada `Monitoring:PollIntervalSeconds` e lista os endpoints habilitados cujo intervalo venceu.
2. Cada verificação é disparada sem bloquear o loop, limitada por um semáforo (`MaxConcurrentChecks`). Uma API lenta ocupa só a própria vaga; um endpoint ainda em verificação não é redisparado.
3. O `HttpEndpointChecker` faz a requisição via `IHttpClientFactory` com timeout por endpoint e classifica o resultado: sucesso, `UNEXPECTED_STATUS_CODE`, `TIMEOUT`, `DNS_ERROR` ou `CONNECTION_ERROR`. Latência = tempo até os headers.
4. O resultado vira um documento em `check_results` (MongoDB, índice `endpointId + checkedAt`) e o status atual/falhas consecutivas são atualizados no PostgreSQL.
5. Ao atingir `AlertFailureThreshold` falhas seguidas, um alerta é registrado em log (uma vez por incidente).

URLs monitoradas não são logadas (query strings costumam carregar chaves).

## Estatísticas

Calculadas no MongoDB com um único `$group` (somatórios), e as métricas derivadas em `CheckAggregate` (código puro, testado sem banco):

- **Uptime** = verificações com sucesso ÷ total. Sem verificações no período → `null` ("sem dados", não 0%).
- **Latência** considera só verificações que receberam resposta HTTP; timeouts e erros de rede não distorcem a média.
- **Série temporal** agrupada com `$dateTrunc`: 1 h (24h), 6 h (7d) e 1 dia (30d).

## Testes

```bash
dotnet test backend/ApiMonitor.sln
```

## Decisões

- **Migrations no startup** só com `Database:MigrateOnStartup=true` (dev/compose, instância única). Com múltiplas réplicas, desligar e aplicar via `dotnet ef migrations bundle` em um job de deploy.
- **Enums em JSON** como `UP`/`DOWN`/`GET` (maiúsculas), persistidos como texto no Postgres para legibilidade.
- **Timeout ≤ intervalo** para impedir que checagens do mesmo endpoint se sobreponham.
- **Worker dentro da API** no MVP. O estado "em andamento" é local ao processo: com várias réplicas, desligar o worker nelas (`Monitoring:WorkerEnabled=false`) ou evoluir para fila/lock distribuído.
- **Histórico sem retenção** por enquanto; um índice TTL em `checkedAt` resolve quando o volume importar.
- `dotnet-ef` fixado em 8.0.x no manifesto local (`.config/dotnet-tools.json`): `dotnet tool restore`.
