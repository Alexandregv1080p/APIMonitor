// Espelham os DTOs da API (enums serializados em maiúsculas).

export type EndpointStatus = 'UP' | 'DOWN' | 'UNKNOWN'
export type HttpMethod = 'GET' | 'HEAD' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | 'OPTIONS'
export type CheckErrorType = 'TIMEOUT' | 'DNS_ERROR' | 'CONNECTION_ERROR' | 'UNEXPECTED_STATUS_CODE'
export type Period = '24h' | '7d' | '30d'

export const HTTP_METHODS: HttpMethod[] = ['GET', 'HEAD', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS']
export const PERIODS: Period[] = ['24h', '7d', '30d']

export interface EndpointInput {
  name: string
  url: string
  method: HttpMethod
  intervalSeconds: number
  timeoutMilliseconds: number
  expectedStatusCode: number
  enabled: boolean
}

export interface Endpoint extends EndpointInput {
  id: string
  createdAt: string
  updatedAt: string
  lastCheckedAt: string | null
  lastStatus: EndpointStatus
}

export interface CheckResult {
  checkedAt: string
  success: boolean
  statusCode: number | null
  latencyMs: number
  responseSizeBytes: number | null
  errorType: CheckErrorType | null
  errorMessage: string | null
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface StatisticsPoint {
  bucketStart: string
  totalChecks: number
  failedChecks: number
  uptimePercentage: number | null
  averageLatencyMs: number | null
}

export interface EndpointStatistics {
  period: Period
  uptimePercentage: number | null
  averageLatencyMs: number | null
  minLatencyMs: number | null
  maxLatencyMs: number | null
  totalChecks: number
  successfulChecks: number
  failedChecks: number
  series: StatisticsPoint[]
}

export interface DashboardEndpoint {
  id: string
  name: string
  url: string
  method: HttpMethod
  enabled: boolean
  status: EndpointStatus
  lastCheckedAt: string | null
  averageLatencyMs: number | null
  uptimePercentage: number | null
}

export interface Dashboard {
  totalEndpoints: number
  upEndpoints: number
  downEndpoints: number
  averageLatencyMs: number | null
  uptimePercentage: number | null
  failedChecksLast24Hours: number
  endpoints: DashboardEndpoint[]
}
