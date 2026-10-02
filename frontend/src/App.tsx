import { useEffect, useState } from 'react'
import ApplicationsPage from './components/ApplicationsPage'
import AdminUsersPage from './components/AdminUsersPage'
import DeploymentsPage from './components/DeploymentsPage'
import LoginPage, { type AuthSession } from './components/LoginPage'
import {
  EnvironmentsPage,
  InfrastructurePage,
  SecurityPage,
  AuditLogsPage,
} from './components/PlatformPages'
import './App.css'

type HealthResponse = {
  status: string
  service: string
  timestamp: string
}

type ConnectionStatus = 'checking' | 'online' | 'offline'

const navigation = [
  'Overview',
  'Applications',
  'Deployments',
  'Environments',
  'Infrastructure',
  'Security',
  'Audit Logs',
  'User Management',
]

function App() {
  const [session, setSession] = useState<AuthSession | null>(null)
  const [health, setHealth] = useState<HealthResponse | null>(null)
  const [connection, setConnection] = useState<ConnectionStatus>('checking')
  const [activePage, setActivePage] = useState('Overview')
  const [applicationCount, setApplicationCount] = useState<number | null>(null)

  async function checkHealth() {
    setConnection('checking')

    try {
      const response = await fetch('/api/health')

      if (!response.ok) {
        throw new Error(`API returned ${response.status}`)
      }

      const data: HealthResponse = await response.json()
      setHealth(data)
      setConnection(data.status === 'Healthy' ? 'online' : 'offline')
    } catch {
      setHealth(null)
      setConnection('offline')
    }
  }

  useEffect(() => {
    if (activePage !== 'Overview' || !session) return

    const accessToken = session.accessToken
    const controller = new AbortController()

    async function fetchApplicationCount() {
      try {
        const response = await fetch('/api/applications/', {
          signal: controller.signal,
          headers: { Authorization: `Bearer ${accessToken}` },
        })

        if (!response.ok) {
          throw new Error('Application API unavailable')
        }

        const applications: { id: string }[] = await response.json()

        if (!controller.signal.aborted) {
          setApplicationCount(applications.length)
        }
      } catch {
        if (!controller.signal.aborted) {
          setApplicationCount(null)
        }
      }
    }

    void fetchApplicationCount()

    return () => controller.abort()
  }, [activePage, session])

  useEffect(() => {
    const controller = new AbortController()

    async function loadHealth() {
      try {
        const response = await fetch('/api/health', {
          signal: controller.signal,
        })

        if (!response.ok) {
          throw new Error(`API returned ${response.status}`)
        }

        const data: HealthResponse = await response.json()

        if (!controller.signal.aborted) {
          setHealth(data)
          setConnection(data.status === 'Healthy' ? 'online' : 'offline')
        }
      } catch {
        if (!controller.signal.aborted) {
          setHealth(null)
          setConnection('offline')
        }
      }
    }

    void loadHealth()

    return () => controller.abort()
  }, [])

  if (!session) return <LoginPage onLogin={setSession} />

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 lg:flex">
      <aside className="border-b border-slate-800 bg-slate-900 lg:min-h-screen lg:w-64 lg:border-b-0 lg:border-r">
        <div className="border-b border-slate-800 px-6 py-7">
          <div className="text-2xl font-bold tracking-tight">
            <span className="text-sky-400">Cloud</span>Forge
          </div>
          <p className="mt-1 text-xs text-slate-400">
            Internal Developer Platform
          </p>
        </div>

        <nav aria-label="Main navigation" className="flex flex-wrap gap-2 p-4 lg:block">
          {navigation
            .filter(item =>
              item !== 'User Management' ||
              session.user.role === 'Admin'
            )
            .map((item) => (
            <button
              key={item}
              type="button"
              onClick={() => setActivePage(item)}
              aria-current={activePage === item ? 'page' : undefined}
              className={`rounded-lg px-4 py-3 text-left text-sm transition lg:mb-1 lg:w-full ${
                activePage === item
                  ? 'bg-sky-500/15 font-semibold text-sky-300'
                  : 'text-slate-400 hover:bg-slate-800 hover:text-white'
              }`}
            >
              {item}
            </button>
          ))}
        </nav>
      </aside>

      <main className="min-w-0 flex-1">
        <header className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-800 px-6 py-5 lg:px-10">
          <div>
            <p className="text-xs uppercase tracking-widest text-sky-400">
              Platform Console
            </p>
            <h1 className="mt-1 text-2xl font-semibold">{activePage}</h1>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-sm text-slate-300">{session.user.email} ({session.user.role})</span>
            <button type="button" onClick={() => setSession(null)} className="rounded-lg border border-slate-600 px-4 py-2 text-sm">Logout</button>
          </div>
        </header>

        <div className="mx-auto max-w-7xl space-y-8 p-6 lg:p-10">
          {activePage === 'Overview' ? (
            <>
              <section>
                <h2 className="text-3xl font-semibold tracking-tight">
                  Platform overview
                </h2>
                <p className="mt-2 text-slate-400">
                  Manage applications, infrastructure and secure deployments
                  from one developer platform.
                </p>
              </section>

              <section className="grid gap-5 md:grid-cols-3">
                <div className="panel">
                  <p className="text-sm text-slate-400">Backend connection</p>
                  <p className="mt-4 text-2xl font-semibold capitalize">
                    {connection}
                  </p>
                  <p className="mt-2 text-xs text-slate-500">
                    Live API connectivity
                  </p>
                </div>

                <div className="panel">
                  <p className="text-sm text-slate-400">Registered applications</p>
                  <p className="mt-4 text-2xl font-semibold">
                    {applicationCount === null ? 'Unavailable' : applicationCount}
                  </p>
                  <p className="mt-2 text-xs text-slate-500">
                    Applications registered in the current API instance
                  </p>
                </div>

                <div className="panel">
                  <p className="text-sm text-slate-400">Deployment pipeline</p>
                  <p className="mt-4 text-2xl font-semibold">Simulation active</p>
                  <p className="mt-2 text-xs text-slate-500">
                    Local deployment simulation; CI security workflows configured
                  </p>
                </div>
              </section>

              <section className="panel">
                <div className="flex flex-wrap items-center justify-between gap-4">
                  <div>
                    <h3 className="text-lg font-semibold">API health</h3>
                    <p className="mt-1 text-sm text-slate-400">
                      Live response from the ASP.NET Core backend
                    </p>
                  </div>

                  <button
                    type="button"
                    onClick={() => void checkHealth()}
                    className="rounded-lg bg-sky-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-sky-400"
                  >
                    Refresh status
                  </button>
                </div>

                <div className="mt-6 grid gap-4 border-t border-slate-800 pt-6 sm:grid-cols-2">
                  <div>
                    <p className="text-xs text-slate-400">Connection</p>
                    <p
                      role="status"
                      className={`mt-2 font-semibold ${
                        connection === 'online'
                          ? 'text-emerald-400'
                          : connection === 'offline'
                            ? 'text-rose-400'
                            : 'text-amber-400'
                      }`}
                    >
                      {connection === 'online'
                        ? 'Connected'
                        : connection === 'offline'
                          ? 'Disconnected'
                          : 'Checking...'}
                    </p>
                  </div>

                  <div>
                    <p className="text-xs text-slate-400">Service</p>
                    <p className="mt-2 font-semibold">
                      {health?.service ?? 'Unavailable'}
                    </p>
                  </div>

                  <div className="sm:col-span-2">
                    <p className="text-xs text-slate-400">Last API response</p>
                    <p className="mt-2 text-sm text-slate-300">
                      {health
                        ? new Date(health.timestamp).toLocaleString()
                        : 'No successful response'}
                    </p>
                  </div>
                </div>
              </section>

              <section className="panel">
                <h3 className="text-lg font-semibold">Platform roadmap</h3>
                <p className="mt-2 text-sm leading-7 text-slate-400">
                  Upcoming integrations include PostgreSQL application
                  registry, authentication and RBAC, GitHub Actions,
                  Docker, Kubernetes, Azure and Terraform.
                </p>
              </section>
            </>
          ) : activePage === 'Applications' ? (
            <ApplicationsPage
              token={session.accessToken}
              canDeploy={['Admin', 'Developer'].includes(session.user.role)}
            />
          ) : activePage === 'Deployments' ? (
            <DeploymentsPage
              token={session.accessToken}
              canDeploy={['Admin', 'Developer'].includes(session.user.role)}
            />
          ) : activePage === 'Environments' ? (
            <EnvironmentsPage />
          ) : activePage === 'Infrastructure' ? (
            <InfrastructurePage />
          ) : activePage === 'Security' ? (
            <SecurityPage role={session.user.role} />
          ) : activePage === 'Audit Logs' ? (
            <AuditLogsPage />
          ) : activePage === 'User Management' &&
              session.user.role === 'Admin' ? (
            <AdminUsersPage
              token={session.accessToken}
              currentUserId={session.user.id}
            />
          ) : (
            <section className="panel">
              <p className="text-xs uppercase tracking-widest text-sky-400">
                Upcoming module
              </p>
              <h2 className="mt-3 text-2xl font-semibold">{activePage}</h2>
              <p className="mt-3 text-slate-400">
                This module has not been implemented yet.
                We will connect it to real backend functionality
                during the next development stages.
              </p>
            </section>
          )}
        </div>
      </main>
    </div>
  )
}

export default App
