import * as signalR from '@microsoft/signalr';
import type { StokHareketDto } from '../types/api.types';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private eventHandlers: Map<string, Function[]> = new Map();

  async connect(token: string): Promise<void> {
    const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubUrl}?access_token=${token}`, {
        skipNegotiation: false,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Debug)
      .build();

    // Register all pending event handlers
    this.eventHandlers.forEach((callbacks, eventName) => {
      callbacks.forEach(callback => {
        console.log(`[SignalR Service] Registering handler for: ${eventName}`);
        this.connection?.on(eventName, callback as any);
      });
    });

    console.log('[SignalR Service] Starting connection to:', hubUrl);
    await this.connection.start();
    console.log('[SignalR Service] Connection started successfully');
  }

  onStokHareketCreated(callback: (event: StokHareketDto | Record<string, any>) => void): void {
    if (!this.eventHandlers.has('StokHareketCreated')) {
      this.eventHandlers.set('StokHareketCreated', []);
    }
    this.eventHandlers.get('StokHareketCreated')?.push(callback);

    // If already connected, register immediately
    if (this.connection) {
      console.log('[SignalR Service] Registering StokHareketCreated handler immediately');
      this.connection.on('StokHareketCreated', callback);
    }
  }

  onStokHareketUpdated(callback: (event: StokHareketDto | Record<string, any>) => void): void {
    if (!this.eventHandlers.has('StokHareketUpdated')) {
      this.eventHandlers.set('StokHareketUpdated', []);
    }
    this.eventHandlers.get('StokHareketUpdated')?.push(callback);

    // If already connected, register immediately
    if (this.connection) {
      console.log('[SignalR Service] Registering StokHareketUpdated handler immediately');
      this.connection.on('StokHareketUpdated', callback);
    }
  }

  onReconnecting(callback: () => void): void {
    this.connection?.onreconnecting(callback);
  }

  onReconnected(callback: () => void): void {
    this.connection?.onreconnected(callback);
  }

  onClose(callback: () => void): void {
    this.connection?.onclose(callback);
  }

  async ping(): Promise<string> {
    if (!this.connection) throw new Error('Not connected');
    return await this.connection.invoke('Ping');
  }

  async getMyGroups(): Promise<any> {
    if (!this.connection) throw new Error('Not connected');
    return await this.connection.invoke('GetMyGroups');
  }

  async disconnect(): Promise<void> {
    console.log('[SignalR Service] Disconnecting...');
    await this.connection?.stop();
    this.connection = null;
    this.eventHandlers.clear();
  }

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state ?? null;
  }

  getConnectionId(): string | null {
    return this.connection?.connectionId ?? null;
  }
}

export const signalRService = new SignalRService();
