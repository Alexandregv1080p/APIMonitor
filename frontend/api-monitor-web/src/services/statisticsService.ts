import { api } from './api'
import type { Dashboard, EndpointStatistics, Period } from '../types'

export const statisticsService = {
  dashboard: (signal?: AbortSignal) => api.get<Dashboard>('/dashboard', { signal }).then((r) => r.data),

  forEndpoint: (id: string, period: Period, signal?: AbortSignal) =>
    api
      .get<EndpointStatistics>(`/endpoints/${id}/statistics`, { params: { period }, signal })
      .then((r) => r.data),
}
