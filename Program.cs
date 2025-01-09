using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

class Program
{
    public static async Task Main(string[] args)
    {
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:8080/");
        httpListener.Start();
        Console.WriteLine("WebSocket server is listening on ws://localhost:8080");

        while (true)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();
            
            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                Console.WriteLine("Client connected.");
                
                // Automatically send a "connected" message
                var message = Encoding.UTF8.GetBytes("Connected");
                await wsContext.WebSocket.SendAsync(
                    new ArraySegment<byte>(message),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                // Continue to handle messages
                await HandleWebSocket(wsContext.WebSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async Task HandleWebSocket(WebSocket webSocket)
    {
        byte[] buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {receivedMessage}");

                // Echo the message back to the client
                var response = Encoding.UTF8.GetBytes($"Echo: {receivedMessage}");
                await webSocket.SendAsync(
                    new ArraySegment<byte>(response),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Client disconnected.");
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Goodbye",
                    CancellationToken.None
                );
            }
        }
    }
}
