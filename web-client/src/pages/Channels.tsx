import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { Radio } from 'lucide-react'
import { channelsApi } from '../services/api'

export default function Channels() {
  const { id: _botId } = useParams() // eslint-disable-line @typescript-eslint/no-unused-vars

  const { data: channels, isLoading } = useQuery({
    queryKey: ['channels'],
    queryFn: channelsApi.getAll,
  })

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
        {channels?.map((channel) => (
          <div key={channel.channelType} className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center gap-3 mb-4">
              <Radio size={24} className="text-blue-600" />
              <h3 className="text-xl font-semibold capitalize">{channel.channelType}</h3>
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
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
              ))}
            </div>

            <div className="mt-6 flex gap-3">
              <button className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">
                Save & Start
              </button>
              <button className="bg-gray-200 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-300">
                Stop
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
