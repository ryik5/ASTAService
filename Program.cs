using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ASTAService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var service = new AstaServiceLocal();
            ServiceBase[] ServicesToRun;

            ServicesToRun = new ServiceBase[]
            {
                service
            };

            if (Environment.UserInteractive)
            {
                // Разбор пути для саморегистрации
                if (args != null && args[0].Length > 1
                    && (args[0].StartsWith("-") || args[0].StartsWith("/")))
                {
                    switch (args[0].Substring(1).ToLower())
                    {
                        case "install":
                        case "i":
                            if (!ServiceInstallerUtility.Install())
                                Console.WriteLine("Failed to install service");
                            else
                            {
                                service.Start();
                                Console.WriteLine("Running service");
                            }
                            break;
                        case "uninstall":
                        case "u":
                            service.Stop();
                            if (!ServiceInstallerUtility.Uninstall())
                                Console.WriteLine("Failed to uninstall service");
                            else 
                                Console.WriteLine("Service stopped. Goodbye.");
                            //"taskkill /f /IM astaservice.exe";
                            break;
                        default:
                            // ServiceInstallerUtility.Install();
                            break;
                    }
                }

                //Console.CancelKeyPress += (x, y) => service.Stop();
                //ServiceInstallerUtility.Install();
                //Console.WriteLine("Running service, press a key to stop");
                //Console.ReadKey();
                //service.Stop();
                //Console.WriteLine("Service stopped. Goodbye.");
            }
            else
            {
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
