import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './style.css';
import { webSocketConfigStoreReady } from '@/stores/webSocketConfigStore.ts';
import { webSocketStoreReady } from '@/stores/websocketStore.ts';
import { initPegasusTransport } from '@webext-pegasus/transport/popup';

initPegasusTransport();

webSocketConfigStoreReady().then(() => {
  webSocketStoreReady().then(() => {
    ReactDOM.createRoot(document.getElementById('root')!).render(
      <React.StrictMode>
        <App />
      </React.StrictMode>,
    );
  });
});