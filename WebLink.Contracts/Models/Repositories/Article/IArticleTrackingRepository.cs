using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IArticleTrackingRepository
    {
        IArticleTracking GetById(int id);
        List<ArticleTrackingInfo> GetTrackedArticles(int page, int pageSize, string search, int factoryID);
        List<ArticleTrackingInfo> GetUntrackedArticles(int page, int pageSize, string search);
        void RemoveArticle(int articleId);
        void AddArticle(int articleId, DateTime initialDate, string username);
        int GetTrackedArticlesCount(string search);
        int GetUntrackedArticlesCount(string search);
        void ResetDate(int articleId, DateTime initialDate, string username);
    }
}
