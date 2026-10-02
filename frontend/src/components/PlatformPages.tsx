import { useState } from 'react'

type PageProps = {
  role: string
}

const panel =
  'rounded-xl border border-slate-700 bg-slate-900 p-6'

const heading =
  'text-2xl font-semibold text-white'

const muted =
  'mt-2 text-sm leading-6 text-slate-400'

function PageHeader({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <header>
      <h2 className="text-3xl font-bold text-white">{title}</h2>
      <p className="mt-2 text-slate-400">{description}</p>
    </header>
  )
}

function StatusBadge({ text }: { text: string }) {
  return (
    <span className="rounded-full border border-sky-500/30 bg-sky-500/10 px-3 py-1 text-xs font-medium text-sky-300">
      {text}
    </span>
  )
}

export function EnvironmentsPage() {
  const environments = [
    {
      name: 'Development',
      code: 'development',
      description: 'Build and validate application changes.',
    },
    {
      name: 'Staging',
      code: 'staging',
      description: 'Test releases before production deployment.',
    },
    {
      name: 'Production',
      code: 'production',
      description: 'Manage production release configuration.',
    },
  ]

  return (
    <div className="space-y-8">
      <PageHeader
        title="Environment Management"
        description="Manage deployment targets and environment configuration."
      />

      <div className="grid gap-5 lg:grid-cols-3">
        {environments.map(environment => (
          <article key={environment.code} className={panel}>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h3 className={heading}>{environment.name}</h3>
              <StatusBadge text="Configuration preview" />
            </div>

            <p className={muted}>{environment.description}</p>

            <div className="mt-6 border-t border-slate-700 pt-5">
              <p className="text-xs uppercase tracking-wider text-slate-500">
                Environment identifier
              </p>
              <p className="mt-2 font-mono text-sm text-sky-300">
                {environment.code}
              </p>
            </div>
          </article>
        ))}
      </div>

      <section className={panel}>
        <h3 className={heading}>Environment Configuration</h3>
        <p className={muted}>
          Future integration will manage environment variables,
          secrets, approval policies and deployment restrictions.
          No live infrastructure is connected to this page yet.
        </p>
      </section>
    </div>
  )
}

export function InfrastructurePage() {
  const services = [
    {
      title: 'Docker',
      description: 'Container images and runtime management.',
      detail: 'Container inventory',
    },
    {
      title: 'Kubernetes',
      description: 'Clusters, namespaces, workloads and services.',
      detail: 'Cluster management',
    },
    {
      title: 'Terraform',
      description: 'Infrastructure as Code and provisioning.',
      detail: 'Infrastructure plans',
    },
    {
      title: 'Cloud Resources',
      description: 'Azure and AWS resource visibility.',
      detail: 'Cloud inventory',
    },
  ]

  return (
    <div className="space-y-8">
      <PageHeader
        title="Infrastructure"
        description="A central workspace for cloud and container infrastructure."
      />

      <div className="grid gap-5 md:grid-cols-2">
        {services.map(service => (
          <article key={service.title} className={panel}>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h3 className={heading}>{service.title}</h3>
              <StatusBadge text="Planned" />
            </div>

            <p className={muted}>{service.description}</p>

            <div className="mt-6 rounded-lg border border-slate-700 bg-slate-950 p-4">
              <p className="text-xs uppercase tracking-wider text-slate-500">
                Module
              </p>
              <p className="mt-2 text-sm text-slate-200">
                {service.detail}
              </p>
            </div>
          </article>
        ))}
      </div>

      <section className={panel}>
        <h3 className={heading}>Infrastructure Overview</h3>
        <p className={muted}>
          Live resource counts, cluster health and infrastructure
          operations will appear after provider integrations are connected.
        </p>
      </section>
    </div>
  )
}

export function SecurityPage({ role }: PageProps) {
  const checks = [
    {
      name: 'Container Vulnerability Scanning',
      tool: 'Trivy',
      description: 'Inspect container images for known vulnerabilities.',
    },
    {
      name: 'Dependency Security',
      tool: 'Dependency scanning',
      description: 'Review application dependencies and security findings.',
    },
    {
      name: 'Role-Based Access Control',
      tool: 'RBAC',
      description: 'Control access to platform operations.',
    },
    {
      name: 'Secrets Management',
      tool: 'Cloud secret stores',
      description: 'Manage sensitive deployment configuration securely.',
    },
  ]

  return (
    <div className="space-y-8">
      <PageHeader
        title="Security Center"
        description="Security controls, access management and scanning."
      />

      <section className={panel}>
        <p className="text-sm text-slate-400">Current session role</p>
        <p className="mt-2 text-2xl font-semibold text-sky-300">
          {role}
        </p>
        <p className={muted}>
          This role comes from your authenticated frontend session.
          Security checks are enforced by backend authorization policies.
        </p>
      </section>

      <div className="grid gap-5 md:grid-cols-2">
        {checks.map(check => (
          <article key={check.name} className={panel}>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h3 className="text-lg font-semibold">{check.name}</h3>
              <StatusBadge text="Integration view" />
            </div>

            <p className="mt-3 text-sm text-sky-300">{check.tool}</p>
            <p className={muted}>{check.description}</p>
          </article>
        ))}
      </div>

      <p className="text-sm text-slate-500">
        Live vulnerability findings are not available in this interface yet.
      </p>
    </div>
  )
}

export function AuditLogsPage() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('all')

  const categories = [
    'all',
    'authentication',
    'deployments',
    'applications',
    'administration',
  ]

  return (
    <div className="space-y-8">
      <PageHeader
        title="Audit Logs"
        description="Search and investigate platform activity."
      />

      <section className={panel}>
        <div className="grid gap-4 md:grid-cols-3">
          <label className="block space-y-2 md:col-span-2">
            <span className="text-sm text-slate-300">
              Search activity
            </span>
            <input
              type="search"
              value={search}
              onChange={event => setSearch(event.target.value)}
              placeholder="Search users, actions or resources"
              className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3 text-white"
            />
          </label>

          <label className="block space-y-2">
            <span className="text-sm text-slate-300">Category</span>
            <select
              value={category}
              onChange={event => setCategory(event.target.value)}
              className="w-full rounded-lg border border-slate-600 bg-slate-800 p-3 text-white"
            >
              {categories.map(item => (
                <option key={item} value={item}>
                  {item === 'all'
                    ? 'All categories'
                    : item.charAt(0).toUpperCase() + item.slice(1)}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="mt-6 overflow-x-auto rounded-lg border border-slate-700">
          <table className="w-full min-w-[600px] text-left text-sm">
            <thead className="bg-slate-800 text-slate-300">
              <tr>
                <th className="p-4">Timestamp</th>
                <th className="p-4">User</th>
                <th className="p-4">Action</th>
                <th className="p-4">Resource</th>
                <th className="p-4">Result</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td
                  colSpan={5}
                  className="p-10 text-center text-slate-400"
                >
                  Audit log backend integration is pending.
                  No live events are available.
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <p className={muted}>
          Search and category controls are ready for integration
          with the future audit events API.
        </p>
      </section>
    </div>
  )
}
