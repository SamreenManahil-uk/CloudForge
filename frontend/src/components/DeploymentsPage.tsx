import { useCallback, useEffect, useState, type FormEvent } from 'react'

type Application = {
  id: string
  name: string
}

type Deployment = {
  id: string
  applicationId: string
  environment: string
  version: string
  status: string
  createdAt: string
  completedAt: string | null
}

type Props = {
  token: string
  canDeploy: boolean
}

export default function DeploymentsPage({ token, canDeploy }: Props) {
  const [applications, setApplications] = useState<Application[]>([])
  const [deployments, setDeployments] = useState<Deployment[]>([])
  const [applicationId, setApplicationId] = useState('')
  const [environment, setEnvironment] = useState('development')
  const [version, setVersion] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const loadData = useCallback(async (signal?: AbortSignal) => {
    const headers = {
      Authorization: `Bearer ${token}`,
    }

    const [appsResponse, deploymentsResponse] = await Promise.all([
      fetch('/api/applications/', { headers, signal }),
      fetch('/api/deployments/', { headers, signal }),
    ])

    if (!appsResponse.ok || !deploymentsResponse.ok) {
      throw new Error('Unable to load deployment data.')
    }

    const apps: Application[] = await appsResponse.json()
    const records: Deployment[] = await deploymentsResponse.json()

    if (!signal?.aborted) {
      setApplications(apps)
      setDeployments(records)
    }
  }, [token])

  useEffect(() => {
    const controller = new AbortController()

    async function load() {
      try {
        await loadData(controller.signal)
      } catch (err) {
        if (!controller.signal.aborted) {
          setError(
            err instanceof Error ? err.message : 'Loading failed.'
          )
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    void load()

    const interval = window.setInterval(() => {
      void loadData(controller.signal).catch(err => {
        if (!controller.signal.aborted) {
          console.error('Deployment refresh failed:', err)
        }
      })
    }, 3000)

    return () => {
      controller.abort()
      window.clearInterval(interval)
    }
  }, [loadData])

  async function createDeployment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!canDeploy || !applicationId || !version.trim()) return

    setSaving(true)
    setError('')
    setMessage('')

    try {
      const response = await fetch('/api/deployments/', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          applicationId,
          environment,
          version: version.trim(),
        }),
      })

      if (!response.ok) {
        const detail = await response.json().catch(() => null)
        throw new Error(
          detail?.error ?? `Deployment request failed (${response.status}).`
        )
      }

      setVersion('')
      await loadData()
      setMessage('Deployment record created successfully.')
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Deployment creation failed.'
      )
    } finally {
      setSaving(false)
    }
  }

  function applicationName(id: string) {
    return applications.find(app => app.id === id)?.name ?? id
  }

  return (
    <section className="space-y-8">
      <div>
        <h2 className="text-3xl font-bold">Deployment Management</h2>
        <p className="mt-2 text-slate-400">
          Register deployment requests and review deployment history.
        </p>
      </div>

      {canDeploy && (
        <form
          onSubmit={event => void createDeployment(event)}
          className="space-y-5 rounded-xl border border-slate-700 bg-slate-900 p-6"
        >
          <h3 className="text-xl font-semibold">Create Deployment</h3>

          <label className="block space-y-2">
            <span>Application</span>
            <select
              required
              value={applicationId}
              onChange={event => setApplicationId(event.target.value)}
              className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3"
            >
              <option value="">Select application</option>
              {applications.map(app => (
                <option key={app.id} value={app.id}>
                  {app.name}
                </option>
              ))}
            </select>
          </label>

          <label className="block space-y-2">
            <span>Environment</span>
            <select
              value={environment}
              onChange={event => setEnvironment(event.target.value)}
              className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3"
            >
              <option value="development">Development</option>
              <option value="staging">Staging</option>
              <option value="production">Production</option>
            </select>
          </label>

          <label className="block space-y-2">
            <span>Version</span>
            <input
              required
              maxLength={100}
              value={version}
              onChange={event => setVersion(event.target.value)}
              placeholder="v1.0.0"
              className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3"
            />
          </label>

          <button
            type="submit"
            disabled={saving || loading || applications.length === 0}
            className="rounded-lg bg-sky-500 px-6 py-3 font-semibold text-slate-950 disabled:opacity-50"
          >
            {saving ? 'Creating...' : 'Create Deployment Record'}
          </button>
        </form>
      )}

      {error && <p role="alert" className="text-rose-400">{error}</p>}
      {message && <p role="status" className="text-emerald-400">{message}</p>}

      <div className="space-y-4">
        <h3 className="text-xl font-semibold">Deployment History</h3>

        {loading && <p>Loading deployments...</p>}

        {!loading && deployments.length === 0 && (
          <p className="text-slate-400">No deployment records yet.</p>
        )}

        {deployments.map(deployment => (
          <article
            key={deployment.id}
            className="rounded-xl border border-slate-700 bg-slate-900 p-5"
          >
            <div className="flex flex-wrap justify-between gap-3">
              <h4 className="font-semibold">
                {applicationName(deployment.applicationId)}
              </h4>
              <span className="rounded-full bg-slate-800 px-3 py-1 text-sm">
                {deployment.status}
              </span>
            </div>

            <p className="mt-3 text-sm text-slate-300">
              Version: {deployment.version}
            </p>
            <p className="mt-1 text-sm text-slate-300">
              Environment: {deployment.environment}
            </p>
            <p className="mt-1 text-sm text-slate-400">
              Created: {new Date(deployment.createdAt).toLocaleString()}
            </p>
          </article>
        ))}
      </div>
    </section>
  )
}
