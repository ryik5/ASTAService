//using Alchemy;
//using Alchemy.Classes;
//using Newtonsoft.Json;
using Newtonsoft.Json;
using SuperSocket.ClientEngine;
using System;
using System.Text;
using System.Threading;
using WebSocket4Net;

namespace ASTAWebClient
{


    /// <summary>
    /// Defines the response object to send back to the client
    /// </summary>
    public class Response
    {
        public ResponseType Type { get; set; }
        public dynamic Data { get; set; }
    }
    /// <summary>
    /// Defines the type of response to send back to the client for parsing logic
    /// </summary>
    public enum ResponseType
    {
        Connection = 0,
        Disconnect = 1,
        Message = 2,
        NameChange = 3,
        UserCount = 4,
        ReadyToWork = 254,
        Error = 255
    }
    /// <summary>
    /// Defines a type of command that the client sends to the server
    /// </summary>
    public enum CommandType
    {
        Register = 0,
        NameChange = 1,
        Message = 2,
        DoWork = 254,
        Nope = 255
    }

    public class WebSocketManager
    {
        private AutoResetEvent messageReceiveEvent = new AutoResetEvent(false);
        private AutoResetEvent reRunClientEvent = new AutoResetEvent(false);
        //  private WebSocketClient webClient;
        WebSocket webClient;
        private string webSocketUri { get; set; }
        public Action<string> Status { get; set; }
        public Action<string> Message { get; set; }
        public bool Connected()
        {
            if (webClient != null)
            {
                if (webClient.State != WebSocketState.Open)
                { webClient.Open(); }
                else
                { return true; }

                reRunClientEvent.WaitOne(1000);
            }

            if (webClient.State == WebSocketState.Open)
                return true;
            else
                return false;
        }


        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ConvertMessage(e.Message);

            messageReceiveEvent.Set();
        }

        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            Status("Ошибка открытия сокета...");
            webClient?.Close();
        }

        protected void webSocketClient_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Message(Encoding.UTF8.GetString(e.Data));
            messageReceiveEvent.Set();
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            if (webClient != null)
            {
                Status("Websocket закрыт.");
            }
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            Status("Сокет открыт...");
        }

        public WebSocketManager(string webSocketUri)
        {
            this.webSocketUri = webSocketUri;

            webClient = new WebSocket(webSocketUri);//создаем вебсокет
            webClient.Opened += new EventHandler(websocket_Opened);//событие возникающее в момент открытия
            webClient.Error += new EventHandler<ErrorEventArgs>(websocket_Error); //событие возникающее при ошибке
            webClient.Closed += new EventHandler(websocket_Closed); //событие закрытия
            webClient.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);//получение сообщений
            webClient.DataReceived += new EventHandler<DataReceivedEventArgs>(webSocketClient_DataReceived);
        }


        public void Send(ResponseType type, string data)
        {
            Status($"Отправляю сообщение: '{data}'");

            var r = new Response { Type = type, Data = data };

            dynamic obj = JsonConvert.SerializeObject(r);

            if (webClient != null && webClient.State == WebSocketState.Open)
                webClient.Send(obj);

            if (!messageReceiveEvent.WaitOne(5000))                         // waiting for the response with 5 secs timeout
            {
                Status("Подтверждение от сервера о получении сообщения не получено. Timeout.");
            }
        }

        public void DestroyClient()
        {
            if (webClient != null)
            {
                webClient?.Close();
                webClient?.Dispose();
                webClient = null;
            }
        }


        StringBuilder sb;
        private void ConvertMessage(string text)
        {
            string message;
            Status($"От сервера получено сообщение: {text}");

            try
            {
                dynamic obj = JsonConvert.DeserializeObject(text);
                Message($"Десериализованные данные: {obj}");

                switch ((int)obj.Type)
                {
                    case (int)CommandType.Register:
                        Message($"Обработка кода не написана - Register: {obj?.Data?.Value}");
                        break;
                    case (int)CommandType.Message:
                        message = obj?.Data?.Value;
                        Message($"Полученное сообщение: {message}");
                        if (message.Equals("SendCollectedData"))
                        {
                            Status($"Начать сбор данных...");
                            IMetricsable metrics = new MetricsOperator();
                            sb = metrics.GetMetrics();
                            Send(ResponseType.Message, sb.ToString());
                        }
                        break;
                    case (int)CommandType.NameChange:
                        Message($"Обработка код не написана - NameChange: {obj?.Data?.Value}");
                        break;
                    case (int)CommandType.DoWork:
                        Message($"Обработка код не написана - DoWork: {obj?.Data?.Value}");
                        break;
                    default:
                        Message($"Неизвестный код сообщения: {obj?.Data?.Value}");
                        break;

                }
            }
            catch
            {
                Message($"Полученное сообщение не обработано: {text}");
            }
        }
    }
}