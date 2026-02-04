import { Link, Outlet, useLocation } from 'react-router-dom'
import { Bot, Home, Settings, MessageSquare, Puzzle, Radio, Menu, Clock } from 'lucide-react'
import { useAppStore } from '../stores/appStore'

const navItems = [
  { path: '/dashboard', label: 'Dashboard', icon: Home },
  { path: '/llm-settings', label: 'LLM Settings', icon: Settings },
  { path: '/scheduled-jobs', label: 'Scheduled Jobs', icon: Clock },
]

export default function Layout() {
  const location = useLocation()
  const { sidebarOpen, toggleSidebar, selectedBot } = useAppStore()

  const botNavItems = selectedBot ? [
    { path: `/bots/${selectedBot.id}`, label: 'Configuration', icon: Bot },
    { path: `/bots/${selectedBot.id}/channels`, label: 'Channels', icon: Radio },
    { path: `/bots/${selectedBot.id}/plugins`, label: 'Plugins', icon: Puzzle },
    { path: `/bots/${selectedBot.id}/conversations`, label: 'Conversations', icon: MessageSquare },
  ] : []

  return (
    <div className="min-h-screen flex">
      {/* Sidebar */}
      <aside className={`${sidebarOpen ? 'w-64' : 'w-16'} bg-gray-900 text-white transition-all duration-300`}>
        <div className="p-4 flex items-center justify-between">
          {sidebarOpen && <h1 className="text-xl font-bold">OctoBot</h1>}
          <button onClick={toggleSidebar} className="p-2 hover:bg-gray-800 rounded">
            <Menu size={20} />
          </button>
        </div>

        <nav className="mt-4">
          {navItems.map((item) => {
            const Icon = item.icon
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-3 px-4 py-3 hover:bg-gray-800 ${
                  location.pathname === item.path ? 'bg-gray-800 border-l-4 border-blue-500' : ''
                }`}
              >
                <Icon size={20} />
                {sidebarOpen && <span>{item.label}</span>}
              </Link>
            )
          })}

          {selectedBot && (
            <>
              <div className="mx-4 my-4 border-t border-gray-700" />
              {sidebarOpen && (
                <div className="px-4 py-2 text-sm text-gray-400">
                  {selectedBot.name}
                </div>
              )}
              {botNavItems.map((item) => {
                const Icon = item.icon
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    className={`flex items-center gap-3 px-4 py-3 hover:bg-gray-800 ${
                      location.pathname === item.path ? 'bg-gray-800 border-l-4 border-blue-500' : ''
                    }`}
                  >
                    <Icon size={20} />
                    {sidebarOpen && <span>{item.label}</span>}
                  </Link>
                )
              })}
            </>
          )}
        </nav>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto">
        <Outlet />
      </main>
    </div>
  )
}
