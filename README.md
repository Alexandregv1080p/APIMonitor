# API Monitor

Plataforma de monitoramento de APIs HTTP. Você cadastra endpoints e o sistema os verifica periodicamente em segundo plano, medindo latência, código HTTP e disponibilidade, registrando falhas e mostrando tudo em um dashboard.

Projeto de portfólio: processamento assíncrono com `BackgroundService`, persistência poliglota (PostgreSQL + MongoDB), testes de integração com Testcontainers, Docker e GitLab CI/CD. Especificação original em [api-monitor-claude.md](api-monitor-claude.md).

## Features

- CRUD de endpoints monitorados (método, intervalo, timeout, status esperado, ativar/pausar)
- Verificação automática respeitando o intervalo de cada endpoint, com concorrência limitada
- Classificação de falhas: status inesperado, timeout, DNS e erro de conexão
- Histórico paginado de verificações e verificação manual sob demanda
- Estatísticas por período (24h / 7d / 30d): uptime, latência média/mín/máx e série temporal
- Dashboard com status em tempo quase real, gráficos de latência e disponibilidade
- Alerta (em log) após N falhas consecutivas
- Health checks de liveness e readiness, logs estruturados, erros em `ProblemDetails`

## Arquitetura

```text
 React (nginx) ──/api──►  ASP.NET Core API ─────────────► PostgreSQL
                          ├─ Controllers                   endpoints, status atual
                          ├─ Application (serviços)
                          ├─ Domain
                          └─ MonitoringWorker ──HTTP──► APIs monitoradas
                                     │
                                     └──────────────────► MongoDB
                                                         check_results (eventos)
```

- **PostgreSQL** guarda os dados estruturais (configuração e status atual de cada endpoint).
- **MongoDB** guarda cada verificação como um evento independente — é o dado que cresce sem parar e é consultado por agregação.

### Como o monitoramento funciona

1. O `MonitoringWorker` acorda a cada `Monitoring:PollIntervalSeconds` e lista os endpoints habilitados cujo intervalo venceu.
2. Cada verificação é disparada sem bloquear o loop, limitada por um semáforo (`MaxConcurrentChecks`). Uma API lenta ocupa só a própria vaga; um endpoint ainda em verificação não é redisparado.
3. O `HttpEndpointChecker` faz a requisição via `IHttpClientFactory` com timeout por endpoint e classifica o resultado. Latência = tempo até os headers.
4. O resultado vira um documento em `check_results` (índice `endpointId + checkedAt`) e o status atual/falhas consecutivas são atualizados no PostgreSQL.
5. Ao atingir `AlertFailureThreshold` falhas seguidas, um alerta é registrado em log (uma vez por incidente).

### Estatísticas

Calculadas no MongoDB com um único `$group` (somatórios); as métricas derivadas ficam em `CheckAggregate`, código puro testado sem banco.

- **Uptime** = verificações com sucesso ÷ total. Sem verificações no período → `null` ("sem dados", não 0%).
- **Latência** considera só verificações que receberam resposta HTTP; timeouts e erros de rede não distorcem a média.
- **Série temporal** agrupada com `$dateTrunc`: 1 h (24h), 6 h (7d) e 1 dia (30d).

## Stack

| Camada | Tecnologias |
|---|---|
| Backend | C# / ASP.NET Core 8, EF Core + Npgsql, MongoDB.Driver, BackgroundService, IHttpClientFactory, FluentValidation, Swagger |
| Frontend | React 19, TypeScript, Vite, React Router, Axios, Recharts |
| Testes | xUnit, Testcontainers, WebApplicationFactory, Vitest |
| Infra | Docker, Docker Compose, nginx, GitLab CI/CD |

```text
backend/
├── ApiMonitor.Api             # controllers, ProblemDetails, health checks, Program.cs
├── ApiMonitor.Application     # DTOs, serviços, validadores, interfaces, estatísticas
├── ApiMonitor.Domain          # entidades, enums, regras de status
├── ApiMonitor.Infrastructure  # EF Core, MongoDB, checker HTTP, MonitoringWorker
└── ApiMonitor.Tests           # unitários + integração (Testcontainers)
frontend/api-monitor-web/      # React + Vite, servido por nginx em produção
```

## Executando

### Tudo via Docker

Requisito: Docker.

```bash
docker compose up -d
```

| Serviço | URL |
|---|---|
| Dashboard | http://localhost:3400 |
| API / Swagger | http://localhost:8095/swagger |
| Health | http://localhost:3400/health · http://localhost:3400/health/ready |

As migrations são aplicadas no startup da API. Portas e credenciais podem ser sobrescritas copiando `.env.example` para `.env`.

