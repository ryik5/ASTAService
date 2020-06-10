using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WebsocketClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ClientLaunchAsync();
        }

        private static async void ClientLaunchAsync()
        {
            ClientWebSocket webSocket = null;
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri("ws://localhost:5000"), CancellationToken.None);

            // Do something with WebSocket

            var arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello"));
            await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
