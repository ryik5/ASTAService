using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Alchemy;
using Alchemy.Classes;

namespace ASTAService
{
    public partial class AstaServiceLocal : ServiceBase
    {
        private System.Timers.Timer timer = null;
        private Thread dirWatcherThread = null;
        private Thread webThread = null;
        DirectoryWatchLogger direcoryWatcherlog = null;
        static readonly Logger log = new Logger();

        WebSocketManager webSocket;
        static readonly string webSocketUri = "ws://10.0.102.54:5000/path";// "wss://ws.binaryws.com/websockets/v3?app_id=1089";// "ws://localhost:5000";

        public AstaServiceLocal()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            Start();
        }

        internal void Start()
        {
            StartDirWatcherThread();

            StartWebsocketClientThread();

            timer = new System.Timers.Timer(10000);//создаём объект таймера
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = true;
            timer.Start();
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
            SendMessage(e.Message);
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
                timer.Stop();
                timer.Dispose();
            }
            catch { }
        }

        protected override void OnStop()
        {
            StopTimer();
            StopWebsocketClient();
            StopDirWatcher();
        }

        public void LogText(string text)
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
            LogText($"{json}");
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();

            //Запускаем процедуру (чего хотим выполнить по таймеру).
            SendMessage("Пинг");

            timer.Enabled = true;
            timer.Start();
        }

        private void SendMessage(string text)
        {
            if (webSocket != null)
            {
                if (webSocket.Connected)
                {
                    webSocket.Send(text); //test webSocketServer
                    LogText($"Отправлен текст: '{text}'");
                }
                else
                {
                    Task.Run(() => StopWebsocketClient());
                }
            }
            else
            {
                LogText("Создаю подключение....");
                Task.Run(() => StartWebsocketClientThread());
            }
        }
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
        Error = 255
    }

    /// <summary>
    /// Defines the response object to send back to the client
    /// </summary>
    public class Response
    {
        public ResponseType Type { get; set; }
        public dynamic Data { get; set; }
    }

    /// <summary>
    /// Defines a type of command that the client sends to the server
    /// </summary>
    public enum CommandType
    {
        Register = 0,
        NameChange,
        Message
    }

    /// <summary>
    /// Утилита саморегистрации
    /// </summary>
    [RunInstaller(true)]
    public partial class ServiceInstallerUtility : Installer
    {
        //https://www.c-sharpcorner.com/article/installing-a-service-programmatically/
        //https://www.csharp-examples.net/install-net-service/
        //https://stackoverflow.com/questions/12201365/programmatically-remove-a-service-using-c-sharp

        //  private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static ServiceInstaller serviceInstaller;
        readonly ServiceProcessInstaller processInstaller;

        public static readonly string serviceExePath = Assembly.GetExecutingAssembly().Location;
        public static readonly string serviceName = "AstaServiceLocal";
        public static readonly string serviceDisplayName = "ASTA Web Client";
        public static readonly string serviceDescription = "ASTA websocket client as a collected events windows service";
        private static int timeoutMilliseconds = 2000;
        public ServiceInstallerUtility()
        {
            //InitializeComponent();
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller = new ServiceInstaller();
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = serviceName;
            serviceInstaller.AfterInstall += new InstallEventHandler(ServiceInstaller_AfterInstall);
            //           serviceInstaller.DelayedAutoStart = true;
            serviceInstaller.DisplayName = serviceDisplayName;
            serviceInstaller.Description = serviceDescription;

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
            {
                sc.Start();
            }
        }

        public static bool Install()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] { "/i", serviceExePath });
            }
            catch { return false; }
            return true;
        }

        public static bool Uninstall()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] { "/u", serviceExePath });
            }
            catch { return false; }
            return true;
        }

        public static bool StopService()
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }


    public class WindowsServiceClass
    {
        #region SERVICE_ACCESS
        [Flags]
        public enum SERVICE_ACCESS : uint
        {
            STANDARD_RIGHTS_REQUIRED = 0xF0000,
            SERVICE_QUERY_CONFIG = 0x00001,
            SERVICE_CHANGE_CONFIG = 0x00002,
            SERVICE_QUERY_STATUS = 0x00004,
            SERVICE_ENUMERATE_DEPENDENTS = 0x00008,
            SERVICE_START = 0x00010,
            SERVICE_STOP = 0x00020,
            SERVICE_PAUSE_CONTINUE = 0x00040,
            SERVICE_INTERROGATE = 0x00080,
            SERVICE_USER_DEFINED_CONTROL = 0x00100,
            SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
                SERVICE_QUERY_CONFIG |
                SERVICE_CHANGE_CONFIG |
                SERVICE_QUERY_STATUS |
                SERVICE_ENUMERATE_DEPENDENTS |
                SERVICE_START |
                SERVICE_STOP |
                SERVICE_PAUSE_CONTINUE |
                SERVICE_INTERROGATE |
                SERVICE_USER_DEFINED_CONTROL)
        }
        #endregion
        #region SCM_ACCESS
        [Flags]
        public enum SCM_ACCESS : uint
        {
            STANDARD_RIGHTS_REQUIRED = 0xF0000,
            SC_MANAGER_CONNECT = 0x00001,
            SC_MANAGER_CREATE_SERVICE = 0x00002,
            SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
            SC_MANAGER_LOCK = 0x00008,
            SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
            SC_MANAGER_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
                SC_MANAGER_CONNECT |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_LOCK |
                SC_MANAGER_QUERY_LOCK_STATUS |
                SC_MANAGER_MODIFY_BOOT_CONFIG
        }
        #endregion

        #region DeleteService
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteService(IntPtr hService);
        #endregion
        #region OpenService
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, SERVICE_ACCESS dwDesiredAccess);
        #endregion
        #region OpenSCManager
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr OpenSCManager(string machineName, string databaseName, SCM_ACCESS dwDesiredAccess);
        #endregion
        #region CloseServiceHandle
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseServiceHandle(IntPtr hSCObject);
        #endregion

        public delegate void InfoMessage(object sender, TextEventArgs e);
        public event InfoMessage EvntInfoMessage;

        public void Uninstall(string serviceName)
        {
            try
            {
                IntPtr schSCManager = OpenSCManager(null, null, SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
                if (schSCManager != IntPtr.Zero)
                {
                    IntPtr schService = OpenService(schSCManager, serviceName, SERVICE_ACCESS.SERVICE_ALL_ACCESS);
                    if (schService != IntPtr.Zero)
                    {
                        if (DeleteService(schService) == false)
                        {
                            EvntInfoMessage?.Invoke(this, new TextEventArgs($"DeleteService failed {Marshal.GetLastWin32Error()}"));

                            //System.Windows.Forms.MessageBox.Show(
                            //    string.Format("DeleteService failed {0}", Marshal.GetLastWin32Error()));
                        }
                    }
                    CloseServiceHandle(schSCManager);
                    // if you don't close this handle, Services control panel
                    // shows the service as "disabled", and you'll get 1072 errors
                    // trying to reuse this service's name
                    CloseServiceHandle(schService);

                }
            }
            catch (System.Exception ex)
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs(ex.Message));
                //System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }
    }


    public class DirectoryWatchLogger
    {
        System.IO.FileSystemWatcher watcher;
        readonly object obj = new object();
        bool enabled = true;
        public delegate void InfoMessage(object sender, TextEventArgs e);
        public event InfoMessage EvntInfoMessage;


        public DirectoryWatchLogger() { }
        public DirectoryWatchLogger(string pathToDir)
        {
            SetDirWatcher(pathToDir);
        }

        public void SetDirWatcher(string pathToDir)
        {
            watcher = new System.IO.FileSystemWatcher(pathToDir);
            watcher.IncludeSubdirectories = true;
            watcher.Deleted += Watcher_Deleted;
            watcher.Created += Watcher_Created;
            watcher.Changed += Watcher_Changed;
            watcher.Renamed += Watcher_Renamed;
        }

        public void StartWatcher()
        {
            watcher.EnableRaisingEvents = true;
            while (enabled)
            {
                Thread.Sleep(1000);
            }
        }
        public void StopWatcher()
        {
            watcher.EnableRaisingEvents = false;
            enabled = false;
        }

        // переименование файлов
        private void Watcher_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            string fileEvent = "переименован в " + e.FullPath;
            string filePath = e.OldFullPath;
            RecordEntry(fileEvent, filePath, e.ChangeType);
        }
        // изменение файлов
        private void Watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            string fileEvent = "изменен";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath, e.ChangeType);
        }
        // создание файлов
        private void Watcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            string fileEvent = "создан";
            string filePath = e.FullPath;

            RecordEntry(fileEvent, filePath, e.ChangeType);
        }
        // удаление файлов
        private void Watcher_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            string fileEvent = "удален";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath, e.ChangeType);
        }

        private void RecordEntry(string fileEvent, string filePath, System.IO.WatcherChangeTypes typo)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            string pathToLog = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".log");

            lock (obj)
            {
              string  message = $"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{typo}|{filePath}";

                EvntInfoMessage?.Invoke(this, new TextEventArgs(message));

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
        }
    }


    public class Logger
    {
        readonly object obj = new object();

        public Logger() { }

        public void WriteString(string text)
        {
            RecordEntry("Message", text);
        }
        private void RecordEntry(string eventText, string text)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            string pathToLog = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".log");
            lock (obj)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{eventText}|{text}");
                    writer.Flush();
                }
            }
        }
    }


    public class WebSocketManager
    {
        private AutoResetEvent messageReceiveEvent = new AutoResetEvent(false);
        private WebSocketClient webClient;
        public delegate void InfoMessage(object sender, TextEventArgs e);
        public event InfoMessage EvntInfoMessage;
        public bool Connected
        {
            get
            {
                if (webClient != null)
                    return webClient.Connected;
                else 
                    return false;
            }
        }
        public WebSocketManager(string webSocketUri)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Инициализация websocket client с Uri: " + webSocketUri));

            webClient = new WebSocketClient(webSocketUri)
            { 
                OnReceive = OnClientReceive , 
                ConnectTimeout=TimeSpan.FromMilliseconds(5000),
                 OnDisconnect= OnClientDisconnect
            };
            
            webClient.Connect();

            if (webClient.Connected && webClient.ReadyState==  WebSocketClient.ReadyStates.OPEN)
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Подключение не установлено"));
            }
            else
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Подключение установлено"));
            }
        }

        private void OnClientDisconnect(UserContext context)
        {
            context.Send("Disconnecting");
            webClient?.Disconnect();
        }

        private void OnClientReceive(UserContext context)
        {
            var data = context.DataFrame.ToString();

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
                        EvntInfoMessage?.Invoke(this, new TextEventArgs($"Полученное сообщение: {obj?.Data?.Value}"));
                        break;
                    case (int)CommandType.NameChange:
                        EvntInfoMessage?.Invoke(this, new TextEventArgs($"Обработка код не написана - NameChange: {obj?.Data?.Value}"));
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

        public void Send(string data)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs($"Отправляю сообщение: '{data}'"));

            var r = new Response { Type = ResponseType.Message, Data = data };

            dynamic obj = JsonConvert.SerializeObject(r);

            if(webClient.Connected)
            webClient.Send(obj);

            if (!messageReceiveEvent.WaitOne(7000))                         // waiting for the response with 5 secs timeout
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Подтверждение от сервера о получении сообщения не получено. Timeout."));
            }
        }

        public void Close()
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Закрываю websocket..."));
            webClient.Disconnect();
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
            Error = 255
        }

        /// <summary>
        /// Defines the response object to send back to the client
        /// </summary>
        public class Response
        {
            public ResponseType Type { get; set; }
            public dynamic Data { get; set; }
        }

        public enum CommandType
        {
            Register = 0,
            NameChange,
            Message
        }
    }
}