import { initPegasusZustandStoreBackend, pegasusZustandStoreReady } from "@webext-pegasus/store-zustand";
import { persist } from "zustand/middleware";
import { create } from "zustand/react";

interface WebSocketConfig {
  host: string;
  port: number;
  autoConnect: boolean;

  setHost: (host: string) => void;
  setPort: (port: number) => void;
  setAutoConnect: (autoConnect: boolean) => void;
}

export const useWebSocketConfigStore = create<WebSocketConfig>()(
  persist(
    (set, get) => ({
      host: 'localhost',
      port: 8899,
      autoConnect: true,

      setHost: (host: string) => set({ host }),
      setPort: (port: number) => set({ port }),
      setAutoConnect: (autoConnect: boolean) => set({ autoConnect }),
    }) as WebSocketConfig,
    {
      name: 'websocket-config'
    }
  )
);

export const STORE_NAME = 'GlobalWebSocketConfigStore';

export const webSocketConfigStoreBackendReady = () =>
  initPegasusZustandStoreBackend(STORE_NAME, useWebSocketConfigStore, {
    storageStrategy: 'sync'
  });

export const webSocketConfigStoreReady = () =>
  pegasusZustandStoreReady(STORE_NAME, useWebSocketConfigStore);