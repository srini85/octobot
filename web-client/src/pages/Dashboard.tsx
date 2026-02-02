import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Plus, Bot, Trash2 } from 'lucide-react'
import { botsApi } from '../services/api'
import { useAppStore } from '../stores/appStore'
import type { BotInstance } from '../types'

export default function Dashboard() {
  const queryClient = useQueryClient()
  const { setSelectedBot } = useAppStore()

  const { data: bots, isLoading } = useQuery({
    queryKey: ['bots'],
    queryFn: botsApi.getAll,
  })

  const deleteMutation = useMutation({
    mutationFn: botsApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bots'] })
    },
  })

  const handleSelectBot = (bot: BotInstance) => {
    setSelectedBot(bot)
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
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold">Bot Dashboard</h1>
        <Link
          to="/bots/new"
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
        >
          <Plus size={20} />
          Create Bot
        </Link>
      </div>

      {bots?.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg shadow">
          <Bot size={48} className="mx-auto text-gray-400 mb-4" />
          <h2 className="text-xl font-semibold mb-2">No bots yet</h2>
          <p className="text-gray-600 mb-4">Create your first bot to get started</p>
          <Link
            to="/bots/new"
            className="inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            <Plus size={20} />
            Create Bot
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {bots?.map((bot) => (
            <div
              key={bot.id}
              className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow"
            >
              <div className="flex justify-between items-start mb-4">
                <div className="flex items-center gap-3">
                  <Bot size={24} className="text-blue-600" />
                  <h3 className="text-lg font-semibold">{bot.name}</h3>
                </div>
                <span
                  className={`px-2 py-1 text-xs rounded-full ${
                    bot.isActive
                      ? 'bg-green-100 text-green-800'
                      : 'bg-gray-100 text-gray-800'
                  }`}
                >
                  {bot.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>

              {bot.description && (
                <p className="text-gray-600 text-sm mb-4 line-clamp-2">
                  {bot.description}
                </p>
              )}

              <div className="flex gap-2">
                <Link
                  to={`/bots/${bot.id}`}
                  onClick={() => handleSelectBot(bot)}
                  className="flex-1 text-center bg-gray-100 text-gray-700 px-3 py-2 rounded hover:bg-gray-200"
                >
                  Configure
                </Link>
                <button
                  onClick={() => deleteMutation.mutate(bot.id)}
                  className="p-2 text-red-600 hover:bg-red-50 rounded"
                  title="Delete"
                >
                  <Trash2 size={20} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
