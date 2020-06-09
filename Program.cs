using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASTAService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application
        /// </summary>
        /// <param name="args"> Parameters for install: ASTAService.exe -i, uninstall: ASTAService.exe -u </param>
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
                             {
                                //  MessageBox.Show("Failed to install service");
                                }
                            else
                            {
                                service.Start();
                             //   MessageBox.Show("Running service");
                            }
                            break;
                        case "uninstall":
                        case "u":
                            ServiceInstallerUtility.StopService("ASTAService.exe",2000);
                            service.Stop();
                            if (!ServiceInstallerUtility.Uninstall())
                              {
                              //  MessageBox.Show("Failed to uninstall service");
                                }
                            else
                              {  
                            //"taskkill /f /IM astaservice.exe";
                          //      MessageBox.Show("Service stopped. Goodbye.");
                                }
                            break;
                        default:
                            service.Start();
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
                service.Start();
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
