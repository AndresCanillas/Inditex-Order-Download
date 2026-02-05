using Newtonsoft.Json;
using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Service.Contracts
{
    public class ConfigurationContext : IConfigurationContext
	{
        internal static ComponentCollection Components = new ComponentCollection();
        
        private readonly IFactory factory;
        private readonly IMetadataStore metadataStore;
        private readonly ILogService log;
        private readonly ConcurrentDictionary<Type, string> registeredSystems = new ConcurrentDictionary<Type, string>();
        private readonly ConcurrentDictionary<string, int> loadedAssemblies = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<Type, string> registeredComponentTypes = new ConcurrentDictionary<Type, string>();

        private string currentSystemUrl;
        private List<ComponentMeta> currentSystemComponents;

        public ConfigurationContext(IFactory factory, IMetadataStore metadataStore, ILogService log)
		{
			this.factory = factory;
			this.metadataStore = metadataStore;
            this.log = log;
			currentSystemUrl = null;
        }


        public void RegisterSystem<T>() where T : class, new()
        {
            var t = typeof(T);
            try
            {
                if(!registeredSystems.ContainsKey(t))
                {
                    object instance = Activator.CreateInstance(t);
                    var systemName = Reflex.GetProperty(instance, "Name");
                    currentSystemUrl = $"/meta/{systemName}";
                    currentSystemComponents = new List<ComponentMeta>();

                    Reflex.Invoke(instance, "Setup", new object[] { factory, this });

                    var systemMeta = new ConfigMeta();
                    var configType = t.GetConfigurationType();
                    systemMeta.Init(configType);
                    metadataStore.SetMeta(currentSystemUrl, systemMeta);

                    metadataStore.SetMeta($"{currentSystemUrl}/components", currentSystemComponents);

                    var configMethod = t.GetMethod("GetConfiguration");
                    var defaultConfig = configMethod.Invoke(instance, null);
                    metadataStore.SetDefaultConfiguration($"{currentSystemUrl}/default", defaultConfig);

                    registeredSystems[t] = t.Name;
                }
            }
            catch(Exception ex)
            {
                var log = factory.GetInstance<ILogService>();
                log.LogException(ex);
            }
        }


		public void RegisterComponent<T>()
		{
            Type ctype = typeof(T);

            if(String.IsNullOrWhiteSpace(currentSystemUrl))
                throw new InvalidOperationException("This operation can only be executed during Configuration System Setup");

            if(!ctype.IsInterface)
                throw new InvalidOperationException("Component Type <T> must be an interface.");

            //Prevents processing the same component type multiple times.
            if(!registeredComponentTypes.TryAdd(ctype, currentSystemUrl))
                throw new InvalidOperationException($"Component {ctype.Name} was already registered by system {registeredComponentTypes[ctype]}. Components cannot be shared between different configuration systems.");

            Components.RegisterComponentType(ctype);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach(var asm in assemblies)
            {
                var implementations = asm.GetTypes().Where(t => !t.IsAbstract && t.IsClass && t.Implements(ctype)).ToList();
                if(implementations.Count > 0)
                {
                    foreach(Type t in implementations)
                    {
                        if(!t.IsAbstract && t.IsClass && t.Implements(ctype))
                        {
                            Components.Add(ctype, t);
                            var compMeta = currentSystemComponents.FirstOrDefault(p => p.Contract == ctype.Name);
                            if(compMeta == null)
                            {
                                compMeta = new ComponentMeta() { Contract = ctype.Name };
                                currentSystemComponents.Add(compMeta);
                            }
                            compMeta.Implementations.Add(new ComponentConfigMeta().Init(ctype, t));
                        }
                    }
                }
            }
		}

		public Contract GetInstance<Contract>(string data)
		{
			var cfgType = typeof(Contract);
			var jsonConverter = factory.GetInstance<ConfigJSONConverter>();
			var config = JsonConvert.DeserializeObject(data, cfgType, jsonConverter.Settings);
			return (Contract)config;
		}
	}
}
