using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;

namespace ASTAService
{
    public partial class AstaServiceLocal : ServiceBase
    {
        private System.Timers.Timer timer = null;
        private Thread workerThread = null;
        FileWatchLogger log=null ;



        public AstaServiceLocal()
        {
            InitializeComponent();

            log = new FileWatchLogger(@"d:\temp"); 

            timer = new System.Timers.Timer(30000);//создаём объект таймера
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }


        protected override void OnStart(string[] args)
        {
            workerThread = new Thread(new ThreadStart(DoWork));
            workerThread.SetApartmentState(ApartmentState.STA);
            //or workerThread = new Thread(new ThreadStart(logger.Start));
            workerThread.Start();
            workerThread.IsBackground = true;
            timer.Enabled = true;
            timer.Start();
        }

        internal void Start()
        {
            if (workerThread == null)
            {
                workerThread = new Thread(new ThreadStart(DoWork));
                workerThread.SetApartmentState(ApartmentState.STA);
                workerThread.IsBackground = true;
            }

            workerThread.Start();
            log.Start();
        }

        protected override void OnStop()
        {
            try
            {
                timer.Enabled = false;
                timer.Stop();
            }
            catch { }

            try { log?.Stop(); } catch { }
            try { workerThread?.Abort(); } catch { }
        }

        public void WriteString(string text)
        {
            log.WriteString(text);
        }

        private void DoWork()
        {

            //form1.Show();
            //System.Windows.Forms.Application.Run(form1);

            log.Start();
            ClientLaunchAsync();
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();
            log.WriteString("Check working Elapsed");
            // runProcedure(); //Запускаем процедуру (чего хотим выполнить по таймеру).
            timer.Enabled = true;
            timer.Start();
        }


        private static async void ClientLaunchAsync()
        {
       // https://archive.codeplex.com/?p=websocket4net //server


            ClientWebSocket webSocket = null;
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri("ws://localhost:5000"), CancellationToken.None);

            // Do something with WebSocket

            var arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello"));
            await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
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
        private static readonly string exePath = Assembly.GetExecutingAssembly().Location;
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;
        private static string serviceName = "AstaServiceLocal";
        public ServiceInstallerUtility()
        {
            //InitializeComponent();
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller = new ServiceInstaller();
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = serviceName;
            serviceInstaller.DisplayName = "ASTA Local Service";
            serviceInstaller.Description = "ASTA (get and send data) as a Local Service";

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        public static bool Install()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] { "/i", exePath });
            }
            catch { return false; }
            return true;
        }

        public static bool Uninstall()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] { "/u", exePath });
            }
            catch { return false; }
            return true;
        }

        public static bool StopService(string serviceName, int timeoutMilliseconds)
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

    public class FileWatchLogger
    {
        FileSystemWatcher watcher;
        readonly object obj = new object();
        bool enabled = true;
        public FileWatchLogger(string pathToDir)
        {
            watcher = new FileSystemWatcher(pathToDir);
            watcher.IncludeSubdirectories = true;
            watcher.Deleted += Watcher_Deleted;
            watcher.Created += Watcher_Created;
            watcher.Changed += Watcher_Changed;
            watcher.Renamed += Watcher_Renamed;
        }

        public void Start()
        {
            watcher.EnableRaisingEvents = true;
            while (enabled)
            {
                Thread.Sleep(1000);
            }
        }
        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
            enabled = false;
        }
        public void WriteString(string text)
        {
            RecordEntry("WriteString", text , WatcherChangeTypes.Created);
        }
        // переименование файлов
        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string fileEvent = "переименован в " + e.FullPath;
            string filePath = e.OldFullPath;
            RecordEntry(fileEvent, filePath, e.ChangeType);
        }
        // изменение файлов
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "изменен";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath, e.ChangeType);
        }
        // создание файлов
        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "создан";
            string filePath = e.FullPath;
            
            RecordEntry(fileEvent, filePath, e.ChangeType);
        }
        // удаление файлов
        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "удален";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath, e.ChangeType);
        }

        private void RecordEntry(string fileEvent, string filePath, WatcherChangeTypes typo)
        {
            //
            string path = Assembly.GetExecutingAssembly().Location;
            string pathToLog =Path.Combine( Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)+".log");
            lock (obj)
            {
                using (StreamWriter writer = new StreamWriter(pathToLog, true))//"D:\\templog.txt"
                {
                    writer.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss")}|{typo}|{filePath} был {fileEvent}");
                    writer.Flush();
                }
            }
        }
    }

    /*      
using System.Timers;
namespace MyService
{
    {
    public partial class MyService: ServiceBase
    {
    private System.Timers.Timer timer = null;

    public MyService()
    {
        InitializeComponent();
        timer = new System.Timers.Timer(30000);//создаём объект таймера
        timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
    }

    void timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        //собственно здесь пишем код таймера
    }
    protected override void OnStart(string[] args)
    {
        timer.Start();
    }

    protected override void OnStop()
    {
        timer.Stop();
    }
} 
*/
}
