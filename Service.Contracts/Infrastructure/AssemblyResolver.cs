using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Service.Contracts
{
	public static class AssemblyResolver
	{
		private static bool initialized;
		private static Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
		private static List<string> searchLocations = new List<string>();

		public static void Initialize()
		{
			lock (loadedAssemblies)
			{
				if (initialized) return;
				AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
				initialized = true;
			}
		}

		public static void AddSearchLocation(string dir)
		{
			if (dir == null)
				throw new ArgumentNullException(nameof(dir));
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			lock (loadedAssemblies)
			{
				searchLocations.Add(dir);
			}
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string asmName, culture;
			Assembly asm;
			asmName = GetAssemblyName(args.Name, out culture);
			if (asmName.EndsWith(".resources") && !culture.EndsWith("neutral"))
				return null;
			lock (loadedAssemblies)
			{
				foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					asmName = GetAssemblyName(loadedAssembly.FullName, out culture);
					if (!loadedAssemblies.TryGetValue(asmName, out asm))
						loadedAssemblies.Add(asmName, loadedAssembly);
				}
				asmName = GetAssemblyName(args.Name, out culture);
				if (!loadedAssemblies.TryGetValue(asmName, out asm))
				{
					string path = FindAssembly(asmName);
					if (path != null)
					{
						asm = Assembly.LoadFrom(path);
						loadedAssemblies.Add(asmName, asm);
					}
				}
			}
			return asm;
		}


		private static string GetAssemblyName(string asmName, out string culture)
		{
			string[] tokens = asmName.Split(',');
			if (tokens.Length >= 3)
				culture = tokens[2];
			else
				culture = "neutral";
			return tokens[0];
		}


		private static string FindAssembly(string assemblyName)
		{
			foreach(var asmPath in searchLocations)
			{
				string key = Path.GetFileName(assemblyName).ToLower();
				if (!(key.EndsWith(".dll") || key.EndsWith(".exe")))
				{
					string path = FindAssembly(assemblyName + ".dll");
					if (path != null)
						return path;
					else
						return FindAssembly(assemblyName + ".exe");
				}
				string filePath = Path.Combine(asmPath, assemblyName);
				if (File.Exists(filePath))
				{
					return filePath;
				}
			}
			return null;
		}
	}
}
