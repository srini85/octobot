import { useState, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { Puzzle, Settings, ChevronDown, ChevronUp, Save, Mail, Link2, Unlink } from 'lucide-react'
import { pluginsApi, office365Api } from '../services/api'
import type { PluginInfo } from '../types'

export default function Plugins() {
  const { id: botId } = useParams()
  const queryClient = useQueryClient()
  const [expandedPlugin, setExpandedPlugin] = useState<string | null>(null)
  const [pluginSettings, setPluginSettings] = useState<Record<string, Record<string, string>>>({})
  const [saveStatus, setSaveStatus] = useState<Record<string, 'idle' | 'saving' | 'saved' | 'error'>>({})

  // Fetch plugin definitions (with setting definitions)
  const { data: pluginDefs } = useQuery({
    queryKey: ['plugins'],
    queryFn: pluginsApi.getAll,
  })

  // Fetch bot-specific plugin status and saved settings
  const { data: botPlugins, isLoading } = useQuery({
    queryKey: ['botPlugins', botId],
    queryFn: () => pluginsApi.getBotPlugins(botId!),
    enabled: !!botId,
  })

  // Office 365 connection status
  const { data: office365Status, refetch: refetchO365Status } = useQuery({
    queryKey: ['office365Status', botId],
    queryFn: () => office365Api.getStatus(botId!),
    enabled: !!botId,
  })

  // Listen for popup callback
  const handleMessage = useCallback((event: MessageEvent) => {
    if (event.data?.type === 'office365-auth-complete') {
      refetchO365Status()
      queryClient.invalidateQueries({ queryKey: ['botPlugins', botId] })
    }
  }, [refetchO365Status, queryClient, botId])

  useEffect(() => {
    window.addEventListener('message', handleMessage)
    return () => window.removeEventListener('message', handleMessage)
  }, [handleMessage])

  // Pre-populate settings from saved configs
  useEffect(() => {
    if (botPlugins) {
      const settingsMap: Record<string, Record<string, string>> = {}
      for (const plugin of botPlugins) {
        if (plugin.settings) {
          settingsMap[plugin.id] = { ...plugin.settings }
        }
      }
      setPluginSettings(prev => ({ ...prev, ...settingsMap }))
    }
  }, [botPlugins])

  const toggleMutation = useMutation({
    mutationFn: ({ pluginId, isEnabled, settings }: { pluginId: string; isEnabled: boolean; settings?: Record<string, string> }) =>
      pluginsApi.togglePlugin(botId!, pluginId, isEnabled, settings),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['botPlugins', botId] })
    },
  })

  const disconnectMutation = useMutation({
    mutationFn: () => office365Api.disconnect(botId!),
    onSuccess: () => {
      refetchO365Status()
      queryClient.invalidateQueries({ queryKey: ['botPlugins', botId] })
    },
  })

  const handleToggle = (pluginId: string, currentEnabled: boolean) => {
    const settings = pluginSettings[pluginId]
    toggleMutation.mutate({ pluginId, isEnabled: !currentEnabled, settings })
  }

  const handleSettingChange = (pluginId: string, key: string, value: string) => {
    setPluginSettings(prev => ({
      ...prev,
      [pluginId]: {
        ...prev[pluginId],
        [key]: value,
      },
    }))
  }

  const handleSaveSettings = async (pluginId: string) => {
    if (!botId) return
    const plugin = botPlugins?.find(p => p.id === pluginId)
    setSaveStatus(prev => ({ ...prev, [pluginId]: 'saving' }))

    try {
      await toggleMutation.mutateAsync({
        pluginId,
        isEnabled: plugin?.isEnabled ?? false,
        settings: pluginSettings[pluginId] || {},
      })
      setSaveStatus(prev => ({ ...prev, [pluginId]: 'saved' }))
      setTimeout(() => {
        setSaveStatus(prev => ({ ...prev, [pluginId]: 'idle' }))
      }, 2000)
    } catch {
      setSaveStatus(prev => ({ ...prev, [pluginId]: 'error' }))
      setTimeout(() => {
        setSaveStatus(prev => ({ ...prev, [pluginId]: 'idle' }))
      }, 3000)
    }
  }

  const handleConnectOffice365 = async () => {
    if (!botId) return
    try {
      const { authUrl } = await office365Api.getAuthUrl(botId)
      // Open in popup
      const width = 600
      const height = 700
      const left = window.screenX + (window.outerWidth - width) / 2
      const top = window.screenY + (window.outerHeight - height) / 2
      window.open(authUrl, 'office365-auth', `width=${width},height=${height},left=${left},top=${top}`)
    } catch (err) {
      console.error('Failed to get auth URL:', err)
    }
  }

  const getPluginDef = (pluginId: string): PluginInfo | undefined => {
    return pluginDefs?.find(p => p.id === pluginId)
  }

  const hasSettings = (pluginId: string): boolean => {
    const def = getPluginDef(pluginId)
    return (def?.settings?.length ?? 0) > 0
  }

  // Filter out internal OAuth settings from display
  const getDisplaySettings = (pluginId: string) => {
    const def = getPluginDef(pluginId)
    return def?.settings?.filter(s =>
      !['AccessToken', 'RefreshToken', 'TokenExpiry', 'ConnectedEmail', 'ConnectedAt', 'LastCheckTime'].includes(s.key)
    )
  }

  const isOffice365Plugin = (pluginId: string) => pluginId === 'office365-email'

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
      <p className="text-gray-600 mb-8">Enable or disable plugins and configure their settings. Changes take effect on next channel restart.</p>

      <div className="space-y-4">
        {botPlugins?.map((plugin) => {
          const isExpanded = expandedPlugin === plugin.id
          const pluginHasSettings = hasSettings(plugin.id) || isOffice365Plugin(plugin.id)
          const displaySettings = getDisplaySettings(plugin.id)

          return (
            <div key={plugin.id} className={`bg-white rounded-lg shadow border-2 ${plugin.isEnabled ? 'border-green-500' : 'border-transparent'}`}>
              {/* Plugin header */}
              <div className="p-6">
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    {isOffice365Plugin(plugin.id)
                      ? <Mail size={24} className={plugin.isEnabled ? 'text-blue-600' : 'text-gray-400'} />
                      : <Puzzle size={24} className={plugin.isEnabled ? 'text-green-600' : 'text-gray-400'} />
                    }
                    <div>
                      <h3 className="font-semibold text-lg">{plugin.name}</h3>
                      <p className="text-sm text-gray-500">v{plugin.version}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className={`text-xs font-medium px-2 py-1 rounded ${
                      plugin.isEnabled
                        ? 'bg-green-100 text-green-800'
                        : 'bg-gray-100 text-gray-600'
                    }`}>
                      {plugin.isEnabled ? 'Enabled' : 'Disabled'}
                    </span>
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
                </div>

                <p className="text-sm text-gray-600 mt-3">{plugin.description}</p>

                {pluginHasSettings && (
                  <button
                    onClick={() => setExpandedPlugin(isExpanded ? null : plugin.id)}
                    className="mt-3 flex items-center gap-1 text-sm text-blue-600 hover:text-blue-800 transition-colors"
                  >
                    <Settings size={14} />
                    <span>{isExpanded ? 'Hide Settings' : 'Configure Settings'}</span>
                    {isExpanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                  </button>
                )}
              </div>

              {/* Settings panel */}
              {pluginHasSettings && isExpanded && (
                <div className="border-t border-gray-200 bg-gray-50 p-6">
                  {/* Office 365 connection section */}
                  {isOffice365Plugin(plugin.id) && (
                    <div className="mb-6">
                      <h4 className="text-sm font-semibold text-gray-700 mb-3">Office 365 Connection</h4>
                      {office365Status?.connected ? (
                        <div className="flex items-center justify-between bg-white rounded-lg border border-green-200 p-4">
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
                              <Mail size={20} className="text-green-600" />
                            </div>
                            <div>
                              <p className="text-sm font-medium text-gray-900">Connected to Office 365</p>
                              <p className="text-xs text-gray-500">{office365Status.email}</p>
                              {office365Status.connectedAt && (
                                <p className="text-xs text-gray-400">
                                  Connected {new Date(office365Status.connectedAt).toLocaleDateString()}
                                </p>
                              )}
                            </div>
                          </div>
                          <button
                            onClick={() => disconnectMutation.mutate()}
                            disabled={disconnectMutation.isPending}
                            className="flex items-center gap-1.5 text-sm text-red-600 hover:text-red-800 px-3 py-1.5 rounded-lg hover:bg-red-50 transition-colors"
                          >
                            <Unlink size={14} />
                            {disconnectMutation.isPending ? 'Disconnecting...' : 'Disconnect'}
                          </button>
                        </div>
                      ) : (
                        <div className="bg-white rounded-lg border border-gray-200 p-4">
                          <p className="text-sm text-gray-600 mb-3">
                            Connect your Office 365 account to monitor your inbox. Email notifications will be sent to your Telegram conversations with this bot.
                          </p>
                          <button
                            onClick={handleConnectOffice365}
                            className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-lg hover:bg-blue-700 transition-colors"
                          >
                            <Link2 size={16} />
                            Connect to Office 365
                          </button>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Regular settings */}
                  {displaySettings && displaySettings.length > 0 && (
                    <>
                      <h4 className="text-sm font-semibold text-gray-700 mb-4">
                        {isOffice365Plugin(plugin.id) ? 'Additional Settings' : 'Plugin Settings'}
                      </h4>
                      <div className="space-y-4">
                        {displaySettings.map((setting) => (
                          <div key={setting.key}>
                            <label className="block text-sm font-medium text-gray-700 mb-1">
                              {setting.displayName}
                              {setting.isRequired && <span className="text-red-500 ml-1">*</span>}
                            </label>
                            <p className="text-xs text-gray-500 mb-2">{setting.description}</p>
                            {setting.type === 'Boolean' ? (
                              <label className="flex items-center gap-2 cursor-pointer">
                                <input
                                  type="checkbox"
                                  checked={pluginSettings[plugin.id]?.[setting.key] === 'true'}
                                  onChange={(e) => handleSettingChange(plugin.id, setting.key, e.target.checked ? 'true' : 'false')}
                                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                />
                                <span className="text-sm text-gray-600">
                                  {pluginSettings[plugin.id]?.[setting.key] === 'true' ? 'Enabled' : 'Disabled'}
                                </span>
                              </label>
                            ) : (
                              <input
                                type={setting.type === 'Secret' ? 'password' : setting.type === 'Number' ? 'number' : 'text'}
                                placeholder={setting.defaultValue || ''}
                                value={pluginSettings[plugin.id]?.[setting.key] || ''}
                                onChange={(e) => handleSettingChange(plugin.id, setting.key, e.target.value)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                              />
                            )}
                          </div>
                        ))}
                      </div>

                      <div className="mt-6 flex items-center gap-3">
                        <button
                          onClick={() => handleSaveSettings(plugin.id)}
                          disabled={saveStatus[plugin.id] === 'saving'}
                          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
                        >
                          <Save size={16} />
                          {saveStatus[plugin.id] === 'saving' ? 'Saving...' : 'Save Settings'}
                        </button>
                        {saveStatus[plugin.id] === 'saved' && (
                          <span className="text-green-600 text-sm">Settings saved successfully!</span>
                        )}
                        {saveStatus[plugin.id] === 'error' && (
                          <span className="text-red-600 text-sm">Failed to save settings</span>
                        )}
                      </div>
                    </>
                  )}
                </div>
              )}
            </div>
          )
        })}
      </div>

      {botPlugins?.length === 0 && (
        <div className="text-center text-gray-500 py-12">
          No plugins available
        </div>
      )}
    </div>
  )
}
