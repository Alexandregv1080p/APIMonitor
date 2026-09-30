import { describe, expect, it } from 'vitest'
import { formatBytes, formatErrorType, formatLatency, formatPercent, formatRelative } from './format'

describe('formatLatency', () => {
  it('mostra traço quando não há dado', () => {
    expect(formatLatency(null)).toBe('—')
    expect(formatLatency(undefined)).toBe('—')
  })
  it('usa ms abaixo de 1 s e segundos acima', () => {
    expect(formatLatency(142.6)).toBe('143 ms')
    expect(formatLatency(1500)).toBe('1,50 s')
  })
})

describe('formatPercent', () => {
  it('formata em pt-BR com até 2 casas', () => {
    expect(formatPercent(99.82)).toBe('99,82%')
    expect(formatPercent(100)).toBe('100%')
  })
  it('distingue "sem dados" de 0%', () => {
    expect(formatPercent(null)).toBe('—')
    expect(formatPercent(0)).toBe('0%')
  })
})

describe('formatBytes', () => {
  it('escolhe a unidade', () => {
    expect(formatBytes(512)).toBe('512 B')
    expect(formatBytes(2048)).toBe('2,0 KB')
    expect(formatBytes(5 * 1024 * 1024)).toBe('5,0 MB')
  })
})

describe('formatRelative', () => {
  const now = Date.parse('2026-09-30T12:00:00Z')

  it('trata nunca verificado', () => expect(formatRelative(null, now)).toBe('nunca'))

  it('escolhe segundos, minutos, horas ou dias', () => {
    expect(formatRelative('2026-09-30T11:59:48Z', now)).toMatch(/12/)
    expect(formatRelative('2026-09-30T11:55:00Z', now)).toMatch(/5 min/)
    expect(formatRelative('2026-09-30T09:00:00Z', now)).toMatch(/3 h/)
    expect(formatRelative('2026-09-27T12:00:00Z', now)).toMatch(/3 dias/)
  })
})

describe('formatErrorType', () => {
  it('traduz os tipos de erro da API', () => {
    expect(formatErrorType('TIMEOUT')).toBe('Timeout')
    expect(formatErrorType('UNEXPECTED_STATUS_CODE')).toBe('Status inesperado')
    expect(formatErrorType(null)).toBe('—')
  })
})
