import type { CheckErrorType, Period } from '../types'

const EMPTY = '—'
const percent = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 })
const integer = new Intl.NumberFormat('pt-BR')
const relative = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto', style: 'short' })

export function formatLatency(ms: number | null | undefined): string {
  if (ms == null) return EMPTY
  return ms >= 1000 ? `${(ms / 1000).toFixed(2).replace('.', ',')} s` : `${Math.round(ms)} ms`
}

export function formatPercent(value: number | null | undefined): string {
  return value == null ? EMPTY : `${percent.format(value)}%`
}

export function formatNumber(value: number): string {
  return integer.format(value)
}

export function formatBytes(bytes: number | null | undefined): string {
  if (bytes == null) return EMPTY
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1).replace('.', ',')} KB`
  return `${(bytes / 1024 / 1024).toFixed(1).replace('.', ',')} MB`
}

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return EMPTY
  return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'medium' })
}

export function formatRelative(iso: string | null | undefined, now = Date.now()): string {
  if (!iso) return 'nunca'
  const seconds = Math.round((new Date(iso).getTime() - now) / 1000)
  const abs = Math.abs(seconds)
  if (abs < 60) return relative.format(seconds, 'second')
  if (abs < 3600) return relative.format(Math.round(seconds / 60), 'minute')
  if (abs < 86400) return relative.format(Math.round(seconds / 3600), 'hour')
  return relative.format(Math.round(seconds / 86400), 'day')
}

/** Rótulo do eixo X de acordo com a granularidade do período. */
export function formatBucket(iso: string, period: Period): string {
  const d = new Date(iso)
  if (period === '24h') return d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
  if (period === '7d')
    return `${d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })} ${d.getHours()}h`
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })
}

const ERROR_LABELS: Record<CheckErrorType, string> = {
  TIMEOUT: 'Timeout',
  DNS_ERROR: 'Erro de DNS',
  CONNECTION_ERROR: 'Erro de conexão',
  UNEXPECTED_STATUS_CODE: 'Status inesperado',
}

export const formatErrorType = (type: CheckErrorType | null) => (type ? ERROR_LABELS[type] : EMPTY)
