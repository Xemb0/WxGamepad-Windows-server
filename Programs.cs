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
    private Label ipAddressLabel;
    private TextBox logsTextBox;
    private HttpListener httpListener;
    private string serverIpAddress;

    public Program()
    {
        // Form settings
        this.Text = "WebSocket Server";
        this.Size = new System.Drawing.Size(500, 300);

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

        // Create the IP Address label
        ipAddressLabel = new Label()
        {
            Text = "IP Address: ",
            Location = new System.Drawing.Point(50, 140),
            Size = new System.Drawing.Size(300, 30)
        };

        // Create the Logs TextBox
        logsTextBox = new TextBox()
        {
            Multiline = true,
            Location = new System.Drawing.Point(50, 180),
            Size = new System.Drawing.Size(400, 70),
            ReadOnly = true
        };

        // Add controls to the form
        this.Controls.Add(refreshButton);
        this.Controls.Add(statusLabel);
        this.Controls.Add(ipAddressLabel);
        this.Controls.Add(logsTextBox);
    }

    private async void RefreshButton_Click(object sender, EventArgs e)
    {
        // Update the status label
        statusLabel.Text = "Server Status: Starting...";
        logsTextBox.AppendText("Server starting...\n");

        // Start the WebSocket server in a background task
        await StartWebSocketServer();

        // Update the status label when the server is ready
        statusLabel.Text = "Server Status: Running";
        logsTextBox.AppendText("Server is running and listening on ws://localhost:3330\n");
    }

    private async Task StartWebSocketServer()
    {
        // Get the server's IP address
        serverIpAddress = GetLocalIPAddress();
        ipAddressLabel.Text = $"IP Address: {serverIpAddress}";
        logsTextBox.AppendText($"Server IP Address: {serverIpAddress}\n");

        // Initialize the HttpListener for WebSocket connection
        httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:3330/");
        httpListener.Start();

        logsTextBox.AppendText("WebSocket server is listening on ws://localhost:3330\n");

        // Listen for incoming WebSocket connections
        while (true)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();

            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                logsTextBox.AppendText("Client connected.\n");

                // Send a "HSK" message to the client
                var message = Encoding.UTF8.GetBytes("HSK");
                await wsContext.WebSocket.SendAsync(
                    new ArraySegment<byte>(message),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );

                // Handle WebSocket messages (ping-pong)
                await HandleWebSocket(wsContext.WebSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                logsTextBox.AppendText("Invalid connection attempt. (400 Bad Request)\n");
            }
        }
    }

    private async Task HandleWebSocket(WebSocket webSocket)
    {
        byte[] buffer = new byte[1024];

        // Wait for the handshake confirmation (HSK_DONE) from the client
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
        logsTextBox.AppendText($"Received message: {receivedMessage}\n");

        if (receivedMessage == "HSK_DONE")
        {
            logsTextBox.AppendText("Handshake complete, starting ping-pong.\n");
            await StartPingPong(webSocket);
        }
    }

    private async Task StartPingPong(WebSocket webSocket)
    {
        byte[] pingMessage = Encoding.UTF8.GetBytes("PING");
        byte[] pongMessage = Encoding.UTF8.GetBytes("PONG");

        while (webSocket.State == WebSocketState.Open)
        {
            // Send a ping message to the client
            await webSocket.SendAsync(
                new ArraySegment<byte>(pingMessage),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            logsTextBox.AppendText("Sent: PING\n");

            // Wait for the pong response from the client
            var buffer = new byte[1024];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (receivedMessage == "PONG")
            {
                logsTextBox.AppendText("Received: PONG\n");
            }

            // Wait for 5 seconds before sending another ping
            await Task.Delay(5000);
        }
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";  // Fallback to localhost
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