import { create } from 'zustand';
import type { StokHareketEvent } from '../types/api.types';

interface EventState {
  events: StokHareketEvent[];
  createdCount: number;
  updatedCount: number;
  addEvent: (event: StokHareketEvent) => void;
  clearEvents: () => void;
}

export const useEventStore = create<EventState>((set) => ({
  events: [],
  createdCount: 0,
  updatedCount: 0,
  addEvent: (event) => {
    console.log('[EventStore] Adding event:', event.type, event.data);
    set((state) => {
      const newState = {
        events: [event, ...state.events].slice(0, 100), // Keep last 100
        createdCount: event.type === 'created' ? state.createdCount + 1 : state.createdCount,
        updatedCount: event.type === 'updated' ? state.updatedCount + 1 : state.updatedCount,
      };
      console.log('[EventStore] New state:', {
        eventsCount: newState.events.length,
        createdCount: newState.createdCount,
        updatedCount: newState.updatedCount,
      });
      return newState;
    });
  },
  clearEvents: () => {
    console.log('[EventStore] Clearing all events');
    set({ events: [], createdCount: 0, updatedCount: 0 });
  },
}));
