
using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                await ProcessWebSocketRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async Task ProcessWebSocketRequest(HttpListenerContext context)
    {
        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
        WebSocket socket = webSocketContext.WebSocket;
        Console.WriteLine("Client connected.");

        byte[] buffer = new byte[1024];
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string receivedMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received message: {receivedMessage}");
                await socket.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes("Connected")), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
}