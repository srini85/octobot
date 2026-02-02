import { useQuery } from '@tanstack/react-query'
import { Puzzle, Check } from 'lucide-react'
import { pluginsApi } from '../services/api'

export default function Plugins() {
  const { data: plugins, isLoading } = useQuery({
    queryKey: ['plugins'],
    queryFn: pluginsApi.getAll,
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
      <h1 className="text-3xl font-bold mb-8">Plugin Management</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {plugins?.map((plugin) => (
          <div key={plugin.id} className="bg-white rounded-lg shadow p-6">
            <div className="flex items-start justify-between mb-4">
              <div className="flex items-center gap-3">
                <Puzzle size={24} className="text-purple-600" />
                <div>
                  <h3 className="font-semibold">{plugin.name}</h3>
                  <p className="text-sm text-gray-500">v{plugin.version}</p>
                </div>
              </div>
              <button className="p-2 rounded-full bg-green-100 text-green-600">
                <Check size={16} />
              </button>
            </div>

            <p className="text-sm text-gray-600 mb-4">{plugin.description}</p>

            {plugin.author && (
              <p className="text-xs text-gray-500">By {plugin.author}</p>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
