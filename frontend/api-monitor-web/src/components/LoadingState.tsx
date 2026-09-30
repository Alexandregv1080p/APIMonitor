export function LoadingState({ label = 'Carregando…' }: { label?: string }) {
  return (
    <div className="state" role="status">
      <span className="spinner" aria-hidden />
      {label}
    </div>
  )
}
