using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Timers;

namespace ASTAService
{
    public partial class AstaServiceLocal : ServiceBase
    {
        private System.Timers.Timer timer = null;
        private Thread workerThread;
        Logger log;


        public AstaServiceLocal()
        {
            InitializeComponent();
        }
        

        protected override void OnStart(string[] args)
        {
            workerThread = new Thread(new ThreadStart( DoWork));
            workerThread.SetApartmentState(ApartmentState.STA);
            //or workerThread = new Thread(new ThreadStart(logger.Start));
            workerThread.Start();
            workerThread.IsBackground = true;
        }

        internal void Start()
        {
            workerThread.Start();
        }

        protected override void OnStop()
        {
            log.Stop();
            workerThread.Abort();
        }

        private  void DoWork()
        {
            timer = new System.Timers.Timer(30000);//создаём объект таймера
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            //form1.Show();
            //System.Windows.Forms.Application.Run(form1);

            log = new Logger();
            log.Start();

            //while (true)
            //{
                
            //    //  MessageBox.Show("Start");
            //    // log.Info("Doing work...");
            //    // do some work, then
            //    Thread.Sleep(1000);
            //}


        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();
            // runProcedure(); //Запускаем процедуру (чего хотим выполнить по таймеру).
            timer.Enabled = true;
            timer.Start();
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

        public ServiceInstallerUtility()
        {
            //InitializeComponent();
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller = new ServiceInstaller();
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "AstaServiceLocal";
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
                //ServiceInstaller ServiceInstallerObj = new ServiceInstaller();
                //InstallContext Context = new InstallContext("<<log file path>>", null);
                //ServiceInstallerObj.Context = Context;
                //ServiceInstallerObj.ServiceName = "AstaServiceLocal";
                //ServiceInstallerObj.Uninstall(null);
            }
            catch { return false; }
            return true;
        }
    }
   
    public class Logger
    {
        FileSystemWatcher watcher;
        object obj = new object();
        bool enabled = true;
        public Logger()
        {
            watcher = new FileSystemWatcher("D:\\Temp");
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
        // переименование файлов
        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string fileEvent = "переименован в " + e.FullPath;
            string filePath = e.OldFullPath;
            RecordEntry(fileEvent, filePath);
        }
        // изменение файлов
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "изменен";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath);
        }
        // создание файлов
        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "создан";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath);
        }
        // удаление файлов
        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "удален";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath);
        }

        private void RecordEntry(string fileEvent, string filePath)
        {
            lock (obj)
            {
                using (StreamWriter writer = new StreamWriter("D:\\templog.txt", true))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss")} файл {filePath} был {fileEvent}");
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
