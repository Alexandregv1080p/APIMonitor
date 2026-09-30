# API Monitor — Plataforma de Monitoramento de APIs

## 1. Visão geral

O **API Monitor** é uma plataforma cloud-native para monitoramento de APIs e endpoints HTTP.

A aplicação permite cadastrar endpoints, executar verificações periódicas automaticamente, medir latência, registrar códigos HTTP e falhas, calcular disponibilidade e apresentar os resultados em um dashboard.

O projeto foi pensado como **projeto de portfólio técnico**, demonstrando C#, ASP.NET Core, React/TypeScript, PostgreSQL, MongoDB, Docker, Git/GitLab, GitLab CI/CD, processamento em segundo plano, testes e observabilidade.

O objetivo não é apenas criar um CRUD, mas demonstrar a construção de um sistema que executa processamento assíncrono, persiste eventos de monitoramento e apresenta métricas operacionais.

---

# 2. Objetivos

O sistema deve permitir:

1. cadastrar APIs para monitoramento;
2. editar e remover endpoints;
3. ativar/desativar monitoramento;
4. configurar intervalo de verificação;
5. configurar timeout;
6. executar verificações automaticamente;
7. registrar código HTTP;
8. medir latência;
9. detectar timeout e erros de conexão;
10. armazenar histórico das verificações;
11. calcular disponibilidade;
12. apresentar estatísticas;
13. apresentar gráficos;
14. identificar APIs indisponíveis;
15. registrar logs estruturados;
16. expor health checks;
17. executar completamente via Docker Compose;
18. possuir pipeline de CI/CD no GitLab;
19. possuir testes unitários e de integração.

---

# 3. Stack

## Frontend

- React
- TypeScript
- Vite
- React Router
- Axios
- Recharts

## Backend

- C#
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- MongoDB.Driver
- BackgroundService
- HttpClient / IHttpClientFactory
- Swagger/OpenAPI
- FluentValidation
- xUnit

## Infraestrutura

- Docker
- Docker Compose
- Git
- GitLab
- GitLab CI/CD

## Futuras extensões

- AWS
- Terraform
- Kubernetes
- Prometheus
- Grafana
- OpenTelemetry
- RabbitMQ ou AWS SQS

---

# 4. Arquitetura

```text
                        +---------------------+
                        |       React         |
                        |     Dashboard       |
                        +----------+----------+
                                   | HTTP
                                   v
                        +---------------------+
                        |   ASP.NET Core API  |
                        |                     |
                        | REST Controllers    |
                        | Application Layer   |
                        | Domain Layer        |
                        +-------+-------------+
                                |
                 +--------------+--------------+
                 |                             |
                 v                             v
        +-----------------+           +-----------------+
        |   PostgreSQL    |           |     MongoDB     |
        |                 |           |                 |
        | Endpoints       |           | CheckResults    |
        | Configurations  |           | Failure data    |
        | Users (future)  |           | Metrics/events  |
        +-----------------+           +--------+--------+
                                              ^
                                              |
                                   +----------+----------+
                                   | Monitoring Worker   |
                                   |                     |
                                   | BackgroundService   |
                                   | HttpClient          |
                                   | Scheduler           |
                                   | Measurements        |
                                   +---------------------+
```

O **PostgreSQL** é a fonte dos dados estruturais. O **MongoDB** armazena os resultados históricos das verificações.

---

# 5. Decisão sobre os bancos

## PostgreSQL

Utilizar para:

- endpoints monitorados;
- configurações;
- status atual;
- intervalos de monitoramento;
- timeout;
- configurações de alerta;
- usuários em uma fase futura.

## MongoDB

Utilizar para resultados de monitoramento.

Cada verificação gera um evento independente:

```json
{
  "endpointId": "uuid",
  "checkedAt": "2026-09-30T12:30:00Z",
  "success": true,
  "statusCode": 200,
  "latencyMs": 143,
  "responseSizeBytes": 1240,
  "error": null
}
```

A separação também permite discutir persistência poliglota e o crescimento potencial do histórico sem sobrecarregar as entidades relacionais principais.

---

# 6. Modelo de domínio

## MonitoredEndpoint

```text
Id
Name
Url
Method
IntervalSeconds
TimeoutMilliseconds
Enabled
ExpectedStatusCode
CreatedAt
UpdatedAt
LastCheckedAt
LastStatus
```

Campos:

