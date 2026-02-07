using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class ArticleCodeExtractorBase
    {
        private IPrinterJobRepository printerJobRepo;
        private IArticleRepository articleRepo;
        protected ArticleCodeExtractorBase(IPrinterJobRepository printerJobRepo, IArticleRepository articleRepo)
        {
            this.printerJobRepo = printerJobRepo;
            this.articleRepo = articleRepo;
        }


        public virtual string Extract(int orderId, int articleID = 0)
        {
            // Try to get article code from specific article ID first
            if (articleID > 0)
            {
                var articleCode = TryExtractArticleCode(articleID);
                if (!string.IsNullOrEmpty(articleCode))
                {
                    return articleCode;
                }
            }

            // Fallback: get article code from printer jobs
            var printerJobs = printerJobRepo.GetByOrderID(orderId, true);
            if (printerJobs == null)
            {
                return string.Empty;
            }

            foreach (var job in printerJobs)
            {
                var articleCode = TryExtractArticleCode(job.ArticleID);
                if (!string.IsNullOrEmpty(articleCode))
                {
                    return articleCode;
                }
            }

            return string.Empty;
        }

        private string TryExtractArticleCode(int articleId)
        {
            if (articleId <= 0)
            {
                return string.Empty;
            }

            var article = articleRepo.GetByID(articleId);
            if (article == null || string.IsNullOrEmpty(article.ArticleCode))
            {
                return string.Empty;
            }

            return ExtractBaseArticleCode(article.ArticleCode);
        }

        private static string ExtractBaseArticleCode(string articleCode)
        {
            if (string.IsNullOrEmpty(articleCode))
            {
                return string.Empty;
            }

            var underscoreIndex = articleCode.LastIndexOf("_");
            return underscoreIndex > 0 
                ? articleCode.Substring(0, underscoreIndex) 
                : articleCode;
        }
    }
}
