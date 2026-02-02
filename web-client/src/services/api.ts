import type { BotInstance, CreateBotDto, UpdateBotDto, LLMProvider, LLMConfig, ChannelInfo, PluginInfo, Conversation, Message } from '../types'

const API_BASE = '/api'

async function fetchApi<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${url}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  })

  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`)
  }

  return response.json()
}

// Bots
export const botsApi = {
  getAll: () => fetchApi<BotInstance[]>('/bots'),
  getById: (id: string) => fetchApi<BotInstance>(`/bots/${id}`),
  create: (dto: CreateBotDto) => fetchApi<BotInstance>('/bots', {
    method: 'POST',
    body: JSON.stringify(dto),
  }),
  update: (id: string, dto: UpdateBotDto) => fetchApi<BotInstance>(`/bots/${id}`, {
    method: 'PUT',
    body: JSON.stringify(dto),
  }),
  delete: (id: string) => fetch(`${API_BASE}/bots/${id}`, { method: 'DELETE' }),
}

// LLM Providers
export const llmApi = {
  getProviders: () => fetchApi<LLMProvider[]>('/llm-providers'),
  getConfigs: () => fetchApi<LLMConfig[]>('/llm-providers/configs'),
  createConfig: (dto: { name: string; providerType: string; modelId?: string; apiKey?: string; endpoint?: string }) =>
    fetchApi<LLMConfig>('/llm-providers/configs', {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
}

// Channels
export const channelsApi = {
  getAll: () => fetchApi<ChannelInfo[]>('/channels'),
}

// Plugins
export const pluginsApi = {
  getAll: () => fetchApi<PluginInfo[]>('/plugins'),
}

// Conversations
export const conversationsApi = {
  getByBot: (botId: string) => fetchApi<Conversation[]>(`/bots/${botId}/conversations`),
  getMessages: (botId: string, conversationId: string) =>
    fetchApi<{ messages: Message[] }>(`/bots/${botId}/conversations/${conversationId}/messages`),
}

// Chat
export const chatApi = {
  sendMessage: (botId: string, message: string, userId?: string) =>
    fetchApi<{ response: string }>(`/bots/${botId}/chat`, {
      method: 'POST',
      body: JSON.stringify({ message, userId }),
    }),
}