```bash
docker compose ps
docker compose logs -f api
docker compose down        # para
docker compose down -v     # para e apaga os volumes
```

### Desenvolvimento

Requisitos: .NET 8 SDK, Node 20 e Docker (para os bancos).

```bash
docker compose up -d postgres mongodb
dotnet run --project backend/ApiMonitor.Api --launch-profile http   # http://localhost:5277/swagger
```

```bash
cd frontend/api-monitor-web
npm install
npm run dev                                                         # http://localhost:5173
```

O Vite faz proxy de `/api` para a API. Para criar uma migration: `dotnet tool restore` e
`dotnet ef migrations add <Nome> -p backend/ApiMonitor.Infrastructure -s backend/ApiMonitor.Api -o Persistence/PostgreSQL/Migrations`.

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
| GET | `/api/endpoints/{id}/statistics?period=24h\|7d\|30d` | Uptime, latência e série temporal |
| GET | `/api/dashboard` | Totais, métricas das últimas 24h e resumo por endpoint |
| GET | `/health` | Liveness (processo de pé, sem checar dependências) |
| GET | `/health/ready` | Readiness (PostgreSQL + MongoDB); 503 se algum cair |

Erros seguem `ProblemDetails` (RFC 7807). Validação: URL http/https absoluta, `intervalSeconds` 10–86400, `timeoutMilliseconds` 100–60000 e não maior que o intervalo, `expectedStatusCode` 100–599.

## Testes

```bash
dotnet test backend/ApiMonitor.sln                                   # tudo (integração precisa de Docker)
dotnet test backend/ApiMonitor.sln --filter "Category!=Integration"  # só unitários
cd frontend/api-monitor-web && npm test
```

- **Unitários (backend):** validações, regras de status/vencimento, cálculo de uptime e latência, checker HTTP (200, 500, timeout, erro de conexão, cancelamento).
- **Integração (backend):** a API inteira via `WebApplicationFactory` contra PostgreSQL e MongoDB reais (Testcontainers) — CRUD, validação, verificação e persistência, classificação de falhas, histórico, exclusão em cascata, health checks e o worker verificando endpoints sozinho. Só o destino HTTP das verificações é simulado.
- **Frontend:** formatação e normalização de erros da API (Vitest).

## CI/CD (GitLab)

[.gitlab-ci.yml](.gitlab-ci.yml):

| Estágio | Jobs |
|---|---|
| build | `backend:build`, `frontend:build` (lint + build) |
| test | testes unitários do backend e do frontend, com relatório JUnit no MR |
| integration | testes com Testcontainers via Docker-in-Docker |
| security | SAST, Secret Detection, Dependency Scanning (templates GitLab), `dotnet list package --vulnerable`, `npm audit` |
| docker | imagens `api` e `web` no GitLab Container Registry com tag `$CI_COMMIT_SHA` (`latest` só na branch padrão) + Container Scanning |
| deploy | reservado para a fase de cloud |

Requer runner com Docker-in-Docker (`privileged`), como os shared runners do GitLab.com.

## Decisões

- **Por que MongoDB para os resultados?** Cada verificação é um evento independente, numeroso e consultado por agregação. Guardar tudo no PostgreSQL também funcionaria; a separação é uma escolha para isolar dados transacionais de eventos, não uma regra.
- **Worker dentro da API** no MVP, isolado atrás de `Monitoring:WorkerEnabled`. O estado "em andamento" é local ao processo: com várias réplicas, deixar o worker em uma só ou evoluir para fila/lock distribuído.
- **Migrations no startup** só com `Database:MigrateOnStartup=true` (instância única). Com várias réplicas, desligar e aplicar via `dotnet ef migrations bundle` num job de deploy.
- **Liveness sem dependências:** um banco oscilando não deve fazer o orquestrador reiniciar a API; isso é papel do readiness.
- **Timeout ≤ intervalo** impede verificações sobrepostas do mesmo endpoint.
- **URLs fora dos logs:** query strings de APIs de terceiros costumam carregar chaves.
- **Enums em JSON** como `UP`/`DOWN`/`GET`, persistidos como texto no Postgres para legibilidade.
- Containers rodam como usuário não-root; o nginx serve o frontend e faz proxy de `/api`, dispensando CORS.

## Evolução futura

- Retenção do histórico (índice TTL em `checkedAt`)
- Canais de alerta (e-mail, Slack, webhook) e configuração de alerta por endpoint
- Autenticação (JWT)
- Separar worker da API e distribuir verificações via fila (RabbitMQ / SQS)
- OpenTelemetry + Prometheus + Grafana
- Deploy em cloud (ECS/App Runner, RDS, MongoDB Atlas) com Terraform
