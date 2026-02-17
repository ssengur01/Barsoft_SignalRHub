import { create } from 'zustand';
import type { UserDto } from '../types/api.types';

interface AuthState {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  setAuth: (user: UserDto, token: string) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  token: localStorage.getItem('jwt_token'),
  isAuthenticated: !!localStorage.getItem('jwt_token'),
  setAuth: (user, token) => {
    localStorage.setItem('jwt_token', token);
    set({ user, token, isAuthenticated: true });
  },
  clearAuth: () => {
    localStorage.removeItem('jwt_token');
    set({ user: null, token: null, isAuthenticated: false });
  },
}));
