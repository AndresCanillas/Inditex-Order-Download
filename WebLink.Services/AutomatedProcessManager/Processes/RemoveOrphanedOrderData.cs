using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
    public class RemoveOrphanedOrderData : IAutomatedProcess
    {
        private IFactory factory;
        private IConnectionManager connManager;
        private IAppConfig config;
        private ILogService log;

        private CancellationTokenSource cts;
        private ManualResetEvent waitHandle;

        public RemoveOrphanedOrderData(IFactory factory, IConnectionManager connManager, IAppConfig config, ILogService log)
        {
            this.factory = factory;
            this.connManager = connManager;
            this.config = config;
            this.log = log;
            waitHandle = new ManualResetEvent(false);
            cts = new CancellationTokenSource();
        }

        public TimeSpan GetIdleTime()
        {
            return TimeSpan.MaxValue;
        }

        public void OnLoad()
        {
            _ = StartLoop();
        }

        public void OnUnload()
        {
            cts.Cancel();
            waitHandle.WaitOne(1000);
            cts.Dispose();
        }

        public void OnExecute()
        {
        }

        private async Task StartLoop()
        {
            while(!cts.Token.IsCancellationRequested)
            {
                await DeleteOrphanedOrders();
                await Task.Delay(TimeSpan.FromMinutes(5), cts.Token);
            }
            waitHandle.Set();
        }

        private async Task DeleteOrphanedOrders()
        {
            try
            {
                var mainDB = connManager.GetInitialCatalog("MainDB");
                var connStr = config.GetValue<string>("Databases.CatalogDB.ConnStr");
                using(var dynamicDB = factory.GetInstance<DynamicDB>())
                {
                    await dynamicDB.OpenAsync(connStr);
                    dynamicDB.Conn.CommandTimeout = 12000;
                    var allProjects = await dynamicDB.Conn.SelectAsync<Project>($"select * from [{mainDB}].dbo.Projects");
                    foreach(var project in allProjects)
                    {

                        try
                        {
                            LogDetailed($"Iniciando Limpieza DATA Projecto {project.ID}");

                            var projectCatalogs = await dynamicDB.Conn.SelectAsync<Catalog>($"select * from [{mainDB}].dbo.Catalogs where ProjectID = @projectid", project.ID);

                            if(projectCatalogs.Count == 0)
                            {
                                log.LogWarning("Project without Catalogs - ProjectID: [{0}]", project.ID);
                                continue;
                            }

                            var catalogDefinitions = (
                                from c in projectCatalogs
                                select new CatalogDefinition()
                                {
                                    ID = c.CatalogID,
                                    Name = c.Name,
                                    Definition = c.Definition,
                                    IsReadonly = c.IsReadonly,
                                    IsHidden = c.IsHidden,
                                    CatalogType = c.CatalogType
                                })
                            .ToList();

                            var ordersCatalog = catalogDefinitions.First(c => c.Name == "Orders");

                            await dynamicDB.RecursiveDeleteAsync(catalogDefinitions, ordersCatalog, $@"
                        SELECT TOP 10 o.ID FROM #TABLE o WITH(NOLOCK)
                        LEFT JOIN [{mainDB}].dbo.CompanyOrders co WITH(NOLOCK) ON o.ID = co.OrderDataID
                        WHERE
                        o.OrderDate < @date
                        AND co.ID IS NULL
						", DateTime.Now.AddDays(-1));

                            LogDetailed($"Terminada Limpieza DATA Projecto {project.ID}");

                        }
                        catch(Exception _ex1)
                        {
                            LogDetailed($"Error limpiando DATA projecto ID {project.ID}");
                            log.LogException($"Error limpiando DATA projecto ID {project.ID}", _ex1);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                log.LogException($"Error Ejecutando Proceso RemoveOrphans", ex);
            }
        }

        private void LogDetailed(string msg)
        {
            var ld = log.GetSection("RemoveOrphans");

            ld.LogMessage(msg);
        }
    }

    public class OrphanedOrder
    {
        public int ID;
        public string OrderNumber;
    }
}
