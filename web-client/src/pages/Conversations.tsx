import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { MessageSquare, User } from 'lucide-react'
import { conversationsApi } from '../services/api'

export default function Conversations() {
  const { id: botId } = useParams()

  const { data: conversations, isLoading } = useQuery({
    queryKey: ['conversations', botId],
    queryFn: () => conversationsApi.getByBot(botId!),
    enabled: !!botId,
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
      <h1 className="text-3xl font-bold mb-8">Conversations</h1>

      {conversations?.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg shadow">
          <MessageSquare size={48} className="mx-auto text-gray-400 mb-4" />
          <h2 className="text-xl font-semibold mb-2">No conversations yet</h2>
          <p className="text-gray-600">Conversations will appear here when users interact with your bot</p>
        </div>
      ) : (
        <div className="space-y-4">
          {conversations?.map((conv) => (
            <div key={conv.id} className="bg-white rounded-lg shadow p-4">
              <div className="flex items-center gap-3 mb-2">
                <User size={20} className="text-gray-400" />
                <span className="font-medium">User: {conv.userId}</span>
                <span className="text-sm text-gray-500">via {conv.channelId}</span>
              </div>
              <div className="flex justify-between text-sm text-gray-500">
                <span>Created: {new Date(conv.createdAt).toLocaleString()}</span>
                <span>Last message: {new Date(conv.lastMessageAt).toLocaleString()}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
