import axios from 'axios'

// Em dev o Vite faz proxy de /api para o backend; em produção o nginx faz o mesmo.
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  timeout: 30_000,
})

export interface ApiError {
  status?: number
  message: string
  /** Erros de validação por campo, com chaves em camelCase (ex.: "url", "intervalSeconds"). */
  fieldErrors: Record<string, string[]>
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

const camelCase = (key: string) => key.charAt(0).toLowerCase() + key.slice(1)

/** Converte qualquer falha (rede, ProblemDetails, erro inesperado) numa forma única para a UI. */
export function toApiError(error: unknown): ApiError {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const problem = error.response?.data
    if (!error.response) {
      return { message: 'Não foi possível conectar à API.', fieldErrors: {} }
    }
    const fieldErrors = Object.fromEntries(
      Object.entries(problem?.errors ?? {}).map(([k, v]) => [camelCase(k), v]),
    )
    return {
      status: error.response.status,
      message: problem?.detail ?? problem?.title ?? `Erro ${error.response.status}`,
      fieldErrors,
    }
  }
  return { message: error instanceof Error ? error.message : 'Erro inesperado.', fieldErrors: {} }
}
