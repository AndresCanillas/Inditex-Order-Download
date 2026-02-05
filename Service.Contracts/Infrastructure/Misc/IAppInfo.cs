using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public interface IAppInfo
	{
		string NodeName { get; }
		string AppName { get; }
		string AssemblyDir { get; }
		string SystemDataDir { get; }
		string SystemDownloadsDir { get; }
		string SystemTempDir { get; }
		string SystemLogDir { get; }
		string SystemBackupDir { get; }
		string UserDataDir { get; }
		string UserDownloadsDir { get; }
		string UserTempDir { get; }
	}

	public class AppInfoPaths
	{
		public string DataDirectory;
		public string DownloadsDirectory;
		public string TempDirectory;
		public string LogDirectory;
		public string BackupDirectory;
	}


	public class AppInfo : IAppInfo
	{
		private string appName;
		private string assemblyDir;
		private string systemDataDir;
		private string systemDownloadsDir;
		private string systemTempDir;
		private string systemLogDir;
		private string systemBackupDir;
		private string userDataDir;
		private string userDownloadsDir;
		private string userTempDir;

		public AppInfo(IAppConfig config)
		{
			appName = Process.GetCurrentProcess().ProcessName;
			assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\"));
			InitializeDefaultPaths();
			var paths = config.Bind<AppInfoPaths>("AppPaths");
			if (paths != null)
			{
				if(!String.IsNullOrWhiteSpace(paths.DataDirectory))
					systemDataDir = paths.DataDirectory;

				if (!String.IsNullOrWhiteSpace(paths.DownloadsDirectory))
					systemDownloadsDir = paths.DownloadsDirectory;

				if (!String.IsNullOrWhiteSpace(paths.TempDirectory))
					systemTempDir = paths.TempDirectory;

				if (!String.IsNullOrWhiteSpace(paths.LogDirectory))
					systemLogDir = paths.LogDirectory;

				if (!String.IsNullOrWhiteSpace(paths.BackupDirectory))
					systemBackupDir = paths.BackupDirectory;

				InitializeUserPaths();
			}
			CreateDirs(systemDataDir, systemDownloadsDir, systemTempDir, systemLogDir, systemBackupDir, userDataDir, userDownloadsDir, userTempDir);
		}


		private void InitializeDefaultPaths()
		{
			systemDataDir = Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.CommonApplicationData,
					Environment.SpecialFolderOption.Create), appName);

			systemDownloadsDir = Path.Combine(systemDataDir, "Downloads");
			systemTempDir = Path.Combine(systemDataDir, "Temp");
			systemLogDir = Path.Combine(systemDataDir, "Log");
			systemBackupDir = Path.Combine(systemDataDir, "Backups");
			InitializeUserPaths();
		}


		private void InitializeUserPaths()
		{
			if (Environment.UserInteractive)
			{
				userDataDir = Path.Combine(
					Environment.GetFolderPath(
						Environment.SpecialFolder.LocalApplicationData,
						Environment.SpecialFolderOption.Create), appName);
				userDownloadsDir = Path.Combine(userDataDir, "Downloads");
				userTempDir = Path.Combine(userDataDir, "Temp");
			}
			else
			{
				userDataDir = systemDataDir;
				userDownloadsDir = systemDownloadsDir;
				userTempDir = systemTempDir;
			}
		}


		private void CreateDirs(params string[] paths)
		{
			foreach (string dir in paths)
			{
				if (!Directory.Exists(dir))
				{
					try
					{
						Directory.CreateDirectory(dir);
					}
					catch
					{
						// Could not create the directory, ignore error
					}
				}
			}
		}

		public string NodeName { get => Environment.MachineName; }

		public string AppName { get { return appName; } }

		public string AssemblyDir { get { return assemblyDir; } }

		public string SystemDataDir { get { return systemDataDir; } }

		public string SystemDownloadsDir { get { return systemDownloadsDir; } }

		public string SystemTempDir { get { return systemTempDir; } }

		public string SystemLogDir { get { return systemLogDir; } }

		public string SystemBackupDir { get { return systemBackupDir; } }

		public string UserDataDir { get { return userDataDir; } }

		public string UserDownloadsDir { get { return userDownloadsDir; } }

		public string UserTempDir { get { return userTempDir; } }
	}
}
