import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Settings } from 'lucide-react'
import { llmApi } from '../services/api'

export default function LLMSettings() {
  const queryClient = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [name, setName] = useState('')
  const [providerType, setProviderType] = useState('')
  const [modelId, setModelId] = useState('')
  const [apiKey, setApiKey] = useState('')
  const [endpoint, setEndpoint] = useState('')

  const { data: providers } = useQuery({
    queryKey: ['llmProviders'],
    queryFn: llmApi.getProviders,
  })

  const { data: configs, isLoading } = useQuery({
    queryKey: ['llmConfigs'],
    queryFn: llmApi.getConfigs,
  })

  const createMutation = useMutation({
    mutationFn: llmApi.createConfig,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['llmConfigs'] })
      setShowForm(false)
      resetForm()
    },
  })

  const resetForm = () => {
    setName('')
    setProviderType('')
    setModelId('')
    setApiKey('')
    setEndpoint('')
  }

  const selectedProvider = providers?.find((p) => p.name === providerType)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    createMutation.mutate({
      name,
      providerType,
      modelId: modelId || undefined,
      apiKey: apiKey || undefined,
      endpoint: endpoint || undefined,
    })
  }

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold">LLM Settings</h1>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
        >
          <Plus size={20} />
          Add Configuration
        </button>
      </div>

      {showForm && (
        <div className="bg-white rounded-lg shadow p-6 mb-8">
          <h2 className="text-xl font-semibold mb-4">New LLM Configuration</h2>
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
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  placeholder="My OpenAI Config"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Provider *
                </label>
                <select
                  value={providerType}
                  onChange={(e) => {
                    setProviderType(e.target.value)
                    setModelId('')
                  }}
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                >
                  <option value="">Select Provider</option>
                  {providers?.map((p) => (
                    <option key={p.name} value={p.name}>
                      {p.displayName}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {selectedProvider && (
              <>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Model
                  </label>
                  <select
                    value={modelId}
                    onChange={(e) => setModelId(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  >
                    <option value="">Select Model</option>
                    {selectedProvider.supportedModels.map((m) => (
                      <option key={m} value={m}>
                        {m}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    API Key
                  </label>
                  <input
                    type="password"
                    value={apiKey}
                    onChange={(e) => setApiKey(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                    placeholder="sk-..."
                  />
                </div>

                {providerType === 'ollama' && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Endpoint
                    </label>
                    <input
                      type="text"
                      value={endpoint}
                      onChange={(e) => setEndpoint(e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg"
                      placeholder="http://localhost:11434"
                    />
                  </div>
                )}
              </>
            )}

            <div className="flex gap-3">
              <button
                type="submit"
                disabled={createMutation.isPending}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                Save
              </button>
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="bg-gray-200 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-300"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="grid gap-6">
        <h2 className="text-xl font-semibold">Available Providers</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {providers?.map((provider) => (
            <div key={provider.name} className="bg-white rounded-lg shadow p-4">
              <h3 className="font-semibold mb-2">{provider.displayName}</h3>
              <div className="flex gap-2 text-xs">
                {provider.supportsStreaming && (
                  <span className="bg-green-100 text-green-700 px-2 py-1 rounded">
                    Streaming
                  </span>
                )}
                {provider.supportsFunctionCalling && (
                  <span className="bg-blue-100 text-blue-700 px-2 py-1 rounded">
                    Functions
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>

        <h2 className="text-xl font-semibold mt-4">Saved Configurations</h2>
        {isLoading ? (
          <div className="animate-pulse">Loading...</div>
        ) : configs?.length === 0 ? (
          <p className="text-gray-500">No configurations yet</p>
        ) : (
          <div className="grid gap-4">
            {configs?.map((config) => (
              <div key={config.id} className="bg-white rounded-lg shadow p-4 flex justify-between items-center">
                <div className="flex items-center gap-3">
                  <Settings size={20} className="text-gray-400" />
                  <div>
                    <h3 className="font-semibold">{config.name}</h3>
                    <p className="text-sm text-gray-500">
                      {config.providerType} - {config.modelId}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
