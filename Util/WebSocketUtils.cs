using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.TabPort.util;

public class WebSocketUtils
{
    private static CancellationTokenSource _cts = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public void RestartWebSocketServer(int port)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        StartWebSocketServer(port);
    }

    public void StartWebSocketServer(int port)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:" + port + "/");
        listener.Start();
        Log.Info("[WebSocketUtils] WebSocket server started on port " + port, GetType());
        new Thread(Start).Start();
        return;

        async void Start()
        {
            try
            {
                while (true)
                {
                    var context = await listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Info("[WebSocketUtils] Error: " + e.Message, GetType());
            }
        }
    }

    private async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        var webSocketContext = await context.AcceptWebSocketAsync(null);
        var clientWebSocket = webSocketContext.WebSocket;

        try
        {
            var buffer = new byte[4096];
            while (clientWebSocket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                    {
                        using var reader = new StreamReader(ms, Encoding.UTF8);
                        var message = await reader.ReadToEndAsync();
                        if (message == "ping") continue;
                        if (message.StartsWith("onWindowRemoved"))
                        {
                            var arr = message.Split(" ");
                            RuntimeStaticData.WindowIdHWndMap.TryRemove(Convert.ToInt64(arr[1]), out var hWnd);
                            RuntimeStaticData.SavedHWnd.TryRemove(hWnd, out _);
                            continue;
                        }

                        WriteToDictionary(message, clientWebSocket);
                        break;
                    }
                    case WebSocketMessageType.Close:
                        await HandleClientClosing(clientWebSocket);
                        break;
                    case WebSocketMessageType.Binary:
                        break;
                    default:
                        var exception = new ArgumentOutOfRangeException
                        {
                            HelpLink = null,
                            HResult = 1,
                            Source = "WebSocketUtils",
                        };
                        throw exception;
                }
            }
        }
        catch (WebSocketException ex)
        {
            Log.Error($"[WebSocketUtils] WebSocket error: {ex.Message}", GetType());
        }
        catch (OperationCanceledException)
        {
            Log.Error("[WebSocketUtils] Operation was canceled.", GetType());
        }
        finally
        {
            await CleanupClientConnection(clientWebSocket);
        }
    }

    private void WriteToDictionary(string message, WebSocket clientWebSocket)
    {
        try
        {
            var tabsObj = JsonSerializer.Deserialize<List<BrowserTab>>(message, JsonOptions);
            RuntimeStaticData.BrowserTabData.AddOrUpdate(clientWebSocket, tabsObj,
                (_, _) => tabsObj);
        }
        catch (Exception e)
        {
            Log.Info($"[WebSocketUtils] Error when trying to parse message: {e.Message}", GetType());
        }
    }

    public static async Task SendMessageAsync(string message, WebSocket clientWebSocket)
    {
        if (clientWebSocket is { State: WebSocketState.Open })
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        else
        {
#if DEBUG
            Log.Error("[WebSocketUtils] WebSocket is not open When sending message: " + message, typeof(WebSocket));
#endif
        }
    }

    private static async Task CleanupClientConnection(WebSocket clientWebSocket)
    {
        Log.Info("[WebSocketUtils] Client disconnected", typeof(WebSocket));
        if (clientWebSocket != null)
        {
            if (clientWebSocket.State == WebSocketState.Closed)
            {
                RuntimeStaticData.BrowserTabData.Remove(clientWebSocket, out _);
                await HandleClientClosing(clientWebSocket);
            }
            clientWebSocket.Dispose();
        }
    }

    private static async Task HandleClientClosing(WebSocket clientWebSocket)
    {
        try
        {
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
        }
        catch (WebSocketException ex)
        {
            Log.Error($"[WebSocketUtils] Error during closing handshake: {ex.Message}", typeof(WebSocketUtils));
        }
    }
}