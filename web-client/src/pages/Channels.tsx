import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { Radio } from 'lucide-react'
import { channelsApi } from '../services/api'

export default function Channels() {
  const { id: botId } = useParams()
  const queryClient = useQueryClient()
  const [channelSettings, setChannelSettings] = useState<Record<string, Record<string, string>>>({})
  const [saveStatus, setSaveStatus] = useState<Record<string, 'idle' | 'saving' | 'saved' | 'error'>>({})

  const { data: channels, isLoading } = useQuery({
    queryKey: ['channels'],
    queryFn: channelsApi.getAll,
  })

  const { data: savedConfigs } = useQuery({
    queryKey: ['channelConfigs', botId],
    queryFn: () => channelsApi.getConfigs(botId!),
    enabled: !!botId,
  })

  const { data: channelStatuses } = useQuery({
    queryKey: ['channelStatus', botId],
    queryFn: () => channelsApi.getStatus(botId!),
    enabled: !!botId,
    refetchInterval: 5000, // Poll every 5 seconds
  })

  // Pre-populate settings from saved configs
  useEffect(() => {
    if (savedConfigs && savedConfigs.length > 0) {
      const settingsMap: Record<string, Record<string, string>> = {}
      for (const config of savedConfigs) {
        settingsMap[config.channelType] = config.settings
      }
      setChannelSettings(prev => ({ ...prev, ...settingsMap }))
    }
  }, [savedConfigs])

  const saveMutation = useMutation({
    mutationFn: channelsApi.saveConfig,
    onSuccess: (_, variables) => {
      setSaveStatus(prev => ({ ...prev, [variables.channelType]: 'saved' }))
      setTimeout(() => {
        setSaveStatus(prev => ({ ...prev, [variables.channelType]: 'idle' }))
      }, 2000)
    },
    onError: (_, variables) => {
      setSaveStatus(prev => ({ ...prev, [variables.channelType]: 'error' }))
    },
  })

  const startMutation = useMutation({
    mutationFn: ({ channelType }: { channelType: string }) =>
      channelsApi.start(botId!, channelType),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['channelStatus', botId] })
    },
  })

  const stopMutation = useMutation({
    mutationFn: ({ channelType }: { channelType: string }) =>
      channelsApi.stop(botId!, channelType),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['channelStatus', botId] })
    },
  })

  const handleSettingChange = (channelType: string, key: string, value: string) => {
    setChannelSettings(prev => ({
      ...prev,
      [channelType]: {
        ...prev[channelType],
        [key]: value,
      },
    }))
  }

  const handleSaveAndStart = async (channelType: string) => {
    if (!botId) return

    setSaveStatus(prev => ({ ...prev, [channelType]: 'saving' }))

    try {
      await saveMutation.mutateAsync({
        botInstanceId: botId,
        channelType,
        isEnabled: true,
        settings: channelSettings[channelType] || {},
      })

      // Start the channel after saving
      await startMutation.mutateAsync({ channelType })
      setSaveStatus(prev => ({ ...prev, [channelType]: 'saved' }))
      setTimeout(() => {
        setSaveStatus(prev => ({ ...prev, [channelType]: 'idle' }))
      }, 2000)
    } catch {
      setSaveStatus(prev => ({ ...prev, [channelType]: 'error' }))
    }
  }

  const handleStop = (channelType: string) => {
    stopMutation.mutate({ channelType })
  }

  const getChannelStatus = (channelType: string) => {
    return channelStatuses?.find(s => s.channelType === channelType)
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
      <h1 className="text-3xl font-bold mb-8">Channel Management</h1>

      <div className="grid gap-6">
        {channels?.map((channel) => {
          const status = getChannelStatus(channel.channelType)
          const isRunning = status?.isRunning ?? false

          return (
            <div key={channel.channelType} className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-3">
                  <Radio size={24} className="text-blue-600" />
                  <h3 className="text-xl font-semibold capitalize">{channel.channelType}</h3>
                </div>
                <div className="flex items-center gap-2">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                    isRunning
                      ? 'bg-green-100 text-green-800'
                      : 'bg-gray-100 text-gray-800'
                  }`}>
                    {isRunning ? (
                      <>
                        <span className="w-2 h-2 bg-green-500 rounded-full mr-1.5 animate-pulse"></span>
                        Running
                      </>
                    ) : (
                      'Stopped'
                    )}
                  </span>
                </div>
              </div>

              <div className="space-y-4">
                {channel.settings.map((setting) => (
                  <div key={setting.key}>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {setting.displayName}
                      {setting.isRequired && <span className="text-red-500">*</span>}
                    </label>
                    <p className="text-xs text-gray-500 mb-2">{setting.description}</p>
                    <input
                      type={setting.type === 'Secret' ? 'password' : 'text'}
                      placeholder={setting.defaultValue || ''}
                      value={channelSettings[channel.channelType]?.[setting.key] || ''}
                      onChange={(e) => handleSettingChange(channel.channelType, setting.key, e.target.value)}
                      disabled={isRunning}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
                    />
                  </div>
                ))}
              </div>

              <div className="mt-6 flex gap-3 items-center">
                {!isRunning ? (
                  <button
                    onClick={() => handleSaveAndStart(channel.channelType)}
                    disabled={saveStatus[channel.channelType] === 'saving' || startMutation.isPending}
                    className="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 disabled:opacity-50"
                  >
                    {saveStatus[channel.channelType] === 'saving' || startMutation.isPending
                      ? 'Starting...'
                      : 'Save & Start'}
                  </button>
                ) : (
                  <button
                    onClick={() => handleStop(channel.channelType)}
                    disabled={stopMutation.isPending}
                    className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 disabled:opacity-50"
                  >
                    {stopMutation.isPending ? 'Stopping...' : 'Stop'}
                  </button>
                )}
                {saveStatus[channel.channelType] === 'saved' && (
                  <span className="text-green-600 text-sm">Started successfully!</span>
                )}
                {saveStatus[channel.channelType] === 'error' && (
                  <span className="text-red-600 text-sm">Failed to start</span>
                )}
                {status?.startedAt && isRunning && (
                  <span className="text-gray-500 text-sm">
                    Started: {new Date(status.startedAt).toLocaleString()}
                  </span>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