| Campo | Tipo | Descrição |
|---|---|---|
| Id | Guid | Identificador |
| Name | string | Nome amigável |
| Url | string | URL monitorada |
| Method | enum | GET, POST, HEAD etc. |
| IntervalSeconds | int | Intervalo entre verificações |
| TimeoutMilliseconds | int | Timeout máximo |
| Enabled | bool | Define se o monitoramento está ativo |
| ExpectedStatusCode | int | Código esperado |
| CreatedAt | DateTime | Data de criação |
| UpdatedAt | DateTime | Última alteração |
| LastCheckedAt | DateTime? | Última verificação |
| LastStatus | enum | UP/DOWN/UNKNOWN |

## CheckResult no MongoDB

```json
{
  "_id": "ObjectId",
  "endpointId": "uuid",
  "checkedAt": "2026-09-30T15:00:00Z",
  "success": true,
  "statusCode": 200,
  "latencyMs": 142,
  "responseSizeBytes": 1500,
  "errorType": null,
  "errorMessage": null
}
```

Em caso de erro:

```json
{
  "endpointId": "uuid",
  "checkedAt": "2026-09-30T15:01:00Z",
  "success": false,
  "statusCode": null,
  "latencyMs": 5001,
  "responseSizeBytes": null,
  "errorType": "Timeout",
  "errorMessage": "The request timed out."
}
```

---

# 7. Estados do endpoint

- `UP`: requisição concluída com o código esperado.
- `DOWN`: timeout, erro de conexão, DNS, rede ou código HTTP inesperado.
- `UNKNOWN`: ainda não existe uma verificação.

---

# 8. Fluxo de monitoramento

```text
BackgroundService
       |
       v
Busca endpoints habilitados
       |
       v
Verifica quais precisam ser executados
       |
       v
Executa requisição HTTP
       |
       +-- sucesso
       +-- HTTP inesperado
       +-- timeout
       +-- erro de conexão
       |
       v
Mede latência
       |
       v
Cria CheckResult
       |
       +------------> MongoDB
       |
       v
Atualiza status atual
       |
       v
PostgreSQL
```

---

# 9. Background Worker

Utilizar `BackgroundService` do ASP.NET Core.

Referência arquitetural:

```csharp
public class MonitoringWorker : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckEndpointsAsync(stoppingToken);
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }
    }
}
```

A implementação final deve:

- respeitar o intervalo individual de cada endpoint;
- utilizar `CancellationToken`;
- evitar bloqueios desnecessários;
- controlar concorrência;
- tratar exceções;
- registrar logs;
- evitar que uma API lenta impeça as demais.

---

# 10. Concorrência

Não executar todos os endpoints de forma puramente sequencial.

```text
             +-- API A
Worker ------+-- API B
             +-- API C
             +-- API D
```

Utilizar limite configurável, por exemplo:

```text
MaxConcurrentChecks = 10
```

Não criar um número ilimitado de Tasks.

---

# 11. HttpClient

Utilizar `IHttpClientFactory`. Não criar um novo `HttpClient` para cada verificação.

```csharp
services.AddHttpClient("MonitorClient");
```

O timeout individual do endpoint deve ser respeitado com segurança.

---

# 12. API REST

## Criar endpoint

```http
POST /api/endpoints
```

Request:

```json
{
  "name": "GitHub API",
  "url": "https://api.github.com",
  "method": "GET",
  "intervalSeconds": 60,
  "timeoutMilliseconds": 5000,
  "expectedStatusCode": 200,
  "enabled": true
}
```

## Listar

```http
GET /api/endpoints
GET /api/endpoints?status=UP
GET /api/endpoints?enabled=true
```

## Buscar

```http
GET /api/endpoints/{id}
```

## Atualizar

```http
PUT /api/endpoints/{id}
```

## Remover

```http
DELETE /api/endpoints/{id}
```

## Ativar/desativar

```http
PATCH /api/endpoints/{id}/status
```

Request:

```json
{
  "enabled": false
}
```

## Verificação manual

```http
POST /api/endpoints/{id}/check
```

Esse endpoint permite disparar uma verificação imediatamente para facilitar testes e demonstrações.

---

# 13. Histórico

```http
GET /api/endpoints/{id}/checks
```

Query parameters:

```text
?from=2026-09-29
&to=2026-09-30
&page=1
&pageSize=50
```

Response:

```json
{
  "items": [
    {
      "checkedAt": "2026-09-30T15:00:00Z",
      "success": true,
      "statusCode": 200,
      "latencyMs": 142
    }
  ],
  "page": 1,
  "pageSize": 50,
  "total": 1200
}
```

---

