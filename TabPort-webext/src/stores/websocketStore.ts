import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { initPegasusZustandStoreBackend, pegasusZustandStoreReady } from "@webext-pegasus/store-zustand";

interface WebSocketStore {
    isConnected: boolean;
    isConnecting: boolean;
    socket: WebSocket | null;

    setIsConnected: (isConnected: boolean) => void;
    setIsConnecting: (isConnecting: boolean) => void;
    setSocket: (socket: WebSocket | null) => void;
}

export const useWebSocketStore = create<WebSocketStore>()(
    immer(
        (set, get) => ({
            isConnected: false,
            isConnecting: false,
            socket: null,

            setIsConnected: (isConnected: boolean) => set({ isConnected }),
            setIsConnecting: (isConnecting: boolean) => set({ isConnecting }),
            setSocket: (socket: WebSocket | null) => set({ socket })
        }) as WebSocketStore
    )
);

export const STORE_NAME = 'GlobalWebSocketStore';

export const webSocketStoreBackendReady = () => initPegasusZustandStoreBackend(STORE_NAME, useWebSocketStore,
    {
        storageStrategy: 'session'
    }
);

export const webSocketStoreReady = () => pegasusZustandStoreReady(STORE_NAME, useWebSocketStore);
