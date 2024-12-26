import {
  Box,
  Card,
  CardContent,
  IconButton,
  Typography,
  Switch,
  TextField,
  Button,
  Alert,
} from '@mui/material';
import Snackbar from '@mui/material/Snackbar';
import {
  Wifi as WifiIcon,
  WifiOff as WifiOffIcon,
  Settings as SettingsIcon,
  ArrowBack as ArrowBackIcon,
} from '@mui/icons-material';
import { useWebSocketConfigStore } from '@/stores/webSocketConfigStore';
import { useWebSocketStore } from '@/stores/websocketStore';

function connect() {
  browser.runtime.sendMessage('connect');
}

function disconnect() {
  browser.runtime.sendMessage('disconnect');
}

export default function Popup() {
  const {
    isConnected,
    isConnecting,
  } = useWebSocketStore();

  const {
    host,
    port,
    autoConnect,
    setHost,
    setPort,
    setAutoConnect
  } = useWebSocketConfigStore();

  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');

  browser.runtime.onMessage.addListener((message: string) => {
    setSnackbarMessage(message);
    setSnackbarOpen(true);
  })

  useEffect(() => {
    if (autoConnect && !isConnected && !isConnecting) {
      connect();
    }
  }, []);

  return (
    <Box sx={{ p: 2, minWidth: 270 }}>
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={2000}
        onClose={() => setSnackbarOpen(false)}
      >
        <Alert variant="filled" severity="error">{snackbarMessage}</Alert>
      </Snackbar>
      <Card elevation={2}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="h6" component="div">
              TabPort
            </Typography>
            <IconButton
              onClick={() => setShowSettings(!showSettings)}
              color={showSettings ? 'primary' : 'default'}
            >
              {showSettings ? <ArrowBackIcon /> : <SettingsIcon />}
            </IconButton>
          </Box>
          {!showSettings ? (
            <Button
              onClick={isConnected ? disconnect : connect}
              disabled={isConnecting}
              variant="text"
              fullWidth={true}
              sx={{ color: 'white' }}
            >
              <Box sx={{
                display: 'flex',
                alignItems: 'center',
                bgcolor: isConnected ? 'success.light' : 'error.light',
                p: 2,
                borderRadius: 1,
                width: '100%',
                color: 'white',
                justifyContent: 'center'
              }}>
                {isConnected ?
                  <WifiIcon sx={{ mr: 1 }} /> :
                  <WifiOffIcon sx={{ mr: 1 }} />
                }
                <Typography>
                  {isConnecting ? 'Connecting...' : isConnected ? 'Connected' : 'Not connected'}
                </Typography>
              </Box>
            </Button>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <TextField
                label="Host Address"
                value={host}
                onChange={(e) => setHost(e.target.value)}
                size="small"
                fullWidth
              />
              <TextField
                label="Port"
                value={port}
                onChange={(e) => setPort(Number(e.target.value))}
                size="small"
                fullWidth
              />
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Typography>Auto Connect</Typography>
                <Switch
                  checked={autoConnect}
                  onChange={(e) => setAutoConnect(e.target.checked)}
                />
              </Box>
            </Box>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}