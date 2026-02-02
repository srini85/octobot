import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import Dashboard from './pages/Dashboard'
import BotConfig from './pages/BotConfig'
import Channels from './pages/Channels'
import Plugins from './pages/Plugins'
import LLMSettings from './pages/LLMSettings'
import Conversations from './pages/Conversations'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="bots/new" element={<BotConfig />} />
        <Route path="bots/:id" element={<BotConfig />} />
        <Route path="bots/:id/channels" element={<Channels />} />
        <Route path="bots/:id/plugins" element={<Plugins />} />
        <Route path="bots/:id/conversations" element={<Conversations />} />
        <Route path="llm-settings" element={<LLMSettings />} />
      </Route>
    </Routes>
  )
}

export default App
