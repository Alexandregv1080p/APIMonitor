import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { Period, StatisticsPoint } from '../types'
import { formatBucket, formatPercent } from '../utils/format'
import { ChartTooltip } from './ChartTooltip'

const colorFor = (uptime: number | null) =>
  uptime == null ? 'var(--unknown)' : uptime >= 99 ? 'var(--up)' : uptime >= 95 ? 'var(--warn)' : 'var(--down)'

export function UptimeChart({ series, period }: { series: StatisticsPoint[]; period: Period }) {
  return (
    <ResponsiveContainer width="100%" height={240}>
      <BarChart data={series} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
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
          domain={[0, 100]}
          stroke="var(--muted)"
          fontSize={12}
          tickLine={false}
          axisLine={false}
          width={56}
          tickFormatter={(v: number) => `${v}%`}
        />
        <Tooltip
          cursor={{ fill: 'var(--surface-2)' }}
          content={
            <ChartTooltip
              period={period}
              format={(p) => [['Uptime', formatPercent(p.uptimePercentage)], ['Falhas', `${p.failedChecks} de ${p.totalChecks}`]]}
            />
          }
        />
        <Bar dataKey="uptimePercentage" radius={[3, 3, 0, 0]} maxBarSize={28} isAnimationActive={false}>
          {series.map((p) => (
            <Cell key={p.bucketStart} fill={colorFor(p.uptimePercentage)} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}
