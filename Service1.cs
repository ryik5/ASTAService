using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WebSocket4Net;

namespace ASTAService
{
    public partial class AstaServiceLocal : ServiceBase
    {
        private System.Timers.Timer timer = null;
        private Thread workerThread = null;
        private Thread webThread = null;
        DirectoryWatchLogger direcoryWatcherlog = null;
        Logger log = null;

        WebSocketManager webSocket;
        static string webSocketUri;


        public AstaServiceLocal()
        {
            InitializeComponent();
            log = new Logger();
        }


        protected override void OnStart(string[] args)
        {
            Start();
        }

        internal void Start()
        {
            if (workerThread == null)
            {
                workerThread = new Thread(new ThreadStart(StartLog));
                workerThread.SetApartmentState(ApartmentState.STA); //ApartmentState.STA - поток надолго НЕ ЗАНИМАТЬ!
                workerThread.IsBackground = true;
            }

            if (webThread == null)
            {
                webThread = new Thread(new ThreadStart(StartWebSocket));
                webThread.SetApartmentState(ApartmentState.STA);
                webThread.IsBackground = true;
            }

            workerThread.Start();
            webThread.Start();

            timer = new System.Timers.Timer(10000);//создаём объект таймера
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = true;
            timer.Start();
        }

        protected override void OnStop()
        {
            try
            {
                timer.Enabled = false;
                timer.Stop();
            }
            catch { }
            try
            {
                webSocket.Close();
                webSocket.EvntInfoMessage -= WebSocket_EvntInfoMessage;
            }
            catch { }

            try { direcoryWatcherlog?.StopWatcher(); } catch { }
            try { workerThread?.Abort(); } catch { }
        }

        public void WriteString(string text)
        {
            if(log != null)
                log.WriteString(text);
        }

        private void StartLog()
        {
            direcoryWatcherlog = new DirectoryWatchLogger();
            direcoryWatcherlog.SetDirWatcher(@"d:\temp");

            direcoryWatcherlog.StartWatcher();
        }

        private void StartWebSocket()
        {
            webSocketUri = "wss://ws.binaryws.com/websockets/v3?app_id=1089";// "ws://localhost:5000";

            webSocket = new WebSocketManager(webSocketUri);
            webSocket.EvntInfoMessage += new WebSocketManager.InfoMessage(WebSocket_EvntInfoMessage);
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();
            log.WriteString("Check working Elapsed");
            
            //Запускаем процедуру (чего хотим выполнить по таймеру).
            webSocket.Send("{\"time\": 1}"); //test webSocketServer
            webSocket.Send("{\"ping\": 1}");

            //  Task.Run(()=>  ClientSendAsync( "Hello"));

            timer.Enabled = true;
            timer.Start();
        }
        
        private void WebSocket_EvntInfoMessage(object sender, TextEventArgs e)
        {
            log.WriteString(e.Message);
        }
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
        public static readonly string serviceDisplayName = "ASTA Local Service";
        public static readonly string serviceDescription = "ASTA (get and send data) as a Local Service";
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
                            System.Windows.Forms.MessageBox.Show(
                                string.Format("DeleteService failed {0}", Marshal.GetLastWin32Error()));
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
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }
    }


    public class DirectoryWatchLogger
    {
        System.IO.FileSystemWatcher watcher;
        readonly object obj = new object();
        bool enabled = true;

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
        public void WriteString(string text)
        {
            RecordEntry("Message", text);
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
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{typo}|{filePath}");
                    writer.Flush();
                }
            }
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


    public class Logger
    {
        System.IO.FileSystemWatcher watcher;
        readonly object obj = new object();
        bool enabled = true;

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
        private string lastMessageReceived;
        private WebSocket webSocket;
        public delegate void InfoMessage(object sender, TextEventArgs e);
        public event InfoMessage EvntInfoMessage;

        public WebSocketManager(string webSocketUri)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Initializing websocket. Uri: " + webSocketUri));

            webSocket = new WebSocket(webSocketUri);
            webSocket.Opened += new EventHandler(websocket_Opened);
            webSocket.Closed += new EventHandler(websocket_Closed);
            webSocket.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(websocket_Error);
            webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);

            webSocket.Open();
            while (webSocket.State == WebSocketState.Connecting) { };   // by default webSocket4Net has AutoSendPing=true, 
                                                                        // so we need to wait until connection established
            if (webSocket.State != WebSocketState.Open)
            {
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Connection is not opened."));
                //  throw new Exception("Connection is not opened.");
            }
        }

        public string Send(string data)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Client wants to send data:" + data));

            webSocket.Send(data);
            if (!messageReceiveEvent.WaitOne(5000))                         // waiting for the response with 5 secs timeout
                EvntInfoMessage?.Invoke(this, new TextEventArgs("Cannot receive the response. Timeout."));

            return lastMessageReceived;
        }

        public void Close()
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Closing websocket..."));
            webSocket.Close();
        }
        private void websocket_Opened(object sender, EventArgs e)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Websocket is opened."));
        }
        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs(e.Exception.Message));
        }
        private void websocket_Closed(object sender, EventArgs e)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Websocket is closed."));
        }
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            EvntInfoMessage?.Invoke(this, new TextEventArgs("Message received: " + e.Message));
            lastMessageReceived = e.Message;
            messageReceiveEvent.Set();
        }
    }

}
