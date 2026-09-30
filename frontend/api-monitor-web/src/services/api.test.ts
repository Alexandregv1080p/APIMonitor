import { AxiosError, AxiosHeaders, type AxiosResponse } from 'axios'
import { describe, expect, it } from 'vitest'
import { toApiError } from './api'

const responseError = (status: number, data: unknown) =>
  new AxiosError('Request failed', 'ERR_BAD_REQUEST', undefined, undefined, {
    status,
    data,
    statusText: '',
    headers: {},
    config: { headers: new AxiosHeaders() },
  } as AxiosResponse)

describe('toApiError', () => {
  it('extrai erros de validação com chaves em camelCase', () => {
    const error = toApiError(
      responseError(400, {
        title: 'Validation failed',
        errors: { Url: ['bad url'], IntervalSeconds: ['too small'] },
      }),
    )

    expect(error.status).toBe(400)
    expect(error.message).toBe('Validation failed')
    expect(error.fieldErrors).toEqual({ url: ['bad url'], intervalSeconds: ['too small'] })
  })

  it('prefere o detail do ProblemDetails', () => {
    const error = toApiError(responseError(404, { title: 'Endpoint not found', detail: 'Not here.' }))
    expect(error).toEqual({ status: 404, message: 'Not here.', fieldErrors: {} })
  })

  it('reconhece falha de rede (sem resposta)', () => {
    const error = toApiError(new AxiosError('Network Error', 'ERR_NETWORK'))
    expect(error.message).toBe('Não foi possível conectar à API.')
    expect(error.status).toBeUndefined()
  })

  it('aceita erros que não são do Axios', () => {
    expect(toApiError(new Error('boom')).message).toBe('boom')
    expect(toApiError('???').message).toBe('Erro inesperado.')
  })
})
