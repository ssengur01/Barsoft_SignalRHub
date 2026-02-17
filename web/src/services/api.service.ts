import axios, { type AxiosInstance } from 'axios';
import type { LoginRequest, LoginResponse, UserDto, StokHareketDto } from '../types/api.types';

class ApiService {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: import.meta.env.VITE_API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Interceptor to add JWT token to requests
    this.client.interceptors.request.use((config) => {
      const token = localStorage.getItem('jwt_token');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });
  }

  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await this.client.post<LoginResponse>('/api/auth/login', credentials);
    return response.data;
  }

  async getCurrentUser(): Promise<UserDto> {
    const response = await this.client.get<UserDto>('/api/auth/me');
    return response.data;
  }

  async healthCheck(): Promise<any> {
    const response = await this.client.get('/health');
    return response.data;
  }

  async getRecentStokHareketler(count: number = 10): Promise<StokHareketDto[]> {
    const response = await this.client.get<StokHareketDto[]>(`/api/stokhareket/recent?count=${count}`);
    return response.data;
  }

  async getStokHareketById(id: number): Promise<StokHareketDto> {
    const response = await this.client.get<StokHareketDto>(`/api/stokhareket/${id}`);
    return response.data;
  }
}

export const apiService = new ApiService();
