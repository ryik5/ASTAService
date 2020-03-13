using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Data;
using System.Diagnostics;
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
        public AstaServiceLocal()
        {
            InitializeComponent();
            workerThread = new Thread(DoWork);
            workerThread.SetApartmentState(ApartmentState.STA);
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
                MessageBox.Show("Start");
                // log.Info("Doing work...");
                // do some work, then
                Thread.Sleep(5000);
            }
        }
    }



    // Утилита саморегистрации
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
}
