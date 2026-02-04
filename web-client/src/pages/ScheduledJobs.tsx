import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Clock, Pencil, Trash2, X, Play, Pause, History, CheckCircle, XCircle, Loader2 } from 'lucide-react'
import { scheduledJobsApi, botsApi } from '../services/api'
import type { ScheduledJob, JobExecution } from '../types'

const CRON_PRESETS = [
  { label: 'Every minute', value: '* * * * *' },
  { label: 'Every 5 minutes', value: '*/5 * * * *' },
  { label: 'Every 15 minutes', value: '*/15 * * * *' },
  { label: 'Every hour', value: '0 * * * *' },
  { label: 'Every 6 hours', value: '0 */6 * * *' },
  { label: 'Daily at midnight', value: '0 0 * * *' },
  { label: 'Daily at 9 AM', value: '0 9 * * *' },
  { label: 'Weekly (Sunday)', value: '0 0 * * 0' },
  { label: 'Monthly (1st)', value: '0 0 1 * *' },
]

export default function ScheduledJobs() {
  const queryClient = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [editingJob, setEditingJob] = useState<ScheduledJob | null>(null)
  const [showExecutions, setShowExecutions] = useState<string | null>(null)

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [instructions, setInstructions] = useState('')
  const [cronExpression, setCronExpression] = useState('')
  const [botInstanceId, setBotInstanceId] = useState('')

  const { data: jobs, isLoading } = useQuery({
    queryKey: ['scheduledJobs'],
    queryFn: scheduledJobsApi.getAll,
  })

  const { data: bots } = useQuery({
    queryKey: ['bots'],
    queryFn: botsApi.getAll,
  })

  const { data: executions } = useQuery({
    queryKey: ['jobExecutions', showExecutions],
    queryFn: () => scheduledJobsApi.getExecutions(showExecutions!),
    enabled: !!showExecutions,
  })

  const createMutation = useMutation({
    mutationFn: scheduledJobsApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] })
      resetForm()
      setShowForm(false)
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: Parameters<typeof scheduledJobsApi.update>[1] }) =>
      scheduledJobsApi.update(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] })
      resetForm()
      setEditingJob(null)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: scheduledJobsApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] })
    },
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      scheduledJobsApi.toggle(id, enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] })
    },
  })

  const runNowMutation = useMutation({
    mutationFn: scheduledJobsApi.runNow,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] })
      queryClient.invalidateQueries({ queryKey: ['jobExecutions'] })
    },
  })

  const resetForm = () => {
    setName('')
    setDescription('')
    setInstructions('')
    setCronExpression('')
    setBotInstanceId('')
  }

  const startEdit = (job: ScheduledJob) => {
    setEditingJob(job)
    setName(job.name)
    setDescription(job.description || '')
    setInstructions(job.instructions)
    setCronExpression(job.cronExpression)
    setBotInstanceId(job.botInstanceId)
    setShowForm(false)
  }

  const cancelEdit = () => {
    setEditingJob(null)
    resetForm()
  }

  const handleDelete = (job: ScheduledJob) => {
    if (confirm(`Are you sure you want to delete "${job.name}"?`)) {
      deleteMutation.mutate(job.id)
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (editingJob) {
      updateMutation.mutate({
        id: editingJob.id,
        dto: {
          name: name || undefined,
          description: description || undefined,
          instructions: instructions || undefined,
          cronExpression: cronExpression || undefined,
        },
      })
    } else {
      createMutation.mutate({
        name,
        description: description || undefined,
        instructions,
        cronExpression,
        botInstanceId,
      })
    }
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return 'Never'
    return new Date(dateStr).toLocaleString()
  }

  const getStatusBadge = (status?: string) => {
    switch (status) {
      case 'Success':
        return <span className="flex items-center gap-1 text-green-600"><CheckCircle size={14} /> Success</span>
      case 'Failed':
        return <span className="flex items-center gap-1 text-red-600"><XCircle size={14} /> Failed</span>
      case 'Running':
        return <span className="flex items-center gap-1 text-blue-600"><Loader2 size={14} className="animate-spin" /> Running</span>
      default:
        return <span className="text-gray-500">-</span>
    }
  }

  const isFormVisible = showForm || editingJob

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold">Scheduled Jobs</h1>
        {!editingJob && (
          <button
            onClick={() => setShowForm(!showForm)}
            className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            <Plus size={20} />
            New Job
          </button>
        )}
      </div>

      {isFormVisible && (
        <div className="bg-white rounded-lg shadow p-6 mb-8">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-xl font-semibold">
              {editingJob ? `Edit "${editingJob.name}"` : 'New Scheduled Job'}
            </h2>
            <button
              onClick={() => editingJob ? cancelEdit() : setShowForm(false)}
              className="text-gray-500 hover:text-gray-700"
            >
              <X size={20} />
            </button>
          </div>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Name *
                </label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required={!editingJob}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  placeholder="Daily Summary"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Bot {!editingJob && '*'}
                </label>
                {editingJob ? (
                  <input
                    type="text"
                    value={editingJob.botName || 'Unknown Bot'}
                    disabled
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-gray-100 text-gray-500"
                  />
                ) : (
                  <select
                    value={botInstanceId}
                    onChange={(e) => setBotInstanceId(e.target.value)}
                    required
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  >
                    <option value="">Select Bot</option>
                    {bots?.map((bot) => (
                      <option key={bot.id} value={bot.id}>
                        {bot.name}
                      </option>
                    ))}
                  </select>
                )}
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <input
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                placeholder="Optional description of this job"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Instructions *
              </label>
              <textarea
                value={instructions}
                onChange={(e) => setInstructions(e.target.value)}
                required={!editingJob}
                rows={4}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                placeholder="The prompt/instructions to execute on schedule..."
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Schedule (Cron Expression) *
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={cronExpression}
                  onChange={(e) => setCronExpression(e.target.value)}
                  required={!editingJob}
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-lg font-mono"
                  placeholder="0 9 * * *"
                />
                <select
                  onChange={(e) => e.target.value && setCronExpression(e.target.value)}
                  className="px-3 py-2 border border-gray-300 rounded-lg"
                  value=""
                >
                  <option value="">Presets...</option>
                  {CRON_PRESETS.map((preset) => (
                    <option key={preset.value} value={preset.value}>
                      {preset.label}
                    </option>
                  ))}
                </select>
              </div>
              <p className="text-xs text-gray-500 mt-1">
                Format: minute hour day month weekday (e.g., "0 9 * * *" = daily at 9 AM)
              </p>
            </div>

            <div className="flex gap-3">
              <button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                {editingJob ? 'Update' : 'Create'}
              </button>
              <button
                type="button"
                onClick={() => editingJob ? cancelEdit() : setShowForm(false)}
                className="bg-gray-200 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-300"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Jobs List */}
      <div className="space-y-4">
        {isLoading ? (
          <div className="animate-pulse">Loading...</div>
        ) : jobs?.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-lg shadow">
            <Clock size={48} className="mx-auto text-gray-400 mb-4" />
            <p className="text-gray-500">No scheduled jobs yet</p>
            <p className="text-sm text-gray-400 mt-1">Create your first job to automate tasks</p>
          </div>
        ) : (
          jobs?.map((job) => (
            <div key={job.id} className="bg-white rounded-lg shadow p-4">
              <div className="flex justify-between items-start">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <h3 className="font-semibold text-lg">{job.name}</h3>
                    <span className={`px-2 py-0.5 text-xs rounded-full ${
                      job.isEnabled ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'
                    }`}>
                      {job.isEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                  </div>
                  {job.description && (
                    <p className="text-gray-600 text-sm mb-2">{job.description}</p>
                  )}
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                    <div>
                      <span className="text-gray-500">Bot:</span>
                      <span className="ml-2 font-medium">{job.botName || 'Unknown'}</span>
                    </div>
                    <div>
                      <span className="text-gray-500">Schedule:</span>
                      <span className="ml-2 font-mono text-xs bg-gray-100 px-2 py-0.5 rounded">{job.cronExpression}</span>
                    </div>
                    <div>
                      <span className="text-gray-500">Last Run:</span>
                      <span className="ml-2">{formatDate(job.lastRunAt)}</span>
                    </div>
                    <div>
                      <span className="text-gray-500">Next Run:</span>
                      <span className="ml-2">{job.isEnabled ? formatDate(job.nextRunAt) : '-'}</span>
                    </div>
                  </div>
                  <div className="mt-2">
                    <span className="text-gray-500 text-sm">Last Status:</span>
                    <span className="ml-2 text-sm">{getStatusBadge(job.lastRunStatus)}</span>
                  </div>
                </div>
                <div className="flex items-center gap-2 ml-4">
                  <button
                    onClick={() => runNowMutation.mutate(job.id)}
                    disabled={runNowMutation.isPending}
                    className="p-2 text-gray-500 hover:text-green-600 hover:bg-green-50 rounded-lg transition-colors"
                    title="Run now"
                  >
                    <Play size={18} />
                  </button>
                  <button
                    onClick={() => toggleMutation.mutate({ id: job.id, enabled: !job.isEnabled })}
                    disabled={toggleMutation.isPending}
                    className="p-2 text-gray-500 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                    title={job.isEnabled ? 'Disable' : 'Enable'}
                  >
                    {job.isEnabled ? <Pause size={18} /> : <Play size={18} />}
                  </button>
                  <button
                    onClick={() => setShowExecutions(showExecutions === job.id ? null : job.id)}
                    className="p-2 text-gray-500 hover:text-purple-600 hover:bg-purple-50 rounded-lg transition-colors"
                    title="View history"
                  >
                    <History size={18} />
                  </button>
                  <button
                    onClick={() => startEdit(job)}
                    className="p-2 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                    title="Edit"
                  >
                    <Pencil size={18} />
                  </button>
                  <button
                    onClick={() => handleDelete(job)}
                    disabled={deleteMutation.isPending}
                    className="p-2 text-gray-500 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors disabled:opacity-50"
                    title="Delete"
                  >
                    <Trash2 size={18} />
                  </button>
                </div>
              </div>

              {/* Execution History */}
              {showExecutions === job.id && (
                <div className="mt-4 pt-4 border-t">
                  <h4 className="font-medium mb-2">Execution History</h4>
                  {executions?.length === 0 ? (
                    <p className="text-gray-500 text-sm">No executions yet</p>
                  ) : (
                    <div className="space-y-2 max-h-64 overflow-y-auto">
                      {executions?.map((exec: JobExecution) => (
                        <div key={exec.id} className="bg-gray-50 rounded p-3 text-sm">
                          <div className="flex justify-between items-start">
                            <div>
                              <span className="font-medium">{getStatusBadge(exec.status)}</span>
                              <span className="text-gray-500 ml-4">
                                {formatDate(exec.startedAt)}
                                {exec.completedAt && ` - ${formatDate(exec.completedAt)}`}
                              </span>
                            </div>
                          </div>
                          {exec.output && (
                            <pre className="mt-2 text-xs bg-white p-2 rounded border max-h-32 overflow-y-auto whitespace-pre-wrap">
                              {exec.output}
                            </pre>
                          )}
                          {exec.errorMessage && (
                            <p className="mt-2 text-red-600 text-xs">{exec.errorMessage}</p>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  )
}
