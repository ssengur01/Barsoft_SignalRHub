import { create } from 'zustand';

interface ConnectionState {
  status: 'disconnected' | 'connecting' | 'connected' | 'reconnecting';
  connectionId: string | null;
  groups: string[];
  setStatus: (status: ConnectionState['status']) => void;
  setConnectionInfo: (connectionId: string, groups: string[]) => void;
  reset: () => void;
}

export const useConnectionStore = create<ConnectionState>((set) => ({
  status: 'disconnected',
  connectionId: null,
  groups: [],
  setStatus: (status) => set({ status }),
  setConnectionInfo: (connectionId, groups) => set({ connectionId, groups }),
  reset: () => set({ status: 'disconnected', connectionId: null, groups: [] }),
}));
