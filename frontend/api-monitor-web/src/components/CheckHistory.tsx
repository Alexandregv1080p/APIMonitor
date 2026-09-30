import { useState } from 'react'
import { useAsync } from '../hooks/useAsync'
import { endpointService } from '../services/endpointService'
import { formatBytes, formatDateTime, formatErrorType, formatLatency, formatNumber } from '../utils/format'
import { ErrorState } from './ErrorState'
import { LoadingState } from './LoadingState'

const PAGE_SIZE = 20

export function CheckHistory({ endpointId, refreshKey }: { endpointId: string; refreshKey: number }) {
  const [page, setPage] = useState(1)
  const { data, error, loading, reload } = useAsync(
    (signal) => endpointService.history(endpointId, page, PAGE_SIZE, signal),
    [endpointId, page, refreshKey],
    page === 1 ? 15_000 : undefined, // só a primeira página acompanha em tempo real
  )

  if (!data && loading) return <LoadingState />
  if (!data && error) return <ErrorState error={error} onRetry={reload} />
  if (!data) return null
  if (data.total === 0) return <p className="empty">Nenhuma verificação registrada ainda.</p>

  const pages = Math.max(1, Math.ceil(data.total / PAGE_SIZE))

  return (
    <>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Horário</th>
              <th>Resultado</th>
              <th className="num">HTTP</th>
              <th className="num">Latência</th>
              <th className="num hide-sm">Tamanho</th>
              <th className="hide-sm">Detalhe</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((c) => (
              <tr key={c.checkedAt}>
                <td className="mono">{formatDateTime(c.checkedAt)}</td>
                <td>
                  <span className={c.success ? 'text-up' : 'text-down'}>
                    {c.success ? 'Sucesso' : formatErrorType(c.errorType)}
                  </span>
                </td>
                <td className="num">{c.statusCode ?? '—'}</td>
                <td className="num">{formatLatency(c.latencyMs)}</td>
                <td className="num hide-sm">{formatBytes(c.responseSizeBytes)}</td>
                <td className="hide-sm muted truncate" title={c.errorMessage ?? undefined}>
                  {c.errorMessage ?? ''}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="pager">
        <span className="muted">
          {formatNumber(data.total)} verificações · página {page} de {pages}
        </span>
        <div className="pager__buttons">
          <button className="btn btn--sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Anterior
          </button>
          <button className="btn btn--sm" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>
            Próxima
          </button>
        </div>
      </div>
    </>
  )
}
