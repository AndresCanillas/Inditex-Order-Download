using Newtonsoft.Json;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace PrintCentral.Utilities
{
    public class ServiceStatus
    {
        public static string GetWindowsServiceStatus(String servicename)
        {
            List<ServiceDT> lstServices = new List<ServiceDT>();
            List<ServiceController> services = ServiceController.GetServices().Where(s => s.ServiceName.Contains(servicename)).ToList();

            int loop = 0;
            foreach (var service in services)
            {
            
                ServiceDT sc = new ServiceDT();

                switch (service.Status)
                {
                    case ServiceControllerStatus.Running:
                        sc.Status = "Running";
                        break;
                    case ServiceControllerStatus.Stopped:
                        sc.Status = "Stopped";
                        break;
                    case ServiceControllerStatus.Paused:
                        sc.Status = "Paused";
                        break;
                    case ServiceControllerStatus.StopPending:
                        sc.Status = "Stopping";
                        break;
                    case ServiceControllerStatus.StartPending:
                        sc.Status = "Starting";
                        break;
                    default:
                        sc.Status = "Status Changing";
                        break;
                }

                sc.Actions = "";
                sc.ServiceName = service.ServiceName;
                sc.ID = loop++;

                lstServices.Add(sc);
            }

            return JsonConvert.SerializeObject(lstServices);
        }

        public static bool StartWindowsService(String servicename)
        {
            double timeoutMilliseconds = 10_000;
            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

            ServiceController sc = new ServiceController(servicename);

            try
            {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static String StopWindowsService(String servicename)
        {

            double timeoutMilliseconds = 10_000;
            TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            ServiceController service = new ServiceController(servicename);
            try
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                return JsonConvert.SerializeObject("Stop");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string RestartWindowsService(string servicename)
        {
            ServiceController service = new ServiceController(servicename);
            try
            {
                double timeoutMilliseconds = 10_000;
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return JsonConvert.SerializeObject("Restart");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string GetProcessIdByServiceName(string servicename)
        {
            foreach (ServiceController Svc in ServiceController.GetServices())
            {
                using (Svc)
                {
                    //The short name of "Microsoft Exchange Service Host"
                    if (Svc.ServiceName.Equals(servicename))
                    {
                        //Try to stop using Process
                        foreach (Process Prc in Process.GetProcessesByName(Svc.ServiceName))
                        {
                            using (Prc)
                            {
                                try
                                {
                                    //Try to kill the service process
                                    Prc.Kill();
                                    return JsonConvert.SerializeObject("Kill");
                                }
                                catch
                                {
                                    //Try to terminate the service using taskkill command
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "cmd.exe",
                                        CreateNoWindow = true,
                                        UseShellExecute = false,
                                        Arguments = string.Format("/c taskkill /pid {0} /f", Prc.Id)
                                    });

                                    //Additional:
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "net.exe",
                                        CreateNoWindow = true,
                                        UseShellExecute = false,
                                        Arguments = string.Format("stop {0}", Prc.ProcessName)
                                    });

                                    return JsonConvert.SerializeObject("Kill");
                                }
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject("");
        }
    }

    public class ServiceDT{
        public int ID { get; set; }
        public string ServiceName { get; set; }
        public string Status { get; set; }
        public string Actions { get; set; }
    }

}
