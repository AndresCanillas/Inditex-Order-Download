using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
	public interface IAppConfig
	{
		T GetValue<T>(string key);
		T GetValue<T>(string key, T defaultValue);
		string this[string key] { get; }
		T Bind<T>(string key);
		string FileName { get; }

		/// <summary>
		/// Returns an object initialized with the properties of the specified section of the configuration file.
		/// IMPORTANT: This overload should only be used from singleton services, otherwise, scoped and transient
		/// services might leak if they call this method (by preventing the GC from removing them from the heap).
		/// </summary>
		T Bind<T>(string key, Action handleUpdate);

		void Load(string filename);
	}

	public class AppConfig: IAppConfig
	{
		private readonly object syncObj = new object();
		private readonly ConcurrentDictionary<Action, Action> registeredUpdateHandlers = new ConcurrentDictionary<Action, Action>();
		private JObject root = new JObject();

		public string FileName { get; }

		public AppConfig()
		{
			string settingsFile;
			var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\"));
			var cfgFileArg = CmdLineHelper.ExtractCmdArgument("config", false, null);
			if (cfgFileArg != null)
				settingsFile = Path.Combine(assemblyDir, cfgFileArg);
			else
				settingsFile = Path.Combine(assemblyDir, Process.GetCurrentProcess().ProcessName + ".json");
#if DEBUG
            if(!File.Exists(settingsFile))
                settingsFile = Path.Combine(assemblyDir, "appsettings.debug.json");
#endif            
            if(!File.Exists(settingsFile))
				settingsFile = Path.Combine(assemblyDir, "appsettings.json");

			if (File.Exists(settingsFile))
				LoadFile(settingsFile);

			FileName = settingsFile;
		}

		private void LoadFile(string settingsFile)
		{
			var json = File.ReadAllText(settingsFile);

            lock(syncObj)
            {
                try
                {
                    root = JObject.Parse(json);
                }
                catch (Exception ex)
                {
                    throw new Exception($"error parsing file{settingsFile}]", ex);
                }
            }
			string dir = Path.GetDirectoryName(settingsFile);
			string filename = Path.GetFileName(settingsFile);
			FileSystemWatcher fsw = new FileSystemWatcher(dir, filename);
			fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite;
			fsw.Changed += FSW_Changed;
			fsw.EnableRaisingEvents = true;
		}

		private void FSW_Changed(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType == WatcherChangeTypes.Changed)
			{
				try
				{
					var json = File.ReadAllText(e.FullPath);
					lock (syncObj)
						root = JObject.Parse(json);
					foreach (var handler in registeredUpdateHandlers.Keys)
					{
						try { handler(); }
						catch { }
					}
				}
				catch { }
			}
		}

		public void Load(string filename)
		{
			lock (syncObj)
				root = JObject.Parse(File.ReadAllText(filename));
		}

		public T GetValue<T>(string key)
		{
			lock (syncObj)
				return root.GetValue<T>(key);
		}

		public T GetValue<T>(string key, T defaultValue)
		{
			lock (syncObj)
				return root.GetValue<T>(key, defaultValue);
		}

		public string this[string key]
		{
			get
			{
				lock (syncObj)
					return root.GetValue<string>(key);
			}
		}

		public T Bind<T>(string key)
		{
			string json;
			lock (syncObj)
				json = root.GetProperty(key);

			if (json != null)
				return JsonConvert.DeserializeObject<T>(json);
			else
				return default(T);
		}


		/// <summary>
		/// Returns an object initialized with the properties of the specified section of the configuration file.
		/// IMPORTANT: This overload should only be used from singleton services, otherwise, references to scoped
		/// or transient services might leak (by preventing the GC from removing them from the heap).
		/// </summary>
		public T Bind<T>(string key, Action handleUpdate)
		{
			T result = Bind<T>(key);
			if(handleUpdate != null)
			{
				if (!registeredUpdateHandlers.TryGetValue(handleUpdate, out var _))
					registeredUpdateHandlers[handleUpdate] = handleUpdate;
			}
			return result;
		}
	}
}
