using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class Program : Form
{
    private Button refreshButton;
    private Label statusLabel;
    private HttpListener httpListener;

    public Program()
    {
        // Form settings
        this.Text = "WebSocket Server";
        this.Size = new System.Drawing.Size(400, 200);

        // Create the Refresh button
        refreshButton = new Button()
        {
            Text = "Start WebSocket Server",
            Location = new System.Drawing.Point(50, 50),
            Size = new System.Drawing.Size(300, 40)
        };
        refreshButton.Click += RefreshButton_Click;

        // Create the Status label
        statusLabel = new Label()
        {
            Text = "Server Status: Stopped",
            Location = new System.Drawing.Point(50, 100),
            Size = new System.Drawing.Size(300, 30)
        };

        // Add controls to the form
        this.Controls.Add(refreshButton);
        this.Controls.Add(statusLabel);
    }

    private async void RefreshButton_Click(object sender, EventArgs e)
    {
        // Update the status label
        statusLabel.Text = "Server Status: Starting...";

        // Start the WebSocket server in a background task
        await StartWebSocketServer();

        // Update the status label when the server is ready
        statusLabel.Text = "Server Status: Running";
    }

    private async Task StartWebSocketServer()
    {
        // Initialize the HttpListener for WebSocket connection
        httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:8080/");
        httpListener.Start();
        Console.WriteLine("WebSocket server is listening on ws://localhost:8080");

        // Listen for incoming requests
        while (true)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();

            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                Console.WriteLine("Client connected.");

                // Send a "connected" message to the client
                var message = Encoding.UTF8.GetBytes("Connected");
                await wsContext.WebSocket.SendAsync(
                    new ArraySegment<byte>(message),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                // Continue handling WebSocket messages
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

    [STAThread]
    public static void Main()
    {
        // Run the Windows Form application
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Program());
    }
}
