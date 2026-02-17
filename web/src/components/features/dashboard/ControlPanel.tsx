import React from 'react';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { signalRService } from '../../../services/signalr.service';
import { toast } from 'sonner';
import { Activity, Users, Trash2, LogOut } from 'lucide-react';
import { useEventStore } from '../../../stores/eventStore';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../../stores/authStore';
import { useConnectionStore } from '../../../stores/connectionStore';

export const ControlPanel: React.FC = () => {
  const clearEvents = useEventStore((state) => state.clearEvents);
  const clearAuth = useAuthStore((state) => state.clearAuth);
  const resetConnection = useConnectionStore((state) => state.reset);
  const navigate = useNavigate();

  const handlePing = async () => {
    try {
      const result = await signalRService.ping();
      toast.success(result);
    } catch (error) {
      toast.error('Ping failed');
    }
  };

  const handleGetGroups = async () => {
    try {
      const groups = await signalRService.getMyGroups();
      toast.success(
        <div>
          <strong>My Groups:</strong>
          <pre className="mt-2 text-xs">{JSON.stringify(groups, null, 2)}</pre>
        </div>
      );
    } catch (error) {
      toast.error('Failed to get groups');
    }
  };

  const handleClearLog = () => {
    clearEvents();
    toast.info('Event log cleared');
  };

  const handleDisconnect = async () => {
    try {
      await signalRService.disconnect();
      resetConnection();
      clearAuth();
      toast.info('Disconnected');
      navigate('/login');
    } catch (error) {
      toast.error('Failed to disconnect');
    }
  };

  return (
    <Card title="Control Panel">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <Button onClick={handlePing} variant="primary" size="sm" className="flex items-center gap-2">
          <Activity className="w-4 h-4" />
          Ping
        </Button>
        <Button onClick={handleGetGroups} variant="secondary" size="sm" className="flex items-center gap-2">
          <Users className="w-4 h-4" />
          Groups
        </Button>
        <Button onClick={handleClearLog} variant="warning" size="sm" className="flex items-center gap-2">
          <Trash2 className="w-4 h-4" />
          Clear Log
        </Button>
        <Button onClick={handleDisconnect} variant="danger" size="sm" className="flex items-center gap-2">
          <LogOut className="w-4 h-4" />
          Logout
        </Button>
      </div>
    </Card>
  );
};
