﻿using System;
using System.Threading;

namespace ASTAWebClient
{
    public class ServiceManager : IServiceManageable
    {
        public void OnPause()
        {
            throw new NotImplementedException();
        }

        public void OnStart()
        {
            StartDirWatcherThread();
            StartWebsocketClientThread();
            StartTimer();
        }

        public void OnStop()
        {
            StopTimer();
            StopWebsocket();
            StopDirWatcher();
        }


        #region Logger
        readonly Logger log = new Logger();
        public void AddInfo(string text)
        {
            if (log != null)
                log.WriteString(text);
        }
        #endregion


        #region Timer
        System.Timers.Timer timer = null;
        private void StartTimer()
        {
            timer = new System.Timers.Timer(10000);//создаём объект таймера
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            timer.Enabled = true;
            timer.Start();
            AddInfo("timer is running...");
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();

            //Запускаем процедуру (чего хотим выполнить по таймеру).
            if (CheckAliveServer())
            {
                SendMessage(ResponseType.ReadyToWork, "254");
            }

            timer.Enabled = true;
            timer.Start();
        }
        #endregion


        #region Directory watcher
        DirectoryWatchLogger direcoryWatcherlog = null;
        Thread dirWatcherThread = null;
        private void StartDirWatcher()
        {
            direcoryWatcherlog = new DirectoryWatchLogger();
            direcoryWatcherlog.EvntInfoMessage += new DirectoryWatchLogger.InfoMessage(DirWatcher_EvntInfoMessage);
            direcoryWatcherlog.SetDirWatcher(@"d:\temp");

            direcoryWatcherlog.StartWatcher();
        }
        private void StartDirWatcherThread()
        {
            dirWatcherThread = new Thread(new ThreadStart(StartDirWatcher));
            dirWatcherThread.SetApartmentState(ApartmentState.STA); //ApartmentState.STA - поток надолго НЕ ЗАНИМАТЬ!
            dirWatcherThread.IsBackground = true;

            dirWatcherThread.Start();
        }
        private void DirWatcher_EvntInfoMessage(object sender, TextEventArgs e)
        {
            SendMessage(ResponseType.Message, e.Message);
        }
        #endregion


        #region WebSocket Client
        private void StartWebsocketClientThread()
        {
            if (webThread != null || webSocket != null)
            {
                StopWebsocket();
                return;
            }

            webThread = new Thread(new ThreadStart(StartWebsocketClient));
            webThread.SetApartmentState(ApartmentState.STA);
            webThread.IsBackground = true;

            webThread.Start();
        }

        readonly string webSocketUri = "ws://10.0.102.54:5000/path";//"ws://172.17.1167.10:5000/path";// "wss://ws.binaryws.com/websockets/v3?app_id=1089";// "ws://localhost:5000";
        WebSocketManager webSocket;
        Thread webThread = null;
        private void StartWebsocketClient()
        {
            webSocket = new WebSocketManager(webSocketUri);

            webSocket.Message = WebsocketClient_EvntMessage;
            webSocket.Status = AddInfo;

            webSocket.Connected();
        }

        private void WebsocketClient_EvntMessage(string e)
        {
            var json = e;
            AddInfo($"Получена комманда: {json}");
        }

        private bool CheckAliveServer()
        {
            if (webSocket != null)
            {
                if (webSocket.Connected())
                { return true; }
                else
                { return false; }
            }
            else
            {
                AddInfo("Создаю подключение....");
                StartWebsocketClientThread();
            }
            return false;
        }
        private void SendMessage(ResponseType type, string text)
        {
            webSocket.Send(type, text); //test webSocketServer
            AddInfo($"Отправлен текст: '{text}'");
        }
        #endregion


        #region Stop running jobs
        private void StopDirWatcher()
        {
            try { direcoryWatcherlog?.StopWatcher(); } catch { }
            try { dirWatcherThread?.Abort(); } catch { }
        }
        private void StopWebsocket()
        {
            AddInfo("Останавливаю клиента....");
            try
            {
                webSocket?.DestroyClient();
                webSocket.Message = null;
                webSocket = null;
            }
            catch { }
            try
            {
                if (webThread.IsAlive && webThread.ThreadState == ThreadState.Running)
                {
                    webThread.Abort();
                }
                webThread = null;
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
        #endregion
    
    }
}