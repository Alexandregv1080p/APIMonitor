import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { CheckHistory } from '../components/CheckHistory'
import { ErrorState } from '../components/ErrorState'
import { LatencyChart } from '../components/LatencyChart'
import { LoadingState } from '../components/LoadingState'
import { MetricCard } from '../components/MetricCard'
import { StatusBadge } from '../components/StatusBadge'
import { UptimeChart } from '../components/UptimeChart'
import { useAsync } from '../hooks/useAsync'
import { toApiError } from '../services/api'
import { endpointService } from '../services/endpointService'
import { statisticsService } from '../services/statisticsService'
import { PERIODS, type CheckResult, type Period } from '../types'
import {
  formatDateTime,
  formatErrorType,
  formatLatency,
  formatNumber,
  formatPercent,
  formatRelative,
} from '../utils/format'

export function EndpointDetailsPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const [period, setPeriod] = useState<Period>('24h')
  const [refreshKey, setRefreshKey] = useState(0)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState<{ tone: 'up' | 'down'; text: string }>()

  const endpoint = useAsync((s) => endpointService.get(id, s), [id, refreshKey], 15_000)
  const stats = useAsync((s) => statisticsService.forEndpoint(id, period, s), [id, period, refreshKey], 30_000)

  if (!endpoint.data && endpoint.loading) return <LoadingState />
  if (!endpoint.data && endpoint.error) {
    return endpoint.error.status === 404 ? (
      <div className="state">
        <strong>API não encontrada</strong>
        <Link to="/" className="btn">
          Voltar ao dashboard
        </Link>
      </div>
    ) : (
      <ErrorState error={endpoint.error} onRetry={endpoint.reload} />
    )
  }
  if (!endpoint.data) return null
  const e = endpoint.data

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setNotice(undefined)
    try {
      await action()
      setRefreshKey((k) => k + 1)
    } catch (err) {
      setNotice({ tone: 'down', text: toApiError(err).message })
    } finally {
      setBusy(false)
    }
  }

  const checkNow = () =>
    run(async () => {
      const r: CheckResult = await endpointService.checkNow(id)
      setNotice(
        r.success
          ? { tone: 'up', text: `Online · HTTP ${r.statusCode} em ${formatLatency(r.latencyMs)}` }
          : { tone: 'down', text: `${formatErrorType(r.errorType)} · ${r.errorMessage ?? ''}` },
      )
    })

  const toggle = () => run(async () => void (await endpointService.setEnabled(id, !e.enabled)))

  const remove = () => {
    if (!window.confirm(`Excluir "${e.name}" e todo o histórico de verificações?`)) return
    run(async () => {
      await endpointService.remove(id)
      navigate('/')
    })
  }

  const s = stats.data

  return (
    <>
      <Link to="/" className="back">
        ← Dashboard
      </Link>

      <div className="page__header">
        <div className="min-w-0">
          <div className="title-row">
            <h1 className="truncate">{e.name}</h1>
            <StatusBadge status={e.lastStatus} enabled={e.enabled} />
          </div>
          <p className="muted truncate">
            <span className="method">{e.method}</span> {e.url}
          </p>
          <p className="muted small">Última verificação {formatRelative(e.lastCheckedAt)}</p>
        </div>
        <div className="actions">
          <button className="btn btn--primary" onClick={checkNow} disabled={busy}>
            Verificar agora
          </button>
          <Link to={`/endpoints/${id}/edit`} className="btn">
            Editar
          </Link>
          <button className="btn" onClick={toggle} disabled={busy}>
            {e.enabled ? 'Pausar' : 'Retomar'}
          </button>
          <button className="btn btn--danger" onClick={remove} disabled={busy}>
            Excluir
          </button>
        </div>
      </div>

      {notice && <div className={`alert alert--${notice.tone}`}>{notice.text}</div>}

      <div className="section-header">
        <h2>Desempenho</h2>
        <div className="segmented" role="group" aria-label="Período">
          {PERIODS.map((p) => (
            <button key={p} className={p === period ? 'is-active' : ''} onClick={() => setPeriod(p)}>
              {p}
            </button>
          ))}
        </div>
      </div>

      {!s && stats.loading && <LoadingState />}
      {!s && stats.error && <ErrorState error={stats.error} onRetry={stats.reload} />}
      {s && (
        <>
          <section className="metrics metrics--6">
            <MetricCard label="Uptime" value={formatPercent(s.uptimePercentage)} />
            <MetricCard label="Latência média" value={formatLatency(s.averageLatencyMs)} />
            <MetricCard label="Menor latência" value={formatLatency(s.minLatencyMs)} />
            <MetricCard label="Maior latência" value={formatLatency(s.maxLatencyMs)} />
            <MetricCard label="Verificações" value={formatNumber(s.totalChecks)} />
            <MetricCard label="Falhas" value={formatNumber(s.failedChecks)} tone={s.failedChecks > 0 ? 'down' : 'default'} />
          </section>

          <section className="charts">
            <div className="card">
              <h3>Latência</h3>
              {s.series.length ? <LatencyChart series={s.series} period={period} /> : <p className="empty">Sem dados no período.</p>}
            </div>
            <div className="card">
              <h3>Disponibilidade</h3>
              {s.series.length ? <UptimeChart series={s.series} period={period} /> : <p className="empty">Sem dados no período.</p>}
            </div>
          </section>
        </>
      )}

      <section className="card card--flush">
        <div className="card__header">
          <h2>Histórico</h2>
        </div>
        <CheckHistory endpointId={id} refreshKey={refreshKey} />
      </section>

      <section className="card">
        <h2>Configuração</h2>
        <dl className="config">
          <div><dt>Intervalo</dt><dd>{e.intervalSeconds} s</dd></div>
          <div><dt>Timeout</dt><dd>{formatLatency(e.timeoutMilliseconds)}</dd></div>
          <div><dt>Status esperado</dt><dd>{e.expectedStatusCode}</dd></div>
          <div><dt>Criado em</dt><dd>{formatDateTime(e.createdAt)}</dd></div>
          <div><dt>Atualizado em</dt><dd>{formatDateTime(e.updatedAt)}</dd></div>
        </dl>
      </section>
    </>
  )
}