# 14. Estatísticas

```http
GET /api/endpoints/{id}/statistics
```

Response:

```json
{
  "uptimePercentage": 99.82,
  "averageLatencyMs": 143,
  "minLatencyMs": 72,
  "maxLatencyMs": 810,
  "totalChecks": 1000,
  "successfulChecks": 998,
  "failedChecks": 2
}
```

Períodos:

```text
?period=24h
?period=7d
?period=30d
```

---

# 15. Dashboard

Cards:

```text
+------------+  +------------+  +------------+  +------------+
| APIs       |  | Uptime     |  | Latência   |  | Falhas     |
| 24         |  | 99.82%     |  | 143 ms     |  | 12         |
+------------+  +------------+  +------------+  +------------+
```

Tabela:

```text
API                 STATUS       LATÊNCIA       UPTIME
------------------------------------------------------
GitHub API          UP           142ms          99.9%
Payment API         UP           238ms          99.7%
User Service        DOWN           --           96.2%
```

Utilizar Recharts para gráficos de latência e disponibilidade.

---

# 16. Página de detalhes

Rota sugerida:

```text
/api/details/:id
```

Mostrar:

- nome;
- URL;
- método;
- status atual;
- uptime;
- latência média;
- menor latência;
- maior latência;
- total de verificações;
- falhas;
- gráfico de latência;
- histórico;
- configurações.

---

# 17. Alertas

No MVP, implementar a infraestrutura para alertas.

Configuração:

```json
{
  "enabled": true,
  "failureThreshold": 3
}
```

Exemplo:

```text
Check 1 -> DOWN
Check 2 -> DOWN
Check 3 -> DOWN
           |
           v
         ALERTA
```

No MVP o alerta pode ser registrado em log. Depois poderá ser integrado com e-mail, Slack, Discord, webhook ou AWS SNS.

---

# 18. Health Checks

Criar:

```http
GET /health
GET /health/ready
```

Verificar aplicação, PostgreSQL e MongoDB.

---

# 19. Logs

Utilizar logging estruturado para:

- monitoring check started;
- monitoring check completed;
- monitoring check failed;
- endpoint status changed;
- worker started/stopped;
- database connection failed.

Nunca registrar senhas, tokens, API keys ou dados sensíveis.

---

# 20. Tratamento de erros

Utilizar tratamento global e `ProblemDetails`.

Exemplo:

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Endpoint not found",
  "status": 404,
  "detail": "The requested monitoring endpoint was not found."
}
```

---

# 21. Validações

Validar:

- URL válida;
- nome obrigatório;
- intervalo mínimo;
- timeout mínimo;
- código HTTP válido;
- método HTTP permitido.

Valores iniciais sugeridos:

```text
intervalSeconds >= 10
timeoutMilliseconds >= 100
expectedStatusCode entre 100 e 599
```

---

# 22. Segurança

No MVP:

- CORS configurado corretamente;
- validação de entrada;
- ProblemDetails;
- headers HTTP apropriados;
- secrets via environment variables;
- nenhuma credencial hardcoded.

Autenticação com JWT/Identity pode ser adicionada posteriormente.

---

# 23. Configuração

Exemplo:

```json
{
  "ConnectionStrings": {
    "Postgres": "",
    "Mongo": ""
  },
  "Monitoring": {
    "MaxConcurrentChecks": 10,
    "DefaultIntervalSeconds": 60
  }
}
```

Em Docker, utilizar environment variables.

---

# 24. Docker Compose

Serviços:

```text
frontend
api
postgres
mongodb
```

O comando principal deve ser:

```bash
docker compose up -d
```

Configurar volumes persistentes para PostgreSQL e MongoDB.

Comandos úteis:

```bash
docker compose ps
docker compose logs -f api
docker compose down
docker compose down -v
```

---

# 25. Migrations

Utilizar EF Core migrations:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Definir uma estratégia segura para migrations em ambiente com múltiplas instâncias.

---

# 26. Testes

## Unit tests

Testar:

- cálculo de uptime;
- cálculo de latência;
- validações;
- regras de status;
- transformação de resultados;
- regras de alerta.

## Integration tests

Testar o fluxo:

```text
Controller
    -> Application
    -> Infrastructure
    -> Database
```

Considerar Testcontainers para PostgreSQL e MongoDB.

## Worker tests

Cobrir:

- HTTP 200;
- HTTP 500;
- timeout;
- erro de conexão;
- persistência do resultado;
- atualização do status.

---

# 27. GitLab CI/CD

Criar `.gitlab-ci.yml`.

Pipeline:

```text
Push
 |
 v
