import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { Period, StatisticsPoint } from '../types'
import { formatBucket, formatLatency } from '../utils/format'
import { ChartTooltip } from './ChartTooltip'

export function LatencyChart({ series, period }: { series: StatisticsPoint[]; period: Period }) {
  return (
    <ResponsiveContainer width="100%" height={240}>
      <AreaChart data={series} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
        <defs>
          <linearGradient id="latencyFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--accent)" stopOpacity={0.35} />
            <stop offset="100%" stopColor="var(--accent)" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" vertical={false} />
        <XAxis
          dataKey="bucketStart"
          tickFormatter={(v: string) => formatBucket(v, period)}
          stroke="var(--muted)"
          fontSize={12}
          tickLine={false}
          axisLine={false}
          minTickGap={24}
        />
        <YAxis
          stroke="var(--muted)"
          fontSize={12}
          tickLine={false}
          axisLine={false}
          width={56}
          tickFormatter={(v: number) => formatLatency(v)}
        />
        <Tooltip
          content={
            <ChartTooltip
              period={period}
              format={(p) => [['Latência média', formatLatency(p.averageLatencyMs)], ['Verificações', String(p.totalChecks)]]}
            />
          }
        />
        <Area
          type="monotone"
          dataKey="averageLatencyMs"
          stroke="var(--accent)"
          strokeWidth={2}
          fill="url(#latencyFill)"
          connectNulls
          isAnimationActive={false}
        />
      </AreaChart>
    </ResponsiveContainer>
  )
}
