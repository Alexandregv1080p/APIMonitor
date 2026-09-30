import { Link } from 'react-router-dom'
import { EndpointTable } from '../components/EndpointTable'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { MetricCard } from '../components/MetricCard'
import { useAsync } from '../hooks/useAsync'
import { statisticsService } from '../services/statisticsService'
import { formatLatency, formatNumber, formatPercent } from '../utils/format'

export function DashboardPage() {
  const { data, error, loading, reload } = useAsync((signal) => statisticsService.dashboard(signal), [], 10_000)

  if (!data && loading) return <LoadingState />
  if (!data && error) return <ErrorState error={error} onRetry={reload} />
  if (!data) return null

  const unknown = data.totalEndpoints - data.upEndpoints - data.downEndpoints

  return (
    <>
      <div className="page__header">
        <div>
          <h1>Dashboard</h1>
          <p className="muted">Métricas das últimas 24 horas · atualiza a cada 10 s</p>
        </div>
        {error && <span className="stale">Sem conexão — exibindo últimos dados</span>}
      </div>

      <section className="metrics">
        <MetricCard
          label="APIs monitoradas"
          value={formatNumber(data.totalEndpoints)}
          hint={
            <>
              <span className="text-up">{data.upEndpoints} online</span> ·{' '}
              <span className={data.downEndpoints ? 'text-down' : ''}>{data.downEndpoints} fora</span>
              {unknown > 0 && <> · {unknown} sem status</>}
            </>
          }
        />
        <MetricCard
          label="Uptime"
          value={formatPercent(data.uptimePercentage)}
          tone={data.uptimePercentage == null ? 'default' : data.uptimePercentage >= 99 ? 'up' : 'down'}
        />
        <MetricCard label="Latência média" value={formatLatency(data.averageLatencyMs)} />
        <MetricCard
          label="Falhas"
          value={formatNumber(data.failedChecksLast24Hours)}
          tone={data.failedChecksLast24Hours > 0 ? 'down' : 'default'}
          hint="verificações com erro"
        />
      </section>

      <section className="card card--flush">
        <div className="card__header">
          <h2>APIs</h2>
        </div>
        {data.endpoints.length === 0 ? (
          <div className="empty">
            <p>Nenhuma API cadastrada ainda.</p>
            <Link to="/endpoints/new" className="btn btn--primary">
              Cadastrar a primeira API
            </Link>
          </div>
        ) : (
          <EndpointTable endpoints={data.endpoints} />
        )}
      </section>
    </>
  )
}
