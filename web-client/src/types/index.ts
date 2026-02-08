export interface BotInstance {
  id: string
  name: string
  description?: string
  systemPrompt?: string
  defaultLLMConfigId?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateBotDto {
  name: string
  description?: string
  systemPrompt?: string
  defaultLLMConfigId?: string
}

export interface UpdateBotDto {
  name?: string
  description?: string
  systemPrompt?: string
  defaultLLMConfigId?: string
  isActive?: boolean
}

export interface LLMConfig {
  id: string
  name: string
  providerType: string
  modelId?: string
  endpoint?: string
  createdAt: string
  updatedAt: string
}

export interface LLMProvider {
  name: string
  displayName: string
  supportsStreaming: boolean
  supportsFunctionCalling: boolean
  supportedModels: string[]
}

export interface ChannelInfo {
  channelType: string
  settings: ChannelSettingDefinition[]
}

export interface ChannelSettingDefinition {
  key: string
  displayName: string
  description: string
  type: 'String' | 'Secret' | 'Number' | 'Boolean' | 'Select'
  isRequired: boolean
  defaultValue?: string
}

export interface PluginSettingDefinition {
  key: string
  displayName: string
  description: string
  type: 'String' | 'Secret' | 'Number' | 'Boolean' | 'Select'
  isRequired: boolean
  defaultValue?: string
}

export interface PluginInfo {
  id: string
  name: string
  description: string
  version: string
  author?: string
  dependencies?: string[]
  settings?: PluginSettingDefinition[]
}

export interface BotPluginStatus {
  id: string
  name: string
  description: string
  version: string
  isEnabled: boolean
  settings?: Record<string, string>
}

export interface Conversation {
  id: string
  botInstanceId: string
  channelId: string
  userId: string
  title?: string
  createdAt: string
  lastMessageAt: string
}

export interface Message {
  id: string
  conversationId: string
  role: 'User' | 'Assistant' | 'System' | 'Tool'
  content: string
  createdAt: string
}

export interface ScheduledJob {
  id: string
  name: string
  description?: string
  instructions: string
  cronExpression: string
  botInstanceId: string
  botName?: string
  targetChannelConfigId?: string
  targetChannelType?: string
  isEnabled: boolean
  lastRunAt?: string
  nextRunAt?: string
  lastRunStatus?: 'Success' | 'Failed' | 'Running'
  createdAt: string
  updatedAt: string
}

export interface CreateScheduledJobDto {
  name: string
  description?: string
  instructions: string
  cronExpression: string
  botInstanceId: string
  targetChannelConfigId?: string
}

export interface UpdateScheduledJobDto {
  name?: string
  description?: string
  instructions?: string
  cronExpression?: string
  targetChannelConfigId?: string
  isEnabled?: boolean
}

export interface JobExecution {
  id: string
  scheduledJobId: string
  startedAt: string
  completedAt?: string
  status: 'Running' | 'Completed' | 'Failed'
  output?: string
  errorMessage?: string
}
