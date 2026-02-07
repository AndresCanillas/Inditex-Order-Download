using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;

namespace WebLink.Services.Automated
{
    public class CheckSageArticlesProcess : IAutomatedProcess
    {
        IFactory factory;
        IAppConfig appConfig;
        ILogSection appLog;

        private static object syncObj = new object();

        public CheckSageArticlesProcess(IFactory factory, IAppConfig cfg, ILogService appLog)
        {
            this.factory = factory;
            this.appConfig = cfg;
            this.appLog = appLog.GetSection("CheckSageArticles");
        }

        public bool IsActive
        {
            get
            {
                var sageClientActive = appConfig.GetValue<bool>("WebLink.Sage.IsActive", false);
                var processActive = appConfig.GetValue<bool>("WebLink.Sage.Processes.CheckArticles.IsActive", false);
                appLog.LogMessage($"SAGE IsActive [{sageClientActive}]");
                appLog.LogMessage($"Process IsActive [{processActive}]");
                return sageClientActive && processActive;
            }
        }

        public TimeSpan GetIdleTime()
        {
            var delta = appConfig.GetValue<int>("WebLink.Sage.Processes.CheckArticles.FrequencyInSeconds");
            appLog.LogMessage($"Frequency [{delta}]");
            return TimeSpan.FromSeconds(delta);
        }

        public void OnExecute()
        {
            lock (syncObj)
            {
                appLog.LogMessage($"Starting articles SAGET sync IsActive {IsActive}");
                var projectID = appConfig.GetValue<int>("WebLink.Sage.Processes.CheckArticles.ProjectID", 0);
                if (!IsActive && projectID > 0)
                {
                    //appLog.LogWarning($"{this.GetType().Name} - Check Sage Configuration 'WebLink.Sage'  ");
                    return;
                }
                
                var articles = GetArticlesToTrackingAsync().GetAwaiter().GetResult();
                articles = articles.Where(_a => _a.ProjectID == projectID);

                appLog.LogMessage($"Articles to sync: {articles.Count()}");
                var externalInfo = ConsultingExternalItemsAsync(articles).GetAwaiter().GetResult();
                
                UpdateArticleDataAsyn(externalInfo).GetAwaiter().GetResult();
                appLog.LogMessage($"Articles Updated: {articles.Count()}");
            }
        }

        public void OnLoad() {}

        public void OnUnload() {}

        private async Task<IEnumerable<IArticle>> GetArticlesToTrackingAsync()
        {
            var articles = await Task.Run(() => { 
                var articleRepo = factory.GetInstance<IArticleRepository>();
                return articleRepo.GetRegisteredInSage();
            });

            return articles;
        }

        private async Task<List<ArticleRelated>> ConsultingExternalItemsAsync(IEnumerable<IArticle> articles)
        {

            var sageClient = factory.GetInstance<ISageClientService>();
            var pendingTask = new List<string>();
            var itmList = new List<ISageItem>();
            List<ArticleRelated> ret = new List<ArticleRelated>();
            var maxRequest = 5; // TODO: add to config
            ISageItem[] work;

            foreach (var art in articles)
            {
                pendingTask.Add(art.SageRef);

                if (pendingTask.Count >= maxRequest)
                {
                    var tasks = pendingTask.Select(p => BufferCall(p));
                    work = await Task.WhenAll(tasks);
                    itmList.AddRange(work.Where(w => w != null));
                    pendingTask.Clear();
                }
            }

            if (pendingTask.Count > 0)
            {
                var tasks = pendingTask.Select(p => BufferCall(p));
                work = await Task.WhenAll(tasks);
                itmList.AddRange(work.Where(w => w != null));
                pendingTask.Clear();
            }

            foreach (var local in articles)
            {
                var mapped = new ArticleRelated()
                {
                    Local = local,
                    Imported = itmList.FirstOrDefault(f => f.Reference.Equals(local.SageRef))
                };

                ret.Add(mapped);
            }

            return ret;
        }

        private async Task UpdateArticleDataAsyn(List<ArticleRelated> articles)
        {
            var tasks = new List<Task<IArticle>>();
            var maxRequest = 5; // TODO: add to config
            foreach (var art in articles.Where(w => w.Imported != null))
            {
                tasks.Add(Task.Run(() => {
    
                    var repo = factory.GetInstance<IArticleRepository>();
                    // TODO: este codigo se repite en SageController
                    IArticle data = repo.GetByID(art.Local.ID);

                    data.BillingCode = art.Imported.Reference;
                    data.Name = !string.IsNullOrEmpty(art.Imported.SeaKey) ? art.Imported.SeaKey : data.Name;
                    data.Description = art.Imported.Descripcion1;
                    data.SyncWithSage = true;
                    data.SageRef = art.Imported.Reference;

                    if (data.Name != art.Imported.SeaKey && data.Description != art.Imported.Descripcion1) {
                        var articleUpdated = repo.Update(data);
                    }

                    if (art.Imported.HasImage)
                    {
                        
                        repo.SetArticlePreview(art.Local.ID, art.Imported.Image);
                        appLog.LogMessage("Set Image");
                    }
                    appLog.LogMessage("Article Updated With SAGE");
                    return repo.Update(data);
                        
                }));

                if (tasks.Count >= maxRequest) {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

            }

            //await Task.WaitAll(tasks.ToArray());
            await Task.WhenAll(tasks);
        }

        private async Task<ISageItem> BufferCall(string itemSageRef)
        {
            var sageClient = factory.GetInstance<ISageClientService>();

            try
            {
                return await sageClient.GetItemDetail(itemSageRef);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                appLog.LogException($"Error to get Item from SAGE [{itemSageRef}]", e);
                return null; //or whatever default
            }
        }
    }

    


    internal class ArticleRelated
    {
        public IArticle Local { get; set; }

        public ISageItem Imported { get; set; }
    }

    
}