Build
 |
 v
Unit Tests
 |
 v
Integration Tests
 |
 v
Security
 |
 v
Docker Build
 |
 v
Deploy
```

Stages:

```yaml
stages:
  - build
  - test
  - integration
  - security
  - docker
  - deploy
```

Backend:

```bash
dotnet restore
dotnet build --no-restore
dotnet test
```

Frontend:

```bash
npm ci
npm run build
npm test
```

Docker deve gerar imagens identificadas por `$CI_COMMIT_SHA`, evitando depender somente de `latest`.

Adicionar, quando disponível:

- dependency scanning;
- secret detection;
- container scanning.

---

# 28. Estrutura do backend

```text
backend/
├── ApiMonitor.Api/
│   ├── Controllers/
│   ├── Middleware/
│   ├── Extensions/
│   └── Program.cs
│
├── ApiMonitor.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   └── Validators/
│
├── ApiMonitor.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── ValueObjects/
│   └── Exceptions/
│
├── ApiMonitor.Infrastructure/
│   ├── Persistence/
│   │   ├── PostgreSQL/
│   │   └── MongoDB/
│   ├── Monitoring/
│   └── DependencyInjection/
│
└── ApiMonitor.Tests/
```

Não colocar regra de negócio nos controllers.

---

# 29. Estrutura do frontend

```text
frontend/
└── api-monitor-web/
    ├── src/
    │   ├── components/
    │   ├── pages/
    │   ├── layouts/
    │   ├── services/
    │   ├── hooks/
    │   ├── types/
    │   ├── utils/
    │   └── routes/
    ├── public/
    └── package.json
```

Componentes sugeridos:

```text
components/
├── StatusBadge/
├── MetricCard/
├── EndpointTable/
├── LatencyChart/
├── UptimeChart/
├── CheckHistory/
├── EndpointForm/
├── LoadingState/
└── ErrorState/
```

Centralizar chamadas HTTP em:

```text
services/
├── api.ts
├── endpointService.ts
├── statisticsService.ts
└── healthService.ts
```

---

# 30. Dashboard API

Criar endpoint agregado:

```http
GET /api/dashboard
```

Response:

```json
{
  "totalEndpoints": 24,
  "upEndpoints": 22,
  "downEndpoints": 2,
  "averageLatencyMs": 143,
  "uptimePercentage": 99.82,
  "failedChecksLast24Hours": 12
}
```

---

# 31. Observabilidade futura

Adicionar OpenTelemetry posteriormente para instrumentar:

- HTTP requests;
- chamadas ao banco;
- Background Worker;
- monitoring checks.

Arquitetura futura:

```text
ASP.NET Core
     |
     v
OpenTelemetry
     |
     +-- Metrics
     +-- Logs
     +-- Traces
            |
            v
       Prometheus
            |
            v
         Grafana
```

Métricas sugeridas:

```text
monitor_checks_total
monitor_checks_failed_total
monitor_check_latency_ms
monitor_endpoints_up
monitor_endpoints_down
monitor_worker_duration_ms
monitor_http_status_total
monitor_timeouts_total
monitor_connection_errors_total
```

---

# 32. AWS / Cloud — fase futura

Depois do MVP local:

```text
React
  -> S3 / CloudFront

ASP.NET Core
  -> ECS / App Runner / EC2

PostgreSQL
  -> RDS

MongoDB
  -> MongoDB Atlas
```

Não implementar cloud antes do MVP estar estável.

---

# 33. Terraform — fase futura

Estrutura:

```text
terraform/
├── main.tf
├── variables.tf
├── outputs.tf
├── providers.tf
└── modules/
```

Objetivo:

```bash
terraform plan
terraform apply
```

---

# 34. Kubernetes — fase futura

Não é requisito do MVP.

Possível evolução:

```text
Ingress
   |
   +-- Frontend
   |
   +-- API
         |
         +-- PostgreSQL
         +-- MongoDB
```

Em uma evolução, separar:

```text
api-monitor-api
api-monitor-worker
api-monitor-frontend
```

---

# 35. Evolução para filas

No MVP:

```text
ASP.NET Core
├── REST API
└── Background Worker
```

Depois:

```text
API
 |
 v
Message Queue
 |
 v
Monitoring Workers
 |
 v
