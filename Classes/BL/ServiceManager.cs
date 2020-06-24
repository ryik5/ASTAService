using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ASTAWebClient
{

    public class ServiceManager : IServiceManageable
    {
        private System.Timers.Timer timer = null;
        private Thread dirWatcherThread = null;
        private Thread webThread = null;
        DirectoryWatchLogger direcoryWatcherlog = null;
         readonly Logger log = new Logger();

        WebSocketManager webSocket;
        static readonly string webSocketUri = "ws://10.0.102.54:5000/path";// "wss://ws.binaryws.com/websockets/v3?app_id=1089";// "ws://localhost:5000";

        public void OnPause()
        {
            throw new NotImplementedException();
        }

        public void OnStart()
        {
            Start();
        }

        public void OnStop()
        {
            StopTimer();
            StopWebsocketClient();
            StopDirWatcher();
        }

        public void AddInfo(string text)
        {
            if (log != null)
                log.WriteString(text);
        }

        private void StartWebsocketClient()
        {
            webSocket = new WebSocketManager(webSocketUri);
            webSocket.EvntInfoMessage += new WebSocketManager.InfoMessage(webSocketClientGotMessage_EvntInfoMessage);
        }
        private void webSocketClientGotMessage_EvntInfoMessage(object sender, TextEventArgs e)
        {
            var json = e.Message;
            AddInfo($"{json}");
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();

            //Запускаем процедуру (чего хотим выполнить по таймеру).
            if (CheckAliveServer())
                SendMessage(ResponseType.ReadyToWork, "254");

            timer.Enabled = true;
            timer.Start();
        }

        private bool CheckAliveServer()
        {
            if (webSocket != null)
            {
                if (webSocket.Connected)
                {
                    return true;
                }
                else
                {
                    Task.Run(() => StopWebsocketClient());
                }
            }
            else
            {
                AddInfo("Создаю подключение....");
                Task.Run(() => StartWebsocketClientThread());
            }
            return false;
        }

        private void SendMessage(ResponseType type, string text)
        {
            webSocket.Send(type, text); //test webSocketServer
            AddInfo($"Отправлен текст: '{text}'");
        }

        internal void Start()
        {
            StartDirWatcherThread();

            StartWebsocketClientThread();

            timer = new System.Timers.Timer(10000);//создаём объект таймера
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = true;
            timer.Start();
            AddInfo("timer is running...");
        }

        private void StartDirWatcherThread()
        {
            dirWatcherThread = new Thread(new ThreadStart(StartDirWatcher));
            dirWatcherThread.SetApartmentState(ApartmentState.STA); //ApartmentState.STA - поток надолго НЕ ЗАНИМАТЬ!
            dirWatcherThread.IsBackground = true;

            dirWatcherThread.Start();
        }
        private void StartDirWatcher()
        {
            direcoryWatcherlog = new DirectoryWatchLogger();
            direcoryWatcherlog.EvntInfoMessage += new DirectoryWatchLogger.InfoMessage(DirectoryWatcher_EvntInfoMessage);
            direcoryWatcherlog.SetDirWatcher(@"d:\temp");

            direcoryWatcherlog.StartWatcher();
        }

        private void DirectoryWatcher_EvntInfoMessage(object sender, TextEventArgs e)
        {
            SendMessage( ResponseType.Message, e.Message);
        }

        private void StopDirWatcher()
        {
            try { direcoryWatcherlog?.StopWatcher(); } catch { }
            try { dirWatcherThread?.Abort(); } catch { }
        }
        private void StartWebsocketClientThread()
        {
            webThread = new Thread(new ThreadStart(StartWebsocketClient));
            webThread.SetApartmentState(ApartmentState.STA);
            webThread.IsBackground = true;

            webThread.Start();
        }
        private void StopWebsocketClient()
        {
            try
            {
                webSocket?.Close();
                webSocket.EvntInfoMessage -= webSocketClientGotMessage_EvntInfoMessage;
                webSocket = null;
            }
            catch { }
        }
        private void StopTimer()
        {
            try
            {
                timer.Enabled = false;
                timer?.Stop();
                timer?.Dispose();
                AddInfo("timer was stoped");
            }
            catch { }
        }
    }
}
