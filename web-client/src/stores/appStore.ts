import { create } from 'zustand'
import type { BotInstance } from '../types'

interface AppState {
  selectedBot: BotInstance | null
  setSelectedBot: (bot: BotInstance | null) => void
  sidebarOpen: boolean
  toggleSidebar: () => void
}

export const useAppStore = create<AppState>((set) => ({
  selectedBot: null,
  setSelectedBot: (bot) => set({ selectedBot: bot }),
  sidebarOpen: true,
  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
}))
