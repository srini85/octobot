import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { Puzzle } from 'lucide-react'
import { pluginsApi } from '../services/api'

export default function Plugins() {
  const { id: botId } = useParams()
  const queryClient = useQueryClient()

  const { data: plugins, isLoading } = useQuery({
    queryKey: ['botPlugins', botId],
    queryFn: () => pluginsApi.getBotPlugins(botId!),
    enabled: !!botId,
  })

  const toggleMutation = useMutation({
    mutationFn: ({ pluginId, isEnabled }: { pluginId: string; isEnabled: boolean }) =>
      pluginsApi.togglePlugin(botId!, pluginId, isEnabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['botPlugins', botId] })
    },
  })

  const handleToggle = (pluginId: string, currentEnabled: boolean) => {
    toggleMutation.mutate({ pluginId, isEnabled: !currentEnabled })
  }

  if (isLoading) {
    return (
      <div className="p-8">
        <div className="animate-pulse">Loading...</div>
      </div>
    )
  }

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-2">Plugin Management</h1>
      <p className="text-gray-600 mb-8">Enable or disable plugins for this bot. Changes take effect on next channel restart.</p>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {plugins?.map((plugin) => (
          <div key={plugin.id} className={`bg-white rounded-lg shadow p-6 border-2 ${plugin.isEnabled ? 'border-green-500' : 'border-transparent'}`}>
            <div className="flex items-start justify-between mb-4">
              <div className="flex items-center gap-3">
                <Puzzle size={24} className={plugin.isEnabled ? 'text-green-600' : 'text-gray-400'} />
                <div>
                  <h3 className="font-semibold">{plugin.name}</h3>
                  <p className="text-sm text-gray-500">v{plugin.version}</p>
                </div>
              </div>
              <button
                onClick={() => handleToggle(plugin.id, plugin.isEnabled)}
                disabled={toggleMutation.isPending}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  plugin.isEnabled ? 'bg-green-600' : 'bg-gray-200'
                } ${toggleMutation.isPending ? 'opacity-50' : ''}`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    plugin.isEnabled ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>

            <p className="text-sm text-gray-600 mb-4">{plugin.description}</p>

            <div className="flex items-center justify-between">
              <span className={`text-xs font-medium px-2 py-1 rounded ${
                plugin.isEnabled
                  ? 'bg-green-100 text-green-800'
                  : 'bg-gray-100 text-gray-600'
              }`}>
                {plugin.isEnabled ? 'Enabled' : 'Disabled'}
              </span>
            </div>
          </div>
        ))}
      </div>

      {plugins?.length === 0 && (
        <div className="text-center text-gray-500 py-12">
          No plugins available
        </div>
      )}
    </div>
  )
}
