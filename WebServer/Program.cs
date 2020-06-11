using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace WebServer
{
    class Program
    {
        static WebSocketManager websocket;
        static string webSocketUri;

        static void Main(string[] args)
        {
            webSocketUri = "wss://ws.binaryws.com/websockets/v3?app_id=1089";//"ws://localhost:5000/";
            ServerLaunchAsync();

            websocket.Send("{\"time\": 1}");
            websocket.Send("{\"ping\": 1}");
            websocket.Close();

            Console.ReadLine();
            Console.ReadKey();
            websocket.Close();
            Console.WriteLine("Finish");
        }

        private static void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Websocket_MessageReceived");
        }

        private static void Websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine("Websocket_Error");
        }

        private static void websocket_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("websocket_Closed");
        }


        private static void websocket_Opened(object sender, EventArgs e)
        {
            websocket.Send("Hello World!");
        }

        private static void ServerLaunchAsync()
        {
            websocket = new WebSocketManager(webSocketUri);

            Console.WriteLine("Websocket server is waiting...");
            
        }

        //private static async void ServerLaunchAsync()
        //{
        //    var httpListener = new HttpListener();
        //    httpListener.Prefixes.Add("http://localhost:5000/");
        //    httpListener.Start();
        //    HttpListenerContext listenerContext = await httpListener.GetContextAsync();
        //    if (listenerContext.Request.IsWebSocketRequest)
        //    {
        //        WebSocketContext webSocketContext = await listenerContext.AcceptWebSocketAsync(null);
        //        WebSocket webSocket = webSocketContext.WebSocket;

        //        // Do something with WebSocket
        //        var buffer = new byte[1000];
        //        var segment = new ArraySegment<byte>(buffer);
        //        var result = await webSocket.ReceiveAsync(segment, CancellationToken.None);
        //        Console.WriteLine(Encoding.UTF8.GetString(segment.Array));
        //    }
        //    else
        //    {
        //        listenerContext.Response.StatusCode = 426;
        //        listenerContext.Response.Close();
        //    }
        //}
    }

    /// <summary>
    /// Only Client!!!
    /// </summary>
    public class WebSocketManager
    {
        private AutoResetEvent messageReceiveEvent = new AutoResetEvent(false);
        private string lastMessageReceived;
        private WebSocket webSocket;

        public WebSocketManager(string webSocketUri)
        {
            Console.WriteLine("Initializing websocket. Uri: " + webSocketUri);
            webSocket = new WebSocket(webSocketUri);
            webSocket.Opened += new EventHandler(websocket_Opened);
            webSocket.Closed += new EventHandler(websocket_Closed);
            webSocket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);

            webSocket.Open();
            while (webSocket.State == WebSocketState.Connecting) { };   // by default webSocket4Net has AutoSendPing=true, 
                                                                        // so we need to wait until connection established
            if (webSocket.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not opened.");
            }
        }

        public string Send(string data)
        {
            Console.WriteLine("Client wants to send data:");
            Console.WriteLine(data);

            webSocket.Send(data);
            if (!messageReceiveEvent.WaitOne(5000))                         // waiting for the response with 5 secs timeout
                Console.WriteLine("Cannot receive the response. Timeout.");

            return lastMessageReceived;
        }

        public void Close()
        {
            Console.WriteLine("Closing websocket...");
            webSocket.Close();
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("Websocket is opened.");
        }
        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception.Message);
        }
        private void websocket_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("Websocket is closed.");
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Message received: " + e.Message);
            lastMessageReceived = e.Message;
            messageReceiveEvent.Set();
        }
    }

}

