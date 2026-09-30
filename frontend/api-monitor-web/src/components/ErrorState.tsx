import type { ApiError } from '../services/api'

export function ErrorState({ error, onRetry }: { error: ApiError; onRetry?: () => void }) {
  return (
    <div className="state state--error" role="alert">
      <strong>Algo deu errado</strong>
      <span>{error.message}</span>
      {onRetry && (
        <button className="btn" onClick={onRetry}>
          Tentar novamente
        </button>
      )}
    </div>
  )
}