MongoDB
```

Possíveis tecnologias:

- RabbitMQ
- AWS SQS
- Azure Service Bus

Não adicionar um broker antes de existir necessidade real.

---

# 36. Escalabilidade

Se houver múltiplos workers, evitar que todos executem o mesmo endpoint simultaneamente.

Possíveis soluções futuras:

- fila;
- distributed lock;
- scheduler central;
- job queue.

Não implementar complexidade prematuramente no MVP.

---

# 37. Regras de engenharia

1. Inspecionar a estrutura existente antes de implementar funcionalidades grandes.
2. Não recriar arquivos existentes sem necessidade.
3. Não trocar tecnologias sem justificativa.
4. Não adicionar dependências desnecessárias.
5. Manter o código compilando.
6. Executar testes após alterações relevantes.
7. Corrigir erros encontrados antes de avançar.
8. Manter o README atualizado.
9. Nunca colocar secrets no Git.
10. Não colocar regra de negócio nos controllers.
11. Utilizar dependency injection.
12. Utilizar async/await corretamente.
13. Utilizar CancellationToken em operações assíncronas relevantes.
14. Priorizar código legível e manutenível.
15. Testar regras importantes.
16. Não usar mocks para esconder funcionalidades ainda não implementadas.
17. Não declarar uma funcionalidade concluída sem verificar seu funcionamento.

---

# 38. Roadmap de implementação

## Sprint 1 — Setup

```text
[ ] Criar repository
[ ] Criar solução .NET
[ ] Criar React
[ ] Configurar PostgreSQL
[ ] Configurar MongoDB
[ ] Docker Compose
[ ] Swagger
```

## Sprint 2 — Endpoint Management

```text
[ ] MonitoredEndpoint
[ ] EF Core
[ ] Migration
[ ] Repository
[ ] Service
[ ] Controller
[ ] CRUD
[ ] Validation
```

## Sprint 3 — Monitoring Engine

```text
[ ] BackgroundService
[ ] IHttpClientFactory
[ ] Check execution
[ ] Latency measurement
[ ] Timeout
[ ] HTTP status
[ ] Error handling
[ ] MongoDB persistence
```

## Sprint 4 — Dashboard

```text
[ ] Dashboard
[ ] Endpoint list
[ ] Status badges
[ ] Metric cards
[ ] Latency chart
[ ] Uptime chart
[ ] Detail page
[ ] Check history
```

## Sprint 5 — Quality

```text
[ ] Unit tests
[ ] Integration tests
[ ] Health checks
[ ] Structured logs
[ ] ProblemDetails
[ ] CORS
```

## Sprint 6 — CI/CD

```text
[ ] GitLab CI
[ ] Build
[ ] Tests
[ ] Security scan
[ ] Docker build
[ ] Registry
```

## Sprint 7 — Cloud

```text
[ ] AWS/DigitalOcean
[ ] Deployment
[ ] Environment variables
[ ] Database
[ ] HTTPS
```

## Sprint 8 — Observability

```text
[ ] OpenTelemetry
[ ] Prometheus
[ ] Grafana
[ ] Metrics
[ ] Traces
```

---

# 39. Definition of Done — MVP

```text
[ ] Cadastrar endpoint
[ ] Editar endpoint
[ ] Excluir endpoint
[ ] Ativar/desativar monitoramento
[ ] Worker verificar endpoints automaticamente
[ ] Medir latência
[ ] Armazenar código HTTP
[ ] Identificar falhas
[ ] Persistir resultados no MongoDB
[ ] Persistir configurações no PostgreSQL
[ ] Dashboard funcionando
[ ] Gráficos funcionando
[ ] Histórico funcionando
[ ] Health check funcionando
[ ] Docker Compose subindo todo o ambiente
[ ] Testes principais passando
[ ] README atualizado
```

Fluxo mínimo demonstrável:

```text
Cadastrar API
      |
      v
Worker verifica API
      |
      v
Resultado salvo
      |
      v
Dashboard atualizado
      |
      v
Histórico disponível
      |
      v
