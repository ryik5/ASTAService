using System;
using System.Management;
using System.Text;

namespace ASTAWebClient
{
   public class GetMetrics
    {
        StringBuilder sb = new StringBuilder();

        public StringBuilder GetMetrics_Do()
        {
            ManagementObjectCollection moReturn;
            ManagementObjectSearcher moSearch;
            string ia;

            sb = new StringBuilder();
            moSearch = new ManagementObjectSearcher("Select * from Win32_OperatingSystem");
            moReturn = moSearch.Get();
            foreach (ManagementObject mo in moReturn)
            {
                try
                {
                    if (mo["Caption"] != null) ia = mo["Caption"].ToString().Trim().ToUpper(); else ia = "";
                    sb.Append("Name: " + ia);

                    if (mo["OperatingSystemSKU"] != null) ia = CheckTypeProductOS(Convert.ToInt32(mo["OperatingSystemSKU"].ToString().Trim())); else ia = "";
                    sb.Append("Product Info: " + ia);
                    if (mo["CSDVersion"] != null) ia = mo["CSDVersion"].ToString().Trim().ToUpper(); else ia = "";
                    if (mo["Version"] != null) ia += "." + mo["Version"].ToString().Trim().ToUpper(); else ia += "";
                    if (mo["BuildNumber"] != null) ia += "." + mo["BuildNumber"].ToString().Trim().ToUpper(); else ia += "";

                    sb.Append("SP: " + ia);
                    if (mo["OSArchitecture"] != null) ia = mo["OSArchitecture"].ToString().Trim().ToUpper(); else ia = "";
                    sb.Append("OS Arch: " + ia);

                    if (mo["ProductType"] != null) ia = CheckTypeOS(Convert.ToInt32(mo["ProductType"].ToString().Trim())); else ia = "";
                    sb.Append("Type of System: " + ia);

                    if (mo["SystemDirectory"] != null) ia = mo["SystemDirectory"].ToString().Trim().ToUpper(); else ia = "";
                    sb.Append("System directory of OS: " + ia);

                    var key = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64);
                    key = key.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
                    if (key != null)
                    {
                        DateTime startDate = new DateTime(1970, 1, 1, 0, 0, 0);

                        object objValue = key.GetValue("InstallDate"); //InstallTime
                        string stringValue = objValue.ToString();
                        Int64 regVal = Convert.ToInt64(stringValue);

                        DateTime installDate = startDate.AddSeconds(regVal);
                        ia = installDate.ToString("yyyy-MM-dd HH:MM");
                    }

                    sb.Append("Install Date: " + ia);
                    if (mo["LastBootUpTime"] != null) ia = mo["LastBootUpTime"].ToString(); else ia = "";
                    DateTime LastBootUpTime = ManagementDateTimeConverter.ToDateTime(ia);
                    sb.Append("Last BootUp Time: " + LastBootUpTime.ToString("yyyy-MM-dd HH:MM"));
                    double dDay = 0; string sUpTime = "";
                    if (UpTime.TotalHours / 24 > 1) dDay = Math.Round(UpTime.TotalHours / 24, 0);
                    if (dDay > 0)
                        sUpTime = dDay.ToString() + " days and " + (UpTime.TotalHours - dDay * 24).ToString("#.##" + " hours");
                    else
                        sUpTime = UpTime.TotalHours.ToString("#.##" + " hours");
                    sb.Append("Up Time: " + sUpTime);

                }
                catch { }
            }
            moSearch.Dispose();
            moReturn.Dispose();
            return sb;
        }

        private TimeSpan UpTime //Take the OS UP time
        {
            get
            {
                using (var uptime = new System.Diagnostics.PerformanceCounter("System", "System Up Time"))
                {
                    uptime.NextValue();       //Call this an extra time before reading its value
                    return TimeSpan.FromSeconds(uptime.NextValue());
                }
            }
        }

