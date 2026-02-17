import React from 'react';
import { Card } from '../../ui/Card';
import { Badge } from '../../ui/Badge';
import { useConnectionStore } from '../../../stores/connectionStore';
import { useAuthStore } from '../../../stores/authStore';
import { Wifi, WifiOff } from 'lucide-react';

export const ConnectionStatus: React.FC = () => {
  const { status, connectionId } = useConnectionStore();
  const user = useAuthStore((state) => state.user);

  const statusConfig = {
    connected: { label: 'Connected', variant: 'success' as const, icon: <Wifi className="w-4 h-4" /> },
    connecting: { label: 'Connecting...', variant: 'warning' as const, icon: <Wifi className="w-4 h-4" /> },
    reconnecting: { label: 'Reconnecting...', variant: 'warning' as const, icon: <Wifi className="w-4 h-4" /> },
    disconnected: { label: 'Disconnected', variant: 'danger' as const, icon: <WifiOff className="w-4 h-4" /> },
  };

  const config = statusConfig[status];

  return (
    <Card title="Connection Status">
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-gray-700">Status</span>
          <Badge variant={config.variant} className="flex items-center gap-1">
            {config.icon}
            {config.label}
          </Badge>
        </div>

        {connectionId && (
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium text-gray-700">Connection ID</span>
            <span className="text-xs font-mono text-gray-600 bg-gray-100 px-2 py-1 rounded">
              {connectionId.substring(0, 8)}...
            </span>
          </div>
        )}

        {user && (
          <>
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium text-gray-700">User</span>
              <span className="text-sm text-gray-900">{user.userCode}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium text-gray-700">Branches</span>
              <span className="text-sm text-gray-900">{user.subeIds.join(', ')}</span>
            </div>
            {user.isAdmin && (
              <Badge variant="primary">Admin</Badge>
            )}
          </>
        )}
      </div>
    </Card>
  );
};
