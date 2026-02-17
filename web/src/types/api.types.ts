// User DTO
export interface UserDto {
  id: number;
  userCode: string;
  description: string;
  isAdmin: boolean;
  subeIds: number[];
  telefon?: string;
}

// Login DTOs
export interface LoginRequest {
  userCode: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: UserDto;
  expiresAt: string;
}

// Stock Movement DTO
export interface StokHareketDto {
  id: number;
  stokId: number;
  belgeKodu: string;
  belgeTarihi: string;
  miktar: number;
  birimFiyati: number;
  toplamTutar: number;
  kdvTutari: number;
  aciklama: string;
  createDate: string;
  changeDate?: string;
  masrafMerkeziId?: number;
  depoId: number;
}

// SignalR Event Types
export interface StokHareketEvent {
  type: 'created' | 'updated';
  data: StokHareketDto;
  timestamp: Date;
}