        //Transform gotten data OEM INFO from system to understand info
        private string CheckTypeProductOS(int iNumber) //Type Product
        {
            string sTypeOs = "";
            switch (iNumber)
            {
                case 0:
                    sTypeOs = iNumber.ToString();
                    break;
                case 1:
                    sTypeOs = "Ultimate Edition ";
                    break;
                case 2:
                    sTypeOs = "Home Basic Edition";
                    break;
                case 3:
                    sTypeOs = "Home Premium Edition";
                    break;
                case 4:
                    sTypeOs = "Enterprise Edition";
                    break;
                case 6:
                    sTypeOs = "Business Edition";
                    break;
                case 7:
                    sTypeOs = "Windows Server Standard Edition (Desktop Experience installation)";
                    break;
                case 8:
                    sTypeOs = "Windows Server Datacenter Edition (Desktop Experience installation";
                    break;
                case 9:
                    sTypeOs = "Small Business Server Editio";
                    break;
                case 10:
                    sTypeOs = "Enterprise Server Edition";
                    break;
                case 11:
                    sTypeOs = "Starter Edition";
                    break;
                case 12:
                    sTypeOs = "Datacenter Server Core Edition";
                    break;
                case 13:
                    sTypeOs = "Standard Server Core Edition";
                    break;
                case 14:
                    sTypeOs = "Enterprise Server Core Edition";
                    break;
                case 17:
                    sTypeOs = "Web Server Edition";
                    break;
                case 19:
                    sTypeOs = "Home Server Edition";
                    break;
                case 20:
                    sTypeOs = "Storage Express Server Edition";
                    break;
                case 21:
                    sTypeOs = "Windows Storage Server Standard Edition (Desktop Experience installation)";
                    break;
                case 22:
                    sTypeOs = "Windows Storage Server Workgroup Edition (Desktop Experience installation)";
                    break;
                case 23:
                    sTypeOs = "Storage Enterprise Server Edition";
                    break;
                case 24:
                    sTypeOs = "Server For Small Business Edition";
                    break;
                case 25:
                    sTypeOs = "Small Business Server Premium Edition";
                    break;
                case 27:
                    sTypeOs = "Windows Enterprise Edition";
                    break;
                case 28:
                    sTypeOs = "Windows Ultimate Edition";
                    break;
                case 29:
                    sTypeOs = "Windows Server Web Server Edition (Server Core installation)";
                    break;
                case 36:
                    sTypeOs = "Windows Server Standard Edition without Hyper-V";
                    break;
                case 37:
                    sTypeOs = "Windows Server Datacenter Edition without Hyper-V (full installation)";
                    break;
                case 38:
                    sTypeOs = "Windows Server Enterprise Edition without Hyper-V (full installation)";
                    break;
                case 39:
                    sTypeOs = "Windows Server Datacenter Edition without Hyper-V (Server Core installation)";
                    break;
                case 40:
                    sTypeOs = "Windows Server Standard Edition without Hyper-V (Server Core installation)";
                    break;
                case 41:
                    sTypeOs = "Windows Server Enterprise Edition without Hyper-V (Server Core installation)";
                    break;
                case 42:
                    sTypeOs = "Microsoft Hyper-V Server";
                    break;
                case 43:
                    sTypeOs = "Storage Server Express Edition (Server Core installation)";
                    break;
                case 44:
                    sTypeOs = "Storage Server Standard Edition (Server Core installation)";
                    break;
                case 45:
                    sTypeOs = "Storage Server Workgroup Edition (Server Core installation)";
                    break;
                case 46:
                    sTypeOs = "Storage Server Enterprise Edition (Server Core installation)";
                    break;
                case 50:
                    sTypeOs = "Windows Server Essentials (Desktop Experience installation)";
                    break;
                case 63:
                    sTypeOs = "Small Business Server Premium (Server Core installation)";
                    break;
                case 64:
                    sTypeOs = "Windows Compute Cluster Server without Hyper-V";
                    break;
                case 97:
                    sTypeOs = "CORE_ARM";
                    break;
                case 101:
                    sTypeOs = "Windows Home";
                    break;
                case 103:
                    sTypeOs = "Windows Professional with Media Center";
                    break;
                case 104:
                    sTypeOs = "Windows Mobile";
                    break;
                case 123:
                    sTypeOs = "Windows IoT (Internet of Things) Core";
                    break;
                case 143:
                    sTypeOs = "Windows Server Datacenter Edition (Nano Server installation)";
                    break;
                case 144:
                    sTypeOs = "Windows Server Standard Edition (Nano Server installation)";
                    break;
                case 147:
                    sTypeOs = "Windows Server Datacenter Edition (Server Core installation)";
                    break;
                case 148:
                    sTypeOs = "Windows Server Standard Edition (Server Core installation)";
                    break;
                default:
                    sTypeOs = iNumber.ToString();
                    break;
            }
            return sTypeOs;
        }

        private string CheckTypeOS(int iNumber) //Type host
        {
            string sTypeOs = "";
            switch (iNumber)
            {
                case 1:
                    sTypeOs = "Workstation";
                    break;
                case 2:
                    sTypeOs = "Domain Controller";
                    break;
                case 3:
                    sTypeOs = "Server";
                    break;
                default:
                    sTypeOs = iNumber.ToString();
                    break;
            }
            return sTypeOs;
        }
    }
}
