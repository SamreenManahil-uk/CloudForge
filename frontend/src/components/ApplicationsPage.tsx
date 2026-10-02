import { useCallback, useEffect, useState, type FormEvent } from 'react'

type Application = {
  id: string
  name: string
  repositoryUrl: string
  environment: string
  createdAt: string
}

export default function ApplicationsPage({ token, canDeploy }: { token: string; canDeploy: boolean }) {
  const [applications, setApplications] = useState<Application[]>([])
  const [name, setName] = useState('')
  const [repositoryUrl, setRepositoryUrl] = useState('')
  const [environment, setEnvironment] = useState('development')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadApplications = useCallback(async (signal?: AbortSignal) => {
    const response = await fetch('/api/applications/', {
      signal,
      headers: { Authorization: `Bearer ${token}` },
    })

    if (!response.ok) {
      throw new Error('Unable to load applications')
    }

    const data: Application[] = await response.json()
    setApplications(data)
  }, [token])

  useEffect(() => {
    const controller = new AbortController()

    async function load() {
      try {
        await loadApplications(controller.signal)
      } catch (err) {
        if (!controller.signal.aborted) {
          setError(
            err instanceof Error ? err.message : 'Request failed'
          )
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    void load()

    return () => controller.abort()
  }, [loadApplications])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setError('')

    try {
      const response = await fetch('/api/applications/', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          name,
          repositoryUrl,
          environment,
        }),
      })

      if (!response.ok) {
        throw new Error('Application registration failed')
      }

      setName('')
      setRepositoryUrl('')
      await loadApplications()
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Request failed'
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="space-y-8 text-white">
      <div>
        <h1 className="text-3xl font-bold">
          Application Management
        </h1>
        <p className="mt-2 text-slate-400">
          Register and manage your cloud applications.
        </p>
      </div>

      {canDeploy && <form
        onSubmit={handleSubmit}
        className="space-y-4 rounded-xl border border-slate-700 bg-slate-900 p-6"
      >
        <h2 className="text-xl font-semibold">
          Register Application
        </h2>

        <input
          className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3"
          placeholder="Application name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          required
        />

        <input
          className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3"
          placeholder="https://github.com/organisation/repository"
          type="url"
          pattern="https://.*"
          value={repositoryUrl}
          onChange={(event) => setRepositoryUrl(event.target.value)}
          required
        />

        <select
          className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3"
          value={environment}
          onChange={(event) => setEnvironment(event.target.value)}
        >
          <option value="development">Development</option>
          <option value="staging">Staging</option>
          <option value="production">Production</option>
        </select>

        <button
          disabled={saving}
          className="rounded-lg bg-blue-600 px-6 py-3 font-semibold disabled:opacity-50"
        >
          {saving ? 'Registering...' : 'Register Application'}
        </button>
      </form>}

      {error && (
        <p role="alert" className="text-red-400">
          {error}
        </p>
      )}

      <div className="space-y-4">
        <h2 className="text-xl font-semibold">
          Registered Applications
        </h2>

        {loading && <p>Loading applications...</p>}

        {!loading && applications.length === 0 && (
          <p className="text-slate-400">
            No applications registered yet.
          </p>
        )}

        {applications.map((application) => (
          <article
            key={application.id}
            className="rounded-xl border border-slate-700 bg-slate-900 p-5"
          >
            <h3 className="text-lg font-semibold">
              {application.name}
            </h3>

            <p className="mt-2 text-slate-400">
              Environment: {application.environment}
            </p>

            <a
              href={application.repositoryUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-2 block break-all text-blue-400"
            >
              {application.repositoryUrl}
            </a>
          </article>
        ))}
      </div>
    </section>
  )
}
