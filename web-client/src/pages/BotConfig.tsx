import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Save } from 'lucide-react'
import { botsApi, llmApi } from '../services/api'
import { useAppStore } from '../stores/appStore'

export default function BotConfig() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { setSelectedBot } = useAppStore()
  const isNew = id === undefined || id === 'new'

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [systemPrompt, setSystemPrompt] = useState('')
  const [defaultLLMConfigId, setDefaultLLMConfigId] = useState('')

  const { data: bot } = useQuery({
    queryKey: ['bot', id],
    queryFn: () => botsApi.getById(id!),
    enabled: !isNew,
  })

  const { data: llmConfigs } = useQuery({
    queryKey: ['llmConfigs'],
    queryFn: llmApi.getConfigs,
  })

  useEffect(() => {
    if (bot) {
      setName(bot.name)
      setDescription(bot.description || '')
      setSystemPrompt(bot.systemPrompt || '')
      setDefaultLLMConfigId(bot.defaultLLMConfigId || '')
      setSelectedBot(bot)
    }
  }, [bot, setSelectedBot])

  const createMutation = useMutation({
    mutationFn: botsApi.create,
    onSuccess: (newBot) => {
      queryClient.invalidateQueries({ queryKey: ['bots'] })
      setSelectedBot(newBot)
      navigate(`/bots/${newBot.id}`)
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: Parameters<typeof botsApi.update>[1] }) =>
      botsApi.update(id, dto),
    onSuccess: (updatedBot) => {
      queryClient.invalidateQueries({ queryKey: ['bots'] })
      queryClient.invalidateQueries({ queryKey: ['bot', id] })
      setSelectedBot(updatedBot)
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const dto = {
      name,
      description: description || undefined,
      systemPrompt: systemPrompt || undefined,
      defaultLLMConfigId: defaultLLMConfigId || undefined,
    }

    if (isNew) {
      createMutation.mutate(dto)
    } else {
      updateMutation.mutate({ id: id!, dto })
    }
  }

  return (
    <div className="p-8 max-w-2xl">
      <h1 className="text-3xl font-bold mb-8">
        {isNew ? 'Create Bot' : 'Edit Bot'}
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Name *
          </label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="My Bot"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Description
          </label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={2}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="A helpful assistant..."
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            System Prompt
          </label>
          <textarea
            value={systemPrompt}
            onChange={(e) => setSystemPrompt(e.target.value)}
            rows={4}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent font-mono text-sm"
            placeholder="You are a helpful assistant..."
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Default LLM Configuration
          </label>
          <select
            value={defaultLLMConfigId}
            onChange={(e) => setDefaultLLMConfigId(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="">Select LLM Configuration</option>
            {llmConfigs?.map((config) => (
              <option key={config.id} value={config.id}>
                {config.name} ({config.providerType} - {config.modelId})
              </option>
            ))}
          </select>
        </div>

        <button
          type="submit"
          disabled={createMutation.isPending || updateMutation.isPending}
          className="flex items-center gap-2 bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
        >
          <Save size={20} />
          {isNew ? 'Create Bot' : 'Save Changes'}
        </button>
      </form>
    </div>
  )
}
