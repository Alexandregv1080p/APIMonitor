import { useState, type FormEvent, type ReactNode } from 'react'
import { toApiError, type ApiError } from '../services/api'
import { HTTP_METHODS, type EndpointInput, type HttpMethod } from '../types'

const DEFAULT_INPUT: EndpointInput = {
  name: '',
  url: '',
  method: 'GET',
  intervalSeconds: 60,
  timeoutMilliseconds: 5000,
  expectedStatusCode: 200,
  enabled: true,
}

interface Props {
  initial?: EndpointInput
  submitLabel: string
  onSubmit: (input: EndpointInput) => Promise<void>
  onCancel: () => void
}

export function EndpointForm({ initial = DEFAULT_INPUT, submitLabel, onSubmit, onCancel }: Props) {
  const [values, setValues] = useState(initial)
  const [error, setError] = useState<ApiError>()
  const [saving, setSaving] = useState(false)

  const set = <K extends keyof EndpointInput>(key: K, value: EndpointInput[K]) =>
    setValues((v) => ({ ...v, [key]: value }))

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)
    try {
      await onSubmit({ ...values, name: values.name.trim(), url: values.url.trim() })
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setSaving(false)
    }
  }

  const fieldError = (key: keyof EndpointInput) => error?.fieldErrors[key]?.[0]
  const hasFieldErrors = error && Object.keys(error.fieldErrors).length > 0

  return (
    <form className="card form" onSubmit={handleSubmit}>
      {error && !hasFieldErrors && <div className="alert">{error.message}</div>}

      <Field label="Nome" error={fieldError('name')}>
        <input
          required
          maxLength={200}
          value={values.name}
          onChange={(e) => set('name', e.target.value)}
          placeholder="GitHub API"
          autoFocus
        />
      </Field>

      <div className="form__row form__row--url">
        <Field label="Método" error={fieldError('method')}>
          <select value={values.method} onChange={(e) => set('method', e.target.value as HttpMethod)}>
            {HTTP_METHODS.map((m) => (
              <option key={m}>{m}</option>
            ))}
          </select>
        </Field>
        <Field label="URL" error={fieldError('url')}>
          <input
            required
            type="url"
            maxLength={2048}
            value={values.url}
            onChange={(e) => set('url', e.target.value)}
            placeholder="https://api.github.com"
          />
        </Field>
      </div>

      <div className="form__row">
        <Field label="Intervalo (s)" hint="Mínimo 10 s" error={fieldError('intervalSeconds')}>
          <input
            required
            type="number"
            min={10}
            max={86400}
            value={values.intervalSeconds}
            onChange={(e) => set('intervalSeconds', e.target.valueAsNumber)}
          />
        </Field>
        <Field label="Timeout (ms)" hint="Até o intervalo" error={fieldError('timeoutMilliseconds')}>
          <input
            required
            type="number"
            min={100}
            max={60000}
            step={100}
            value={values.timeoutMilliseconds}
            onChange={(e) => set('timeoutMilliseconds', e.target.valueAsNumber)}
          />
        </Field>
        <Field label="Status esperado" error={fieldError('expectedStatusCode')}>
          <input
            required
            type="number"
            min={100}
            max={599}
            value={values.expectedStatusCode}
            onChange={(e) => set('expectedStatusCode', e.target.valueAsNumber)}
          />
        </Field>
      </div>

      <label className="checkbox">
        <input type="checkbox" checked={values.enabled} onChange={(e) => set('enabled', e.target.checked)} />
        Monitoramento ativo
      </label>

      <div className="form__actions">
        <button type="button" className="btn btn--ghost" onClick={onCancel}>
          Cancelar
        </button>
        <button type="submit" className="btn btn--primary" disabled={saving}>
          {saving ? 'Salvando…' : submitLabel}
        </button>
      </div>
    </form>
  )
}

function Field({ label, hint, error, children }: { label: string; hint?: string; error?: string; children: ReactNode }) {
  return (
    <label className={`field${error ? ' field--invalid' : ''}`}>
      <span className="field__label">{label}</span>
      {children}
      {error ? <span className="field__error">{error}</span> : hint && <span className="field__hint">{hint}</span>}
    </label>
  )
}
