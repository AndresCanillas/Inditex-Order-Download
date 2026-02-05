using System;

namespace Service.Contracts
{
    public interface IConfigurationSystem<T> where T : class
	{
		string Name { get; }
		void Setup(IFactory factory, IConfigurationContext ctx);
		T GetConfiguration();
	}

	public interface IConfigurationContext
	{
        void RegisterSystem<T>() where T : class, new();

        void RegisterComponent<T>();
		Contract GetInstance<Contract>(string data);
	}

	public interface IConfigurable<T> where T : class
	{
		T GetConfiguration();
		void SetConfiguration(T config);
	}

	public interface IMetadataStore
	{
		void SetMeta<T>(string path, T meta)
			where T : class, new();
        
        void SetDefaultConfiguration<T>(string path, T meta)
            where T : class, new();

        bool TryGetValue(string path, out string result);
		
        T GetMeta<T>(string path)
			where T : class, new();
	}

	public class EmptyConfig
	{
		public static EmptyConfig Value = new EmptyConfig();
	}
}
