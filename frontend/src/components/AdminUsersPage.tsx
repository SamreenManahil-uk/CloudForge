import { useCallback, useEffect, useState } from 'react'

type User = {
  id: string
  email: string
  role: string
  createdAt: string
}

type Props = {
  token: string
  currentUserId: string
}

const roles = ['Viewer', 'Developer', 'Admin']

export default function AdminUsersPage({
  token,
  currentUserId,
}: Props) {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [savingId, setSavingId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const loadUsers = useCallback(async (
    signal?: AbortSignal
  ) => {
    const response = await fetch('/api/admin/users', {
      signal,
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })

    if (!response.ok) {
      throw new Error(
        response.status === 403
          ? 'Admin access required.'
          : 'Unable to load users.'
      )
    }

    const data: User[] = await response.json()

    if (!signal?.aborted) {
      setUsers(data)
    }
  }, [token])

  useEffect(() => {
    const controller = new AbortController()

    async function load() {
      try {
        await loadUsers(controller.signal)
      } catch (err) {
        if (!controller.signal.aborted) {
          setError(
            err instanceof Error
              ? err.message
              : 'Unable to load users.'
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
  }, [loadUsers])

  async function updateRole(
    user: User,
    role: string
  ) {
    if (user.id === currentUserId) return
    if (user.role === role) return

    const confirmed = window.confirm(
      `Change ${user.email} from ${user.role} to ${role}?`
    )

    if (!confirmed) return

    setSavingId(user.id)
    setError('')
    setMessage('')

    try {
      const response = await fetch(
        `/api/admin/users/${user.id}/role`,
        {
          method: 'PATCH',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ role }),
        }
      )

      if (!response.ok) {
        throw new Error(
          response.status === 403
            ? 'Admin permission required.'
            : 'Unable to update user role.'
        )
      }

      await loadUsers()
      setMessage(
        `${user.email} role updated to ${role}.`
      )
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Role update failed.'
      )
    } finally {
      setSavingId(null)
    }
  }

  return (
    <section className="space-y-6 text-white">
      <div>
        <h1 className="text-3xl font-bold">
          User Management
        </h1>
        <p className="mt-2 text-slate-400">
          Manage registered users and access roles.
        </p>
      </div>

      <div className="rounded-xl border border-slate-700 bg-slate-900 p-6">
        <h2 className="text-lg font-semibold">
          Registered users
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Total: {users.length}
        </p>

        {loading && (
          <p className="mt-5">Loading users...</p>
        )}

        {error && (
          <p role="alert" className="mt-5 text-rose-400">
            {error}
          </p>
        )}

        {message && (
          <p role="status" className="mt-5 text-emerald-400">
            {message}
          </p>
        )}

        {!loading && (
          <div className="mt-6 space-y-4">
            {users.map(user => (
              <div
                key={user.id}
                className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-slate-700 bg-slate-800 p-4"
              >
                <div>
                  <p className="break-all font-medium">
                    {user.email}
                  </p>

                  <p className="mt-1 text-xs text-slate-400">
                    Current role: {user.role}
                  </p>

                  {user.id === currentUserId && (
                    <p className="mt-1 text-xs text-sky-400">
                      Your account
                    </p>
                  )}
                </div>

                <label className="space-y-2">
                  <span className="block text-xs text-slate-400">
                    Access role
                  </span>

                  <select
                    aria-label={`Role for ${user.email}`}
                    value={user.role}
                    disabled={
                      user.id === currentUserId ||
                      savingId !== null
                    }
                    onChange={event =>
                      void updateRole(
                        user,
                        event.target.value
                      )
                    }
                    className="rounded-lg border border-slate-600 bg-slate-900 px-4 py-2"
                  >
                    {roles.map(role => (
                      <option key={role} value={role}>
                        {role}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  )
}
