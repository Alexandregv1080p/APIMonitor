import { lazy, Suspense } from 'react'
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { LoadingState } from './components/LoadingState'
import { AppLayout } from './layouts/AppLayout'
import { DashboardPage } from './pages/DashboardPage'
import { EndpointFormPage } from './pages/EndpointFormPage'

// Recharts só é baixado quando se abre a página de detalhes.
const EndpointDetailsPage = lazy(() =>
  import('./pages/EndpointDetailsPage').then((m) => ({ default: m.EndpointDetailsPage })),
)

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="endpoints/new" element={<EndpointFormPage />} />
          <Route
            path="endpoints/:id"
            element={
              <Suspense fallback={<LoadingState />}>
                <EndpointDetailsPage />
              </Suspense>
            }
          />
          <Route path="endpoints/:id/edit" element={<EndpointFormPage />} />
          <Route
            path="*"
            element={
              <div className="state">
                <strong>Página não encontrada</strong>
                <Link to="/" className="btn">Voltar ao dashboard</Link>
              </div>
            }
          />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
