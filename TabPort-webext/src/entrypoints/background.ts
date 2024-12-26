import { useWebSocketConfigStore, webSocketConfigStoreBackendReady } from "@/stores/webSocketConfigStore";
import { useWebSocketStore, webSocketStoreBackendReady } from "@/stores/websocketStore";
import { initPegasusTransport } from "@webext-pegasus/transport/background";

const TEN_SECONDS_MS = 10 * 1000;
let keepAliveIntervalId: number | null = null;
let tabUpdateListener: any | null = null;

browser.runtime.onStartup.addListener(() => {
  const { isConnected, isConnecting, setIsConnected, setIsConnecting, setSocket } = useWebSocketStore.getState();
  const { host, port, autoConnect, } = useWebSocketConfigStore.getState();
  if (autoConnect && !isConnected && !isConnecting) {
    console.log('Auto-connecting to WebSocket server at:', `ws://${host}:${port}`);
    connect(
      host,
      port,
      setIsConnected,
      setIsConnecting,
      setSocket,
      setupTabUpdateListener,
      sendTabs
    );
  }
});

export default defineBackground(async () => {
  initPegasusTransport();
  try {
    await webSocketConfigStoreBackendReady();
    await webSocketStoreBackendReady();
  } catch (error) {
    console.error("Error initializing stores:", error);
    return; // Consider whether to proceed if store initialization fails
  }

  const { setIsConnected, setIsConnecting, setSocket } = useWebSocketStore.getState();

  browser.runtime.onMessage.addListener((message: string) => {
    switch (message) {
      case 'connect':
        connect(
          useWebSocketConfigStore.getState().host,
          useWebSocketConfigStore.getState().port,
          setIsConnected,
          setIsConnecting,
          setSocket,
          setupTabUpdateListener,
          sendTabs
        );
        break;
      case 'disconnect':
        disconnect();
        break;
    }
  });

  function removeTabUpdateListener() {
    if (!tabUpdateListener) return;

    browser.tabs.onUpdated.removeListener(tabUpdateListener);
    browser.tabs.onActivated.removeListener(tabUpdateListener);
    browser.tabs.onDetached.removeListener(tabUpdateListener);
    browser.windows.onRemoved.removeListener(onWindowRemoved);
    browser.windows.onCreated.removeListener(tabUpdateListener);
    tabUpdateListener = null;
  }

  function disconnect() {
    removeTabUpdateListener();
    const { socket } = useWebSocketStore.getState();
    if (socket) {
      socket.close();
    }
  }
});

function connect(
  host: string,
  port: number,
  setIsConnected: (value: boolean) => void,
  setIsConnecting: (value: boolean) => void,
  setSocket: (socket: WebSocket | null) => void,
  setupTabUpdateListener: () => void,
  sendTabs: () => void
) {
  console.log('Connecting to WebSocket server at:', `ws://${host}:${port}`);
  setIsConnecting(true);
  let ws: WebSocket;
  try {
    ws = new WebSocket(`ws://${host}:${port}`);
  } catch (error) {
    console.error('Invalid WebSocket URL:', error);
    browser.runtime.sendMessage("Invalid WebSocket URL.");
    setIsConnecting(false);
    return;
  }

  ws.onopen = () => {
    console.log('WebSocket connection opened.');
    setSocket(ws);
    setIsConnected(true);
    setIsConnecting(false);
    setupTabUpdateListener();
    keepAlive();
    sendTabs();
  };

  ws.onmessage = (event) => {
    if (!event.data) {
      console.warn("Received empty message from WebSocket.");
      return;
    }

    const [command, ...args] = event.data.split(' ');
    const argumentString = args.join(' ');

    switch (command) {
      case "query":
        sendTabs();
        break;
      case "switch":
        handleSwitchCommand(argumentString);
        break;
      case "close":
        handleCloseCommand(argumentString);
        break;
      case "reNameDuplicate":
        handleRenameDuplicateCommand(argumentString);
        break;
      case "restore":
        handleRestoreCommand(argumentString);
        break;
      default:
        console.error("Unknown WebSocket command:", command);
    }
  };

  ws.onclose = () => {
    console.log('WebSocket connection closed.');
    setSocket(null);
    setIsConnected(false);
    clearInterval(keepAliveIntervalId!);
    keepAliveIntervalId = null;
  };

  ws.onerror = (error) => {
    console.error('WebSocket error:', error);
    setSocket(null);
    setIsConnected(false);
    setIsConnecting(false);
    // Consider implementing a reconnect mechanism here
  };
}

