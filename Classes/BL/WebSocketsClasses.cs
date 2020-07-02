using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;

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
        private WebSocketClient webClient;
        private string webSocketUri { get; set; }
        public delegate void InfoMessage(object sender, TextEventArgs e);
        public event InfoMessage EvntInfoMessage;
        public bool Connected()
        {
            if (webClient != null && webClient.Connected && webClient.ReadyState == WebSocketClient.ReadyStates.OPEN)
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Клиент подключен к серверу..."));
            }
            else
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Подключение еще не было установлено..."));

                Connect(webSocketUri);
            }

            try
            {
                if (webClient != null && webClient.Connected&& webClient.ReadyState == WebSocketClient.ReadyStates.OPEN)
                    return true;
                else 
                    return false;
            }
            catch { return false; }
        }

        public WebSocketManager(string webSocketUri)
        {
            this.webSocketUri = webSocketUri;
         }

        
        private void Connect(string webSocketUr)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Инициализация websocket client с Uri: " + webSocketUr));

            try
            {
                webClient = new WebSocketClient(webSocketUr)
                {                 
                    ConnectTimeout = TimeSpan.FromMilliseconds(3000)                  
                };

                webClient.Connect();
                Thread.Sleep(1000);
                if (webClient!=null&& webClient.Connected)
                {
                    webClient.OnReceive = OnClientReceive;
                    webClient.OnDisconnect = OnClientDisconnect;
                }
                else
                {
                    webClient = null;
                }
            }
            catch
            {
                webClient = null;
            }
        }

        private void OnClientDisconnect(UserContext context)
        {
            if (webClient != null && webClient.Connected)
            {
                context.Send("Disconnecting");
                webClient?.Disconnect();
            }
        }

        StringBuilder sb;
        private void OnClientReceive(UserContext context)
        {
            var data = context.DataFrame.ToString();
            string message;
            EvntInfoMessage?.Invoke(this, new TextEventArgs($"От сервера получено сообщение: {data}"));

            try
            {
                dynamic obj = JsonConvert.DeserializeObject(data);
                EvntInfoMessage?.Invoke(this, new TextEventArgs($"Десериализованные данные: {obj}"));

                switch ((int)obj.Type)
                {
                    case (int)CommandType.Register:
                        EvntInfoMessage?.Invoke(this, new TextEventArgs($"Обработка кода не написана - Register: {obj?.Data?.Value}"));
                        break;
                    case (int)CommandType.Message:
                        message = obj?.Data?.Value;
                        EvntInfoMessage?.Invoke(this, new TextEventArgs($"Полученное сообщение: {message}"));
                        if (message.Equals("CollectData"))
                        {
                            EvntInfoMessage?.Invoke(this, new TextEventArgs($"Начать сбор данных..."));
                            IMetricsable metrics = new MetricsOperator();
                            sb = metrics.GetMetrics();
                            Send(ResponseType.Message, sb.ToString());
                        }
                        break;
                    case (int)CommandType.NameChange:
                        EvntInfoMessage?.Invoke(this, new TextEventArgs($"Обработка код не написана - NameChange: {obj?.Data?.Value}"));
                        break;
                    case (int)CommandType.DoWork:
                        EvntInfoMessage?.Invoke(this, new TextEventArgs($"Обработка код не написана - DoWork: {obj?.Data?.Value}"));
                        break;
                    default:
                        EvntInfoMessage?.Invoke(this, new TextEventArgs($"Неизвестный код сообщения: {obj?.Data?.Value}"));
                        break;

                }
            }
            catch (Exception err)
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs($"Полученное сообщение не распознано: {err.Message}"));
            }
            //context.Send(data);
        }

        public void Send(ResponseType type, string data)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs($"Отправляю сообщение: '{data}'"));

            var r = new Response { Type = type, Data = data };

            dynamic obj = JsonConvert.SerializeObject(r);

            if (webClient != null && webClient.Connected)
                webClient.Send(obj);

            if (!messageReceiveEvent.WaitOne(7000))                         // waiting for the response with 5 secs timeout
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Подтверждение от сервера о получении сообщения не получено. Timeout."));
            }
        }

        public void Close()
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Закрываю websocket..."));
            if (webClient != null && webClient.Connected)
                webClient.Disconnect();
        }
    }
}
