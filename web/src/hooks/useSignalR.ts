import { useEffect, useRef } from 'react';
import { signalRService } from '../services/signalr.service';
import { useAuthStore } from '../stores/authStore';
import { useConnectionStore } from '../stores/connectionStore';
import { useEventStore } from '../stores/eventStore';
import type { StokHareketDto } from '../types/api.types';
import { toast } from 'sonner';

export const useSignalR = () => {
  const token = useAuthStore((state) => state.token);
  const setStatus = useConnectionStore((state) => state.setStatus);
  const setConnectionInfo = useConnectionStore((state) => state.setConnectionInfo);
  const addEvent = useEventStore((state) => state.addEvent);
  const isConnectedRef = useRef(false);

  useEffect(() => {
    if (!token || isConnectedRef.current) return;

    const connectToHub = async () => {
      try {
        console.log('[SignalR] Starting connection...');
        setStatus('connecting');
        isConnectedRef.current = true;

        // Setup event listeners BEFORE connecting
        signalRService.onStokHareketCreated((data: StokHareketDto | Record<string, any>) => {
          console.log('[SignalR] StokHareketCreated event received:', data);
          addEvent({
            type: 'created',
            data: data as StokHareketDto,
            timestamp: new Date(),
          });
        });

        signalRService.onStokHareketUpdated((data: StokHareketDto | Record<string, any>) => {
          console.log('[SignalR] StokHareketUpdated event received:', data);
          addEvent({
            type: 'updated',
            data: data as StokHareketDto,
            timestamp: new Date(),
          });
        });

        signalRService.onReconnecting(() => {
          console.log('[SignalR] Reconnecting...');
          setStatus('reconnecting');
          toast.warning('Reconnecting to SignalR Hub...');
        });

        signalRService.onReconnected(() => {
          console.log('[SignalR] Reconnected!');
          setStatus('connected');
          toast.success('Reconnected to SignalR Hub');
        });

        signalRService.onClose(() => {
          console.log('[SignalR] Connection closed');
          setStatus('disconnected');
          isConnectedRef.current = false;
        });

        // Connect
        await signalRService.connect(token);
        console.log('[SignalR] Connection established');
        setStatus('connected');

        const connectionId = signalRService.getConnectionId();
        if (connectionId) {
          console.log('[SignalR] Connection ID:', connectionId);
          // Fetch groups info
          try {
            const groupsInfo = await signalRService.getMyGroups();
            console.log('[SignalR] Groups:', groupsInfo);
            setConnectionInfo(connectionId, groupsInfo.Groups || []);
            toast.success(`Connected to SignalR Hub (Groups: ${groupsInfo.Groups?.join(', ') || 'none'})`);
          } catch (error) {
            console.error('[SignalR] Failed to get groups:', error);
            setConnectionInfo(connectionId, []);
            toast.success('Connected to SignalR Hub');
          }
        }
      } catch (error) {
        console.error('[SignalR] Connection error:', error);
        setStatus('disconnected');
        isConnectedRef.current = false;
        toast.error(`Failed to connect: ${error instanceof Error ? error.message : 'Unknown error'}`);
      }
    };

    connectToHub();

    // Cleanup function - only disconnect when component unmounts
    return () => {
      console.log('[SignalR] Cleanup - disconnecting');
      signalRService.disconnect();
      isConnectedRef.current = false;
    };
  }, [token]); // Only depend on token
};
