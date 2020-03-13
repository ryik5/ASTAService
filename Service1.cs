using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASTAService
{
    public partial class AstaServiceLocal : ServiceBase
    {
        private readonly Thread workerThread;
        //or Logger logger;
        public AstaServiceLocal()
        {
            InitializeComponent();
            workerThread = new Thread(DoWork);
            workerThread.SetApartmentState(ApartmentState.STA);
            //or workerThread = new Thread(new ThreadStart(logger.Start));
        }

        protected override void OnStart(string[] args)
        {
            workerThread.Start();
        }

        internal void Start()
        {
            workerThread.Start();
        }

        protected override void OnStop()
        {
            workerThread.Abort();
        }

        private static void DoWork()
        {
            while (true)
            {
              //  MessageBox.Show("Start");
                // log.Info("Doing work...");
                // do some work, then
                Thread.Sleep(5000);
            }
        }
    }

        /// <summary>
        /// Утилита саморегистрации
        /// </summary>
    [RunInstaller(true)]
    public partial class ServiceInstallerUtility : Installer
    {
      //  private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static readonly string exePath = Assembly.GetExecutingAssembly().Location;
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;

        public ServiceInstallerUtility()
        {
           // InitializeComponent();
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller = new ServiceInstaller();
            serviceInstaller.StartType = ServiceStartMode.Manual;           
            serviceInstaller.ServiceName = "AstaServiceLocal";
            serviceInstaller.DisplayName = "ASTA Local Service";
            serviceInstaller.Description = "ASTA (get and send data) as a Local Service";

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        public static bool Install()
        {
            try { 
                ManagedInstallerClass.InstallHelper(new[] { exePath }); 
            }
            catch { return false; }
            return true;
        }

        public static bool Uninstall()
        {
            try { 
                ManagedInstallerClass.InstallHelper(new[] { "/u", exePath }); 
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
                    writer.WriteLine(String.Format("{0} файл {1} был {2}",
                        DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss"), filePath, fileEvent));
                    writer.Flush();
                }
            }
        }
    }
}
