import { useNavigate } from 'react-router-dom'
import type { DashboardEndpoint } from '../types'
import { formatLatency, formatPercent, formatRelative } from '../utils/format'
import { StatusBadge } from './StatusBadge'

export function EndpointTable({ endpoints }: { endpoints: DashboardEndpoint[] }) {
  const navigate = useNavigate()
  const open = (id: string) => navigate(`/endpoints/${id}`)

  return (
    <div className="table-wrap">
      <table className="table table--clickable">
        <thead>
          <tr>
            <th>API</th>
            <th>Status</th>
            <th className="num hide-sm">Latência</th>
            <th className="num">Uptime</th>
            <th className="hide-sm">Última verificação</th>
          </tr>
        </thead>
        <tbody>
          {endpoints.map((e) => (
            <tr
              key={e.id}
              tabIndex={0}
              onClick={() => open(e.id)}
              onKeyDown={(ev) => ev.key === 'Enter' && open(e.id)}
            >
              <td>
                <div className="cell-title">{e.name}</div>
                <div className="cell-sub">
                  <span className="method">{e.method}</span> {e.url}
                </div>
              </td>
              <td>
                <StatusBadge status={e.status} enabled={e.enabled} />
              </td>
              <td className="num hide-sm">{formatLatency(e.averageLatencyMs)}</td>
              <td className="num">
                <UptimeValue value={e.uptimePercentage} />
              </td>
              <td className="hide-sm muted">{formatRelative(e.lastCheckedAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function UptimeValue({ value }: { value: number | null }) {
  const tone = value == null ? '' : value >= 99 ? 'text-up' : value >= 95 ? 'text-warn' : 'text-down'
  return <span className={tone}>{formatPercent(value)}</span>
}
