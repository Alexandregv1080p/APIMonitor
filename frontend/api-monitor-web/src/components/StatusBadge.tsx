import type { EndpointStatus } from '../types'

const LABELS: Record<EndpointStatus, string> = { UP: 'Online', DOWN: 'Fora do ar', UNKNOWN: 'Aguardando' }

export function StatusBadge({ status, enabled = true }: { status: EndpointStatus; enabled?: boolean }) {
  const variant = enabled ? status.toLowerCase() : 'paused'
  return (
    <span className={`badge badge--${variant}`}>
      <span className="badge__dot" aria-hidden />
      {enabled ? LABELS[status] : 'Pausado'}
    </span>
  )
}
