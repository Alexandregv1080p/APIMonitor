import { api } from './api'
import type { CheckResult, Endpoint, EndpointInput, Paged } from '../types'

export const endpointService = {
  get: (id: string, signal?: AbortSignal) =>
    api.get<Endpoint>(`/endpoints/${id}`, { signal }).then((r) => r.data),

  create: (input: EndpointInput) => api.post<Endpoint>('/endpoints', input).then((r) => r.data),

  update: (id: string, input: EndpointInput) =>
    api.put<Endpoint>(`/endpoints/${id}`, input).then((r) => r.data),

  setEnabled: (id: string, enabled: boolean) =>
    api.patch<Endpoint>(`/endpoints/${id}/status`, { enabled }).then((r) => r.data),

  remove: (id: string) => api.delete(`/endpoints/${id}`),

  checkNow: (id: string) => api.post<CheckResult>(`/endpoints/${id}/check`).then((r) => r.data),

  history: (id: string, page: number, pageSize: number, signal?: AbortSignal) =>
    api
      .get<Paged<CheckResult>>(`/endpoints/${id}/checks`, { params: { page, pageSize }, signal })
      .then((r) => r.data),
}
