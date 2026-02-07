using Service.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using System.Linq;
using Service.Contracts.Database;
using Services.Core;

namespace WebLink.Services
{
	public class CatalogPluginManager: ICatalogPluginManager
	{
		private IFactory factory;
		private IEventQueue events;
		private IAppConfig appConfig;
		private ILogService log;

		private object syncObj = new object();
		private List<ICatalogPluginInfo> plugins = new List<ICatalogPluginInfo>();

		public CatalogPluginManager(IFactory factory, IEventQueue events, IAppConfig appConfig, ILogService log)
		{
			this.factory = factory;
			this.events = events;
			this.appConfig = appConfig;
			this.log = log;
		}

		public void Start()
		{
			var dir = appConfig.GetValue<string>("Plugins.CatalogPlugins");
			if (!String.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
			{
				foreach (var f in Directory.EnumerateFiles(dir, "*.dll"))
				{
					LoadAssemblyPlugins(f);
				}
				events.Subscribe<DynamicDBBeforeInsert>(HandleBeforeInsert);
				events.Subscribe<DynamicDBBeforeUpdate>(HandleBeforeUpdate);
				events.Subscribe<DynamicDBBeforeDelete>(HandleBeforeDelete);
			}
		}

		private void HandleBeforeInsert(DynamicDBBeforeInsert e)
		{
			var matchingPlugins = GetByDataCatalogID(e.CatalogID);
			foreach (var p in matchingPlugins)
			{
				if (p.ImplementsOnInsert)
					(p as CatalogPluginInfo).plugin.BeforeInsert(e);
			}
		}

		private void HandleBeforeUpdate(DynamicDBBeforeUpdate e)
		{
			var matchingPlugins = GetByDataCatalogID(e.CatalogID);
			foreach (var p in matchingPlugins)
			{
				if (p.ImplementsOnUpdate)
					(p as CatalogPluginInfo).plugin.BeforeUpdate(e);
			}
		}

		private void HandleBeforeDelete(DynamicDBBeforeDelete e)
		{
			var matchingPlugins = GetByDataCatalogID(e.CatalogID);
			foreach (var p in matchingPlugins)
			{
				if(p.ImplementsOnDelete)
					(p as CatalogPluginInfo).plugin.BeforeDelete(e);
			}
		}

		private void LoadAssemblyPlugins(string filename)
		{
			var asm = Assembly.LoadFrom(filename);
			var types = asm.GetTypes();
			foreach(var t in types)
			{
				if (typeof(CatalogPlugin).IsAssignableFrom(t) && !t.IsAbstract)
				{
					AddPlugin(filename, t);
				}
			}
		}

		private void AddPlugin(string assemblyName, Type t)
		{
			try
			{
				var info = GetPluginInfo(t);
				lock (syncObj)
					plugins.Add(info);
			}
			catch(Exception ex)
			{
				log.LogException($"Error loading {t.Name} form {assemblyName}.", ex);
			}
		}

		private ICatalogPluginInfo GetPluginInfo(Type t)
		{
			var plugin = factory.GetInstance(t) as CatalogPlugin;
			var info = new CatalogPluginInfo(t, plugin);
			info.PluginName = t.FullName;
			var descriptionAttr = t.GetCustomAttribute<Description>();
			if (descriptionAttr != null)
				info.PluginDescription = descriptionAttr.Text;
			var targetAttr = t.GetCustomAttribute<CatalogPluginTargetAttribute>();
			if (targetAttr == null)
				throw new Exception("Plugin is missing the [CatalogPluginTarget(...)] attribute.");
			info.CompanyName = targetAttr.Company;
			info.BrandName = targetAttr.Brand;
			info.ProjectName = targetAttr.Project;
			info.CatalogName = targetAttr.Catalog;
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				info.CompanyID = (from c in ctx.Companies where c.Name == info.CompanyName select c.ID).Single();
				info.BrandID = (from b in ctx.Brands where b.Name == info.BrandName && b.CompanyID == info.CompanyID select b.ID).Single();
				info.ProjectID = (from p in ctx.Projects where p.Name == info.ProjectName && p.BrandID == info.BrandID select p.ID).Single();
				var catalog = (from c in ctx.Catalogs where c.Name == info.CatalogName && c.ProjectID == info.ProjectID select c).Single();
				info.CatalogID = catalog.ID;
				info.DataCatalogID = catalog.CatalogID;
			}
			info.ImplementsOnInsert = Reflex.OverridesMethod(t, "BeforeInsert");
			info.OnInsertDescription = Reflex.GetMethodDescription(t, "BeforeInsert");
			info.ImplementsOnUpdate = Reflex.OverridesMethod(t, "BeforeUpdate");
			info.OnUpdateDescription = Reflex.GetMethodDescription(t, "BeforeUpdate");
			info.ImplementsOnDelete = Reflex.OverridesMethod(t, "BeforeDelete");
			info.OnDeleteDescription = Reflex.GetMethodDescription(t, "BeforeDelete");
			return info;
		}

		public List<ICatalogPluginInfo> GetByCompanyID(int companyid)
		{
			lock (syncObj)
				return plugins.Where(p => p.CompanyID == companyid).ToList();
		}

		public List<ICatalogPluginInfo> GetByBrandID(int brandid)
		{
			lock (syncObj)
				return plugins.Where(p => p.BrandID == brandid).ToList();
		}

		public List<ICatalogPluginInfo> GetByProjectID(int projectid)
		{
			lock (syncObj)
				return plugins.Where(p => p.ProjectID == projectid).ToList();
		}

		public List<ICatalogPluginInfo> GetByCatalogID(int catalogid)
		{
			lock (syncObj)
				return plugins.Where(p => p.CatalogID == catalogid).ToList();
		}

		public List<ICatalogPluginInfo> GetByDataCatalogID(int datacatalogid)
		{
			lock (syncObj)
				return plugins.Where(p => p.DataCatalogID == datacatalogid).ToList();
		}
	}


	class CatalogPluginInfo : ICatalogPluginInfo
	{
		internal Type type;
		internal CatalogPlugin plugin;

		public CatalogPluginInfo(Type type, CatalogPlugin plugin)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (plugin == null)
				throw new ArgumentNullException(nameof(plugin));
			this.type = type;
			this.plugin = plugin;
		}

		public int CompanyID { get; set; }
		public string CompanyName { get; set; }
		public int BrandID { get; set; }
		public string BrandName { get; set; }
		public int ProjectID { get; set; }
		public string ProjectName { get; set; }
		public int CatalogID { get; set; }
		public string CatalogName { get; set; }
		public int DataCatalogID { get; set; }
		public string PluginName { get; set; }
		public string PluginDescription { get; set; }
		public bool ImplementsOnInsert { get; set; }
		public string OnInsertDescription { get; set; }
		public bool ImplementsOnUpdate { get; set; }
		public string OnUpdateDescription { get; set; }
		public bool ImplementsOnDelete { get; set; }
		public string OnDeleteDescription { get; set; }
	}
}
