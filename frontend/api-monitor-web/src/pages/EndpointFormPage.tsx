import { Link, useNavigate, useParams } from 'react-router-dom'
import { EndpointForm } from '../components/EndpointForm'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { useAsync } from '../hooks/useAsync'
import { endpointService } from '../services/endpointService'
import type { Endpoint, EndpointInput } from '../types'

const toInput = ({ name, url, method, intervalSeconds, timeoutMilliseconds, expectedStatusCode, enabled }: Endpoint): EndpointInput =>
  ({ name, url, method, intervalSeconds, timeoutMilliseconds, expectedStatusCode, enabled })

/** Criação (/endpoints/new) e edição (/endpoints/:id/edit). */
export function EndpointFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const existing = useAsync((s) => (id ? endpointService.get(id, s) : Promise.resolve(undefined)), [id])

  if (id && !existing.data) {
    if (existing.error) return <ErrorState error={existing.error} onRetry={existing.reload} />
    return <LoadingState />
  }

  const submit = async (input: EndpointInput) => {
    const saved = id ? await endpointService.update(id, input) : await endpointService.create(input)
    navigate(`/endpoints/${saved.id}`)
  }

  return (
    <div className="narrow">
      <Link to={id ? `/endpoints/${id}` : '/'} className="back">
        ← Voltar
      </Link>
      <h1>{id ? 'Editar API' : 'Nova API'}</h1>
      <p className="muted">
        {id ? 'Alterações valem a partir da próxima verificação.' : 'A primeira verificação acontece em poucos segundos.'}
      </p>
      <EndpointForm
        initial={existing.data ? toInput(existing.data) : undefined}
        submitLabel={id ? 'Salvar alterações' : 'Cadastrar API'}
        onSubmit={submit}
        onCancel={() => navigate(-1)}
      />
    </div>
  )
}
