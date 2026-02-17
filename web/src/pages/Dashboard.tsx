import { useSignalR } from '../hooks/useSignalR';
import { useEventStore } from '../stores/eventStore';
import { ConnectionStatus } from '../components/features/dashboard/ConnectionStatus';
import { StatsCard } from '../components/features/dashboard/StatsCard';
import { ControlPanel } from '../components/features/dashboard/ControlPanel';
import { EventLog } from '../components/features/dashboard/EventLog';
import { RecentMovements } from '../components/features/dashboard/RecentMovements';
import { TrendingUp, TrendingDown, Activity } from 'lucide-react';

export default function Dashboard() {
  useSignalR(); // Auto-connect to SignalR hub

  const { createdCount, updatedCount, events } = useEventStore();

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <h1 className="text-2xl font-bold text-gray-900">
            Barsoft SignalR Hub - Dashboard
          </h1>
          <p className="text-sm text-gray-600 mt-1">Real-time Stock Movement Monitoring</p>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {/* Stats Row */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <ConnectionStatus />

          <StatsCard
            title="Created Events"
            value={createdCount}
            variant="success"
            icon={<TrendingUp className="w-8 h-8" />}
          />

          <StatsCard
            title="Updated Events"
            value={updatedCount}
            variant="warning"
            icon={<TrendingDown className="w-8 h-8" />}
          />

          <StatsCard
            title="Total Events"
            value={events.length}
            variant="primary"
            icon={<Activity className="w-8 h-8" />}
          />
        </div>

        {/* Control Panel */}
        <div className="mb-6">
          <ControlPanel />
        </div>

        {/* Recent Movements Table */}
        <div className="mb-6">
          <RecentMovements />
        </div>

        {/* Event Log */}
        <EventLog />
      </div>
    </div>
  );
}
