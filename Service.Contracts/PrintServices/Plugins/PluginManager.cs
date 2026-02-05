using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;

namespace Service.Contracts
{
    public interface IPluginManager<T> where T: class
    {
        IEnumerable<IPluginInfo> GetList();
        T GetInstanceByName(string pluingName);
    }

    public interface IPluginInfo
    {
        string Name { get; }
        string Description { get; }
    }

    public class PluginInfo : IPluginInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        internal string FileName { get; set; }
        internal Type PluginType { get; set; }
    }

    public class PluginManager<T> : IPluginManager<T> where T : class
    {
        private string pluginDirectory;
        private List<PluginInfo> plugins;
        private IFactory factory;
        private ILogService log;
        private IConnectionManager connMng;

        private List<PluginInfo> dbPluginsList;

        public PluginManager(IFactory factory, IAppInfo appInfo, IAppConfig config, ILogService log, IConnectionManager connMng)
        {
            this.factory = factory;
            this.log = log;
            this.connMng = connMng;
            var typeName = typeof(T).Name;
            var defaultDirectory = Path.Combine(appInfo.AssemblyDir, "plugins");
            pluginDirectory = config.GetValue($"Plugins.{typeName}", defaultDirectory);
            if(String.IsNullOrWhiteSpace(pluginDirectory)) return;

            log.LogMessage("Plugin Manager Directory Path: '{0}'", pluginDirectory);

            ChecklPuginsTableCreated();
            dbPluginsList = GetPluginsFromDB();

            plugins = new List<PluginInfo>();
            if(Directory.Exists(pluginDirectory))
            {
                foreach(var f in Directory.EnumerateFiles(pluginDirectory, "*.dll"))
                {
                    LoadPlugins(f);
                }
            }
        }

        private void LoadPlugins(string filename)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var asm = FindAssembly(assemblies, filename);
                if(asm == null)
                    asm = Assembly.LoadFrom(filename);

                var types = asm.GetTypes();
                foreach(var t in types)
                {
                    if(typeof(T).IsAssignableFrom(t) && !t.IsAbstract)
                    {
                        AddPlugin(filename, t);
                        log.LogMessage($"Loaded plugin {t.Name} from {filename}");
                    }
                    else
                    {
                        var isassginable = typeof(T).IsAssignableFrom(t);
                        var isabastract = t.IsAbstract;
                        var typename = t.Name;
                    }
                

            }
            }
            catch(ReflectionTypeLoadException tle)
            {
                if(tle.LoaderExceptions.Length > 0)
                    log.LogException($"Error while loading assembly {filename}, plugins of type {typeof(T).Name} will not be loaded from this assembly.", tle.LoaderExceptions[0]);
                else
                    log.LogException($"Error while loading assembly {filename}, plugins of type {typeof(T).Name} will not be loaded from this assembly.", tle);
            }
            catch(Exception ex)
            {
                log.LogException($"Error while loading assembly {filename}, plugins of type {typeof(T).Name} will not be loaded from this assembly.", ex);
            }
        }

        private Assembly FindAssembly(Assembly[] assemblies, string filename)
        {
            filename = filename.Trim();
            for(int i = 0; i < assemblies.Length; i++)
            {
                var asm = assemblies[i];
                if(asm.IsDynamic)
                    continue;
                var loc = asm.Location.Trim();
                if(String.Compare(loc, filename, true) == 0)
                    return asm;
            }
            return null;
        }

        private void AddPlugin(string assemblyName, Type t)
        {
            try
            {
                var info = GetPluginInfo(assemblyName, t);
                plugins.Add(info);

                // Insert into database
                UpdatePluginsTable(info);
            }
            catch(Exception ex)
            {
                log.LogException($"Error loading {t.Name} form {assemblyName}.", ex);
            }
        }

        private void ChecklPuginsTableCreated()
        {
            using(var suppressedScope = new TransactionScope(TransactionScopeOption.Suppress))
            {
                using(var db = connMng.OpenDB())
                {
                    db.ExecuteNonQuery(@"
				    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Plugins]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[Plugins](
		                    [ID] [int] IDENTITY(1,1) NOT NULL,
                            [Type] [nvarchar](100) NOT NULL,
				            [Name] [nvarchar](100) NOT NULL,
                            [Description] [nvarchar](MAX),
                            CONSTRAINT [PK_Plugin] PRIMARY KEY CLUSTERED 
                            (
				                [ID] ASC
                            ) ON [PRIMARY]
                        ) ON [PRIMARY] 
                        CREATE UNIQUE INDEX IX_Plugins_Name ON [dbo].[Plugins] ( [Name] )
                        CREATE INDEX IX_Plugins_Type ON [dbo].[Plugins] ( [Type] )
                    END"
                    );
                }
            }
        }

        private void UpdatePluginsTable(PluginInfo info)
        {
            // if plugin don't exists insert it into table 
            if(!dbPluginsList.Any(p => String.Compare(p.Name, info.Name, true) == 0))
            {
                AddPluginIntoDB(info);
            }
        }

        private List<PluginInfo> GetPluginsFromDB()
        {
            var ret = new List<PluginInfo>();

            using(var suppressedScope = new TransactionScope(TransactionScopeOption.Suppress))
            {
                using(var db = connMng.OpenDB())
                {
                    ret = db.Select<PluginInfo>(
                        $@"SELECT p.[Name], p.[Description] FROM [dbo].[Plugins] p WHERE p.[Type] = @type",
                        typeof(T).Name);
                }
                return ret;
            }
        }

        private void AddPluginIntoDB(PluginInfo info)
        {
            using(var suppressedScope = new TransactionScope(TransactionScopeOption.Suppress))
            {
                using(var db = connMng.OpenDB())
                {
                    db.ExecuteNonQuery($@"
                    INSERT INTO [dbo].[Plugins] ([Type], [Name], [Description])
                    VALUES (@type, @name, @description)
                ", typeof(T).Name, info.Name, info.Description);
                }
            }
        }

        private PluginInfo GetPluginInfo(string fileName, Type t)
        {
            var info = new PluginInfo();
            info.FileName = fileName;
            info.PluginType = t;
            info.Name = t.Name;
            info.Description = "N/A";
            foreach (var attr in t.GetCustomAttributes())
            {
                if (attr is FriendlyName)
                    info.Name = (attr as FriendlyName).Text;
                if (attr is Description)
                    info.Description = (attr as Description).Text;
            }
            return info;
        }

        public IEnumerable<IPluginInfo> GetList()
        {
            return GetPluginsFromDB();
        }

        public T GetInstanceByName(string pluingName)
        {
            foreach (var elm in plugins)
            {
                if (String.Compare(elm.Name, pluingName, true) == 0)
                {
                    return factory.GetInstance(elm.PluginType) as T;
                }
            }
            throw new InvalidOperationException($"Plugin {pluingName} was not found. Ensure plugin assemblies are copied to the correct location.");
        }
    }
}
