import { Link, NavLink, Outlet } from 'react-router-dom'

export function AppLayout() {
  return (
    <>
      <header className="topbar">
        <div className="container topbar__inner">
          <Link to="/" className="brand">
            <svg viewBox="0 0 32 32" width="26" height="26" aria-hidden>
              <rect width="32" height="32" rx="8" fill="var(--surface-2)" />
              <path d="M5 17h5l3-7 5 13 3-6h6" fill="none" stroke="var(--up)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            API Monitor
          </Link>
          <nav className="topbar__nav">
            <NavLink to="/" end>
              Dashboard
            </NavLink>
            <Link to="/endpoints/new" className="btn btn--primary btn--sm">
              + Nova API
            </Link>
          </nav>
        </div>
      </header>
      <main className="container page">
        <Outlet />
      </main>
    </>
  )
}
