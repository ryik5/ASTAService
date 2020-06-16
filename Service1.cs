using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;

namespace ASTAService
{
    public partial class AstaServiceLocal : ServiceBase
    {
        private System.Timers.Timer timer = null;
        private Thread dirWatcherThread = null;
        private Thread webThread = null;
        DirectoryWatchLogger direcoryWatcherlog = null;
        static readonly Logger log = new Logger();

        WebSocketClient client;

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
            RunDirWatcher();

            RunWebsocketClient();

            timer = new System.Timers.Timer(10000);//создаём объект таймера
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = true;
            timer.Start();
        }

        private void RunDirWatcher()
        {
            dirWatcherThread = new Thread(new ThreadStart(InitDirWatcher));
            dirWatcherThread.SetApartmentState(ApartmentState.STA); //ApartmentState.STA - поток надолго НЕ ЗАНИМАТЬ!
            dirWatcherThread.IsBackground = true;

            dirWatcherThread.Start();
        }
        private void InitDirWatcher()
        {
            direcoryWatcherlog = new DirectoryWatchLogger();
            direcoryWatcherlog.EvntInfoMessage += new DirectoryWatchLogger.InfoMessage(WebSocketSend_EvntInfoMessage);
            direcoryWatcherlog.SetDirWatcher(@"d:\temp");

            direcoryWatcherlog.StartWatcher();
        }

        private void WebSocketSend_EvntInfoMessage(object sender, TextEventArgs e)
        {
            SendMessage(e.Message);
        }

        private void StopDirWatcher()
        {
            try { direcoryWatcherlog?.StopWatcher(); } catch { }
            try { dirWatcherThread?.Abort(); } catch { }
        }
        private void RunWebsocketClient()
        {
            webThread = new Thread(new ThreadStart(StartWebSocket));
            webThread.SetApartmentState(ApartmentState.STA);
            webThread.IsBackground = true;

            webThread.Start();
        }
        private void StopWebsocketClient()
        {
            try
            {
                client.Disconnect();
                client = null;
            }
            catch { }
        }
        protected override void OnStop()
        {
            try
            {
                timer.Enabled = false;
                timer.Stop();
            }
            catch { }

            StopWebsocketClient();
            StopDirWatcher();
        }
        public void WriteString(string text)
        {
            if (log != null)
                log.WriteString(text);
        }

        private void StartWebSocket()
        {
            client = new WebSocketClient(webSocketUri)
            {
              //  Origin = "localhost",
                OnReceive = OnClientReceive
            };
            ConnectToServer();
        }
        private void ConnectToServer()
        {
            if (client!=null&&!client.Connected)
            { client.Connect(); }
        }
        private void OnClientReceive(UserContext context)
        {
            var json = context.DataFrame.ToString();
            WriteString($"От: \"{context.ClientAddress}\" получены \"сырые\" данные: {json}");

            Response r = null;
            dynamic obj = null;
            try
            {
                // <3 dynamics
                obj = JsonConvert.DeserializeObject(json);

                WriteString($"Десериализованные данные: {obj}");
            }
            catch (Exception e) // Bad JSON! For shame.
            {
                r = new Response { Type = ResponseType.Message, Data = $" Сейчас {DateTime.Now.ToString("yyyy-MM-dd hh:MM:ss")} и ты спросил {json}{Environment.NewLine} это ошибка: {e.Message}" };
            }

            if (obj != null)
            {
                switch ((int)obj.Type)
                {

                    case (int)CommandType.Message:
                        r = new Response { Type = ResponseType.Message, Data = $"Вы отправили {obj?.Data}" };
                        WriteString($"Получено сообщение: {obj?.Data?.Value}");
                        //try { ChatMessage(obj.Data.Value, context); }
                        //catch (Exception err) { WriteString($"Ошибка ChatMessage: {err.Message}"); }
                        break;
                }
            }
            //context.Send(JsonConvert.SerializeObject(r));
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendMessage("Ping");            
        }

        private void SendMessage(string text)
        {
            WriteString($"Служба '{nameof(AstaServiceLocal)}' активная...");
            timer.Enabled = false;
            timer.Stop();

            //Запускаем процедуру (чего хотим выполнить по таймеру).
            if (client != null)
            {
                if (client.Connected)
                {
                    client.Send(text); //test webSocketServer
                }
                else
                {
                    Task.Run(() => ConnectToServer());
                }
            }
            else
            {
                WriteString("К серверу не подключен.");
                Task.Run(() => RunWebsocketClient());
            }

            timer.Enabled = true;
            timer.Start();
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
        /// Holds the name and context instance for an online user
        /// </summary>
        public class User
        {
            public string Name = String.Empty;
            public UserContext Context { get; set; }
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
        public static readonly string serviceName = "ASTAWebClient";
        public static readonly string serviceDisplayName = "ASTA Web Client";
        public static readonly string serviceDescription = "ASTA websocket client monitoring windows service";
        private static int timeoutMilliseconds = 2000;
        public ServiceInstallerUtility()
        {
            //InitializeComponent();
            processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,
                //DelayedAutoStart = true,
                ServiceName = serviceName,
                DisplayName = serviceDisplayName,
                Description = serviceDescription
            };
            serviceInstaller.AfterInstall += new InstallEventHandler(ServiceInstaller_AfterInstall);

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        private void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
                {
                    sc.Start();
                }
            }catch(Exception err)
            {

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
            string message = null;
            lock (obj)
            {
                message = $"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{typo}|{filePath}";

                EvntInfoMessage?.Invoke(this, new TextEventArgs(message));

                try
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                    {
                        writer.WriteLine(message);
                        writer.Flush();
                    }
                }
                catch(Exception err) { EvntInfoMessage?.Invoke(this, new TextEventArgs($"Ошибка записи '{err.Message}' лога в файл '{pathToLog}'")); }
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
                try
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                    {
                        writer.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{eventText}|{text}");
                        writer.Flush();
                    }
                }catch { }
            }
        }
    }
}