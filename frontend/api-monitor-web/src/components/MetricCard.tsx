import type { ReactNode } from 'react'

interface Props {
  label: string
  value: ReactNode
  hint?: ReactNode
  tone?: 'default' | 'up' | 'down'
}

export function MetricCard({ label, value, hint, tone = 'default' }: Props) {
  return (
    <div className={`card metric metric--${tone}`}>
      <span className="metric__label">{label}</span>
      <span className="metric__value">{value}</span>
      {hint && <span className="metric__hint">{hint}</span>}
    </div>
  )
}