Métricas calculadas
```

---

# 40. README final

O README do projeto deve conter:

- descrição;
- features;
- arquitetura;
- stack;
- requisitos;
- instruções para execução;
- Swagger;
- testes;
- CI/CD;
- decisões arquiteturais;
- evolução futura.

Comando principal:

```bash
docker compose up -d
```

---

# 41. Como apresentar em entrevista

A apresentação deve enfatizar problemas técnicos e decisões.

Exemplo de narrativa:

> Desenvolvi uma plataforma de monitoramento de APIs utilizando React e ASP.NET Core. O sistema permite cadastrar endpoints e executar verificações periódicas automaticamente através de um BackgroundService.
>
> Para persistência, utilizei PostgreSQL para os dados estruturais e MongoDB para os resultados das verificações, já que cada check gera um evento histórico independente.
>
> Cada execução registra status HTTP, latência, sucesso ou falha e informações de erro.
>
> Também implementei Docker Compose para reproduzir todo o ambiente localmente e GitLab CI/CD para automatizar build, testes e criação das imagens.
>
> A arquitetura foi pensada para posteriormente separar o worker da API e introduzir uma fila caso a quantidade de endpoints cresça.

---

# 42. Perguntas técnicas que o projeto deve permitir responder

### Por que PostgreSQL?

Os dados estruturais possuem relacionamentos claros e requisitos de consistência transacional.

### Por que MongoDB?

Os resultados são eventos históricos independentes, potencialmente numerosos, e o modelo documental é adequado para esse tipo de registro.

### Por que não guardar tudo no PostgreSQL?

Seria uma alternativa válida. A separação foi escolhida para demonstrar persistência poliglota e separar dados transacionais de eventos de monitoramento. Essa decisão deve ser tratada como uma escolha arquitetural, não como uma regra universal.

### Por que BackgroundService?

Porque as verificações precisam acontecer independentemente das requisições HTTP dos usuários.

### Por que não executar a verificação dentro do Controller?

Isso acoplaria o monitoramento ao ciclo de vida da requisição e não resolveria adequadamente o processamento periódico.

### Como escalar?

Separando o worker da API e, posteriormente, utilizando uma fila para distribuir verificações entre múltiplos workers.

### Como evitar dois workers verificando o mesmo endpoint?

Utilizar fila, distributed lock ou scheduler central em uma arquitetura distribuída.

### Como lidar com uma API lenta?

Timeout, CancellationToken e limite de concorrência.

### Como monitorar o próprio monitor?

Health checks, logs, métricas e OpenTelemetry.

### Como fazer deploy?

Containerizar os serviços e utilizar uma plataforma cloud como AWS ou DigitalOcean.

---

# 43. Instruções para o Claude Code

Você é o principal agente de desenvolvimento deste projeto.

Implemente o projeto de forma incremental e verificável.

## Regras

1. Antes de implementar uma funcionalidade grande, inspecione a estrutura existente.
2. Não recrie arquivos existentes sem necessidade.
3. Não altere tecnologias sem justificativa.
4. Não adicione dependências sem necessidade.
5. Mantenha o código compilando.
6. Execute testes após alterações relevantes.
7. Corrija erros antes de avançar.
8. Mantenha o README atualizado.
9. Nunca coloque secrets no código.
10. Não use mocks como substituição permanente de funcionalidades reais.
11. Não declare funcionalidades como concluídas sem verificá-las.
12. Se uma decisão arquitetural precisar ser tomada, prefira a solução simples e documente a decisão.

## Ordem obrigatória

```text
1. Repository structure
2. Backend setup
3. PostgreSQL
4. Endpoint CRUD
5. MongoDB
6. Monitoring engine
7. Background worker
8. Dashboard
9. Statistics
10. Tests
11. Health checks
12. Docker
13. GitLab CI/CD
14. Documentation
15. Optional observability
```

## Prioridade

O MVP funcional tem prioridade sobre tecnologias avançadas.

Não implementar Kubernetes, Terraform, AWS, RabbitMQ ou observabilidade avançada antes de o núcleo do sistema estar funcionando.

O código deve parecer um projeto profissional pequeno, e não um tutorial: priorize clareza, manutenção, separação de responsabilidades, testes, tratamento de erros, observabilidade e documentação.

---

# 44. Resultado esperado

Ao finalizar o MVP, o projeto deve demonstrar concretamente:

```text
React                 ✓
ASP.NET Core          ✓
C#                    ✓
PostgreSQL            ✓
MongoDB               ✓
Docker                ✓
Git/GitLab            ✓
GitLab CI/CD          ✓
Background Processing ✓
REST API              ✓
Automated Tests       ✓
Health Checks         ✓
Observability concepts ✓
```

O sistema deve ser executável localmente com:

```bash
docker compose up -d
```

E deve permitir demonstrar o fluxo completo:

```text
Cadastrar API
     ↓
Worker executa verificação
     ↓
Mede latência/status
     ↓
Salva CheckResult no MongoDB
     ↓
Atualiza status no PostgreSQL
     ↓
Dashboard consulta estatísticas
     ↓
Usuário visualiza histórico e falhas
```
