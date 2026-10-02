import { useState, type FormEvent } from 'react'

export type AuthSession = {
  accessToken: string
  user: {
    id: string
    email: string
    role: string
  }
}

type Props = {
  onLogin: (session: AuthSession) => void
}

export default function LoginPage({ onLogin }: Props) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  function switchMode(nextMode: 'login' | 'register') {
    setMode(nextMode)
    setPassword('')
    setConfirmPassword('')
    setError('')
    setSuccess('')
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSuccess('')

    if (mode === 'register') {
      if (password.length < 12 || password.length > 128) {
        setError('Password must contain 12–128 characters.')
        return
      }

      if (password !== confirmPassword) {
        setError('Passwords do not match.')
        return
      }
    }

    setLoading(true)

    try {
      const response = await fetch(
        mode === 'login'
          ? '/api/auth/login'
          : '/api/auth/register',
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            email: email.trim().toLowerCase(),
            password,
          }),
        }
      )

      if (!response.ok) {
        if (mode === 'register' && response.status === 409) {
          throw new Error(
            'Registration could not be completed. Try signing in or use another email.'
          )
        }

        if (mode === 'login') {
          throw new Error('Invalid email or password.')
        }

        throw new Error(
          response.status === 400
            ? 'Enter a valid email and a password of 12–128 characters.'
            : 'Registration failed. Please try again.'
        )
      }

      if (mode === 'login') {
        const data: AuthSession = await response.json()
        onLogin(data)
      } else {
        setMode('login')
        setPassword('')
        setConfirmPassword('')
        setSuccess('Account created! You can now sign in.')
      }
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Something went wrong. Please try again.'
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-950 p-6 text-white">
      <div className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold">
            <span className="text-sky-400">Cloud</span>Forge
          </h1>

          <p className="mt-2 text-slate-400">
            Internal Developer Platform
          </p>
        </div>

        <div className="mb-8 grid grid-cols-2 gap-2 rounded-xl bg-slate-800 p-1">
          <button
            type="button"
            disabled={loading}
            onClick={() => switchMode('login')}
            className={`rounded-lg px-4 py-3 font-medium ${
              mode === 'login'
                ? 'bg-sky-500 text-slate-950'
                : 'text-slate-300'
            }`}
          >
            Sign In
          </button>

          <button
            type="button"
            disabled={loading}
            onClick={() => switchMode('register')}
            className={`rounded-lg px-4 py-3 font-medium ${
              mode === 'register'
                ? 'bg-sky-500 text-slate-950'
                : 'text-slate-300'
            }`}
          >
            Create Account
          </button>
        </div>

        <h2 className="mb-6 text-xl font-semibold">
          {mode === 'login'
            ? 'Welcome back'
            : 'Create your account'}
        </h2>

        <form onSubmit={submit} className="space-y-5">
          <label className="block space-y-2">
            <span>Email address</span>
            <input
              type="email"
              autoComplete="username"
              required
              maxLength={254}
              value={email}
              onChange={event => setEmail(event.target.value)}
              placeholder="you@example.com"
              className="w-full rounded-lg border border-slate-700 bg-slate-800 p-3"
            />
          </label>

          <label className="block space-y-2">
            <span>Password</span>
            <input
              type="password"
              autoComplete={
                mode === 'login'
                  ? 'current-password'
                  : 'new-password'
              }
              required
              minLength={mode === 'register' ? 12 : undefined}
              maxLength={128}
              value={password}
              onChange={event => setPassword(event.target.value)}
              placeholder="Enter your password"
              className="w-full rounded-lg border border-slate-700 bg-slate-800 p-3"
            />
          </label>

          {mode === 'register' && (
            <label className="block space-y-2">
              <span>Confirm password</span>
              <input
                type="password"
                autoComplete="new-password"
                required
                minLength={12}
                maxLength={128}
                value={confirmPassword}
                onChange={event =>
                  setConfirmPassword(event.target.value)
                }
                placeholder="Confirm your password"
                className="w-full rounded-lg border border-slate-700 bg-slate-800 p-3"
              />
            </label>
          )}

          {mode === 'register' && (
            <p className="text-xs text-slate-400">
              Use a password of at least 12 characters.
              New accounts receive Viewer permissions.
            </p>
          )}

          {error && (
            <p role="alert" className="text-sm text-rose-400">
              {error}
            </p>
          )}

          {success && (
            <p role="status" className="text-sm text-emerald-400">
              {success}
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-lg bg-sky-500 p-3 font-semibold text-slate-950 disabled:opacity-50"
          >
            {loading
              ? 'Please wait...'
              : mode === 'login'
                ? 'Sign In'
                : 'Create Account'}
          </button>
        </form>
      </div>
    </main>
  )
}
