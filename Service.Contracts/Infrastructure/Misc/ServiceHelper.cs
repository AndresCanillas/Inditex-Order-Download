#if NET461
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public static class ServiceHelper
	{
		/// <summary>
		/// Checks if the specified service is installed and running.
		/// </summary>
		/// <param name="serviceName">The name of the service</param>
		/// <returns></returns>
		public static bool IsServiceRunning(string serviceName)
		{
			ServiceController[] services = ServiceController.GetServices();
			foreach (ServiceController service in services)
			{
				if (service.ServiceName.StartsWith(serviceName))
				{
					if (service.Status == ServiceControllerStatus.Running)
						return true;
					else
						return false;
				}
			}
			throw new Exception($"Service {serviceName} is not installed.");
		}


		public static bool IsServiceInstalled(string serviceName)
		{
			ServiceController[] services = ServiceController.GetServices();
			foreach (ServiceController service in services)
			{
				if (service.ServiceName == serviceName)
					return true;
			}
			return false;
		}


		public static ServiceController FindService(string serviceName)
		{
			ServiceController[] services = ServiceController.GetServices();
			foreach (ServiceController service in services)
			{
				if (service.ServiceName == serviceName)
					return service;
			}
			return null;
		}


		public static void StartService(string serviceName)
		{
			ServiceController service = FindService(serviceName);
			if (service != null)
			{
				if (service.Status == ServiceControllerStatus.Stopped)
					service.Start();
			}
		}


		public static void RestartService(string serviceName, int retryCount)
		{
			StopService(serviceName, retryCount);
			StartService(serviceName, retryCount);
		}


		public static void StopService(string serviceName)
		{
			StopService(serviceName, 2, false, null);
		}


		public static void StopService(string serviceName, int retryCount)
		{
			StopService(serviceName, retryCount, false, null);
		}


		public static void StopService(string serviceName, int retryCount, bool killUnresponsiveProcess, string processName)
		{
			bool sucess = false;
			int count = 0;
			ServiceController service = FindService(serviceName);
			if (service == null)
				return;
			do
			{
				count++;
				try
				{
					if (service.Status == ServiceControllerStatus.Running)
					{
						service.Stop();
						service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
					}
					service.Refresh();
					if (service.Status == ServiceControllerStatus.Stopped)
					{
						sucess = true;
					}
				}
				catch
				{
					if (count >= retryCount)
						throw;
					else
						Thread.Sleep(500);
				}
			} while (count < retryCount && !sucess);
			if (!sucess)
			{
				if (killUnresponsiveProcess)
				{
					KillProcess(processName);
				}
				else throw new Exception("Unable to stop service.");
			}
			Thread.Sleep(1000);
		}


		public static void KillProcess(string processName)
		{
			Process[] processes = Process.GetProcessesByName(processName);
			if (processes != null)
			{
				foreach (Process p in processes)
				{
					p.Kill();
				}
			}
		}

		public static void StartService(string serviceName, int retryCount)
		{
			bool sucess = false;
			int count = 0;
			ServiceController service = FindService(serviceName);
			if (service == null) return;
			do
			{
				count++;
				try
				{
					if (service.Status == ServiceControllerStatus.Stopped)
					{
						service.Start();
						service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
					}
					service.Refresh();
					if (service.Status == ServiceControllerStatus.Running)
					{
						sucess = true;
					}
				}
				catch
				{
					if (count >= retryCount)
						throw;
					else
						Thread.Sleep(500);
				}
			} while (count < retryCount && !sucess);
			if (!sucess)
				throw new Exception("Unable to start service.");
		}


		public static string GetServiceAccount(string serviceAssembly)
		{
			SelectQuery query = new SelectQuery(string.Format("select name, startname from Win32_Service where name = '{0}'", serviceAssembly));
			using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
			{
				foreach (ManagementObject service in searcher.Get())
				{
					return service["startname"].ToString();
				}
			}
			return "";
		}


		public static void InstallService(string serviceName, string assemblyName)
		{
			InstallService(serviceName, serviceName, assemblyName, false);
		}

		public static void InstallService(string serviceName, string description, string assemblyName)
		{
			InstallService(serviceName, description, assemblyName, false);
		}

		public static void InstallService(string serviceName, string description, string assemblyName, bool overwrite)
		{
			bool sucess = false;
			int retryCount = 0;
			if (overwrite && IsServiceInstalled(serviceName))
			{
				UninstallService(serviceName);
			}
			do
			{
				try
				{
					Process p = new Process();
					ProcessStartInfo info = new ProcessStartInfo();
					OperatingSystem OS = Environment.OSVersion;
					if (!(OS.Platform == PlatformID.Win32NT && OS.Version.Major < 6))
						info.Verb = "runas";
					info.WindowStyle = ProcessWindowStyle.Hidden;
					info.FileName = "sc.exe";
					info.Arguments = String.Format("create \"{0}\" binPath= \"{1}\" start= auto displayName= \"{0}\"", serviceName, assemblyName);
					p.StartInfo = info;
					p.Start();
					p.WaitForExit();
					if (p.ExitCode != 0)
						throw new Exception("Error while creating service " + serviceName + ": " + p.ExitCode);
					p = new Process();
					info.Arguments = String.Format("description \"{0}\" \"{1}\"", serviceName, description);
					p.StartInfo = info;
					p.Start();
					p.WaitForExit();
					if (p.ExitCode != 0)
						throw new Exception("Error while editing service " + serviceName + ": " + p.ExitCode);
					sucess = true;
				}
				catch
				{
					retryCount++;
					if (retryCount >= 3)
						throw;
				}
			} while (!sucess);
		}


		public static void UninstallService(string serviceName)
		{
			if (IsServiceInstalled(serviceName))
			{
				ServiceHelper.StopService(serviceName, 3);
				bool sucess = false;
				int retryCount = 0;
				do
				{
					try
					{
						Process p = new Process();
						ProcessStartInfo info = new ProcessStartInfo();
						OperatingSystem OS = Environment.OSVersion;
						if (!(OS.Platform == PlatformID.Win32NT && OS.Version.Major < 6))
							info.Verb = "runas";
						info.WindowStyle = ProcessWindowStyle.Hidden;
						info.FileName = "sc.exe";
						info.Arguments = String.Format("delete \"{0}\"", serviceName);
						p.StartInfo = info;
						p.Start();
						p.WaitForExit();
						if (p.ExitCode != 0)
							throw new Exception("Error while removing service " + serviceName + ": " + p.ExitCode);
						sucess = true;
					}
					catch
					{
						retryCount++;
						if (retryCount >= 3)
							throw;
					}
				} while (!sucess);
			}
		}


		public static void RenameService(string originalName, string newName, string targetAssembly)
		{
			UninstallService(originalName);
			InstallService(newName, targetAssembly);
		}

		public static void ChangeServiceDescription(string serviceName, string description)
		{
			Process p = new Process();
			ProcessStartInfo info = new ProcessStartInfo();
			OperatingSystem OS = Environment.OSVersion;
			if (!(OS.Platform == PlatformID.Win32NT && OS.Version.Major < 6))
				info.Verb = "runas";
			info.WindowStyle = ProcessWindowStyle.Hidden;
			info.FileName = "sc.exe";
			info.Arguments = String.Format("description \"{0}\" \"{1}\"", serviceName, description);
			p.StartInfo = info;
			p.Start();
			p.WaitForExit();
			if (p.ExitCode != 0)
				throw new Exception("Error while updating service description " + serviceName + ": " + p.ExitCode);
		}
	}
}
#endif