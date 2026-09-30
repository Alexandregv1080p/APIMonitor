import { useCallback, useEffect, useState } from 'react'
import { toApiError, type ApiError } from '../services/api'

interface AsyncState<T> {
  data?: T
  error?: ApiError
  loading: boolean
}

/**
 * Carrega dados com cancelamento automático e, opcionalmente, recarrega a cada `refreshMs`.
 * Mantém o último dado durante recargas para a tela não "piscar".
 */
export function useAsync<T>(
  load: (signal: AbortSignal) => Promise<T>,
  deps: readonly unknown[],
  refreshMs?: number,
) {
  const [state, setState] = useState<AsyncState<T>>({ loading: true })
  const [tick, setTick] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState((s) => ({ ...s, loading: true }))
    load(controller.signal).then(
      (data) => !controller.signal.aborted && setState({ data, loading: false }),
      (error) =>
        !controller.signal.aborted &&
        setState((s) => ({ data: s.data, error: toApiError(error), loading: false })),
    )
    return () => controller.abort()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick])

  useEffect(() => {
    if (!refreshMs) return
    const id = setInterval(() => {
      if (document.visibilityState === 'visible') setTick((t) => t + 1)
    }, refreshMs)
    return () => clearInterval(id)
  }, [refreshMs])

  const reload = useCallback(() => setTick((t) => t + 1), [])
  return { ...state, reload }
}