// Helper functions for specific commands
function handleSwitchCommand(argumentString: string) {
  const [tabIdStr, windowIdStr] = argumentString.split(' ');
  const tabId = parseInt(tabIdStr, 10);
  const windowId = parseInt(windowIdStr, 10);

  if (isNaN(tabId) || isNaN(windowId)) {
    console.error("Invalid tabId or windowId received:", tabIdStr, windowIdStr);
    return;
  }

  browser.tabs.update(tabId, { active: true }).then(() => {
    if (browser.runtime.lastError) {
      console.error("Error executing script for tab:", tabId, browser.runtime.lastError.message);
    }
  });
  browser.windows.update(windowId, { focused: true, drawAttention: true }).then(() => {
    if (browser.runtime.lastError) {
      console.error("Error executing script for tab:", tabId, browser.runtime.lastError.message);
    }
  });
}

function handleCloseCommand(argumentString: string) {
  const tabId = parseInt(argumentString, 10);

  if (isNaN(tabId)) {
    console.error("Invalid tabId received:", argumentString);
    return;
  }

  browser.tabs.remove(tabId).then(() => {
    if (browser.runtime.lastError) {
      console.error("Error executing script for tab:", tabId, browser.runtime.lastError.message);
    }
  });
}

function handleRenameDuplicateCommand(argumentString: string) {
  const tabIds = argumentString.split(' ').map(id => parseInt(id, 10));

  for (const tabId of tabIds) {
    if (isNaN(tabId)) {
      console.error("Invalid tabId in rename command:", tabId);
      continue;
    }

    browser.scripting.executeScript({
      target: { tabId },
      func: () => {
        document.title = document.title + ' (' + crypto.randomUUID() + ')';
      }
    }).then(() => {
      if (browser.runtime.lastError) {
        console.error("Error executing script for tab:", tabId, browser.runtime.lastError.message);
      }
    });
  }
}

function handleRestoreCommand(argumentString: string) {
  const tabIds = argumentString.split(' ').map(id => parseInt(id, 10));

  for (const tabId of tabIds) {
    if (isNaN(tabId)) {
      console.error("Invalid tabId in restore command:", tabId);
      continue;
    }

    browser.scripting.executeScript({
      target: { tabId },
      func: () => {
        document.title = document.title.slice(0, -39);
      }
    }).then(() => {
      if (browser.runtime.lastError) {
        console.error("Error executing script for tab:", tabId, browser.runtime.lastError.message);
      }
    });
  }
}

function keepAlive() {
  if (keepAliveIntervalId) {
    clearInterval(keepAliveIntervalId);
  }

  // @ts-ignore
  keepAliveIntervalId = setInterval(() => {
    const { socket } = useWebSocketStore.getState();
    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send('ping');
    } else {
      console.log("socket is null", socket)
      clearInterval(keepAliveIntervalId!);
      keepAliveIntervalId = null;
    }
  }, TEN_SECONDS_MS);
}

function setupTabUpdateListener() {
  if (tabUpdateListener) return;

  tabUpdateListener = (event: { type: string }, ...args: any[]) => {
    sendTabs();
  };

  browser.tabs.onUpdated.addListener(tabUpdateListener);
  browser.tabs.onActivated.addListener(tabUpdateListener);
  browser.tabs.onDetached.addListener(tabUpdateListener);
  browser.windows.onCreated.addListener(tabUpdateListener);
  browser.windows.onRemoved.addListener(onWindowRemoved);
}

function sendTabs() {
  const { socket } = useWebSocketStore.getState();
  if (!socket || socket.readyState !== WebSocket.OPEN) {
    console.log('No active socket to send tabs to.');
    return;
  }

  try {
    browser.tabs.query({}).then((tabs) => {
      const jsonData = JSON.stringify(tabs);
      socket.send(jsonData);
    });
  } catch (error) {
    console.error("Error querying or sending tabs:", error);
  }
}

function onWindowRemoved(windowId: number) {
  console.log('Window removed:', windowId);
  const { socket } = useWebSocketStore.getState();
  if (socket && socket.readyState === WebSocket.OPEN) {
    socket.send(`onWindowRemoved ${windowId}`);
  } else {
    console.log("No active socket to send window removal event to.");
  }
}