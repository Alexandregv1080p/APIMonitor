import type { Period, StatisticsPoint } from '../types'
import { formatBucket } from '../utils/format'

interface Props {
  period: Period
  format: (point: StatisticsPoint) => [string, string][]
  // Injetados pelo Recharts
  active?: boolean
  payload?: readonly { payload: StatisticsPoint }[]
}

export function ChartTooltip({ active, payload, period, format }: Props) {
  const point = payload?.[0]?.payload
  if (!active || !point) return null
  return (
    <div className="chart-tooltip">
      <div className="chart-tooltip__title">{formatBucket(point.bucketStart, period)}</div>
      {format(point).map(([label, value]) => (
        <div key={label} className="chart-tooltip__row">
          <span>{label}</span>
          <strong>{value}</strong>
        </div>
      ))}
    </div>
  )
}
