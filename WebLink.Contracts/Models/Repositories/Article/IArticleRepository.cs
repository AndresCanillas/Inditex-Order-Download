using System;
using System.Collections.Generic;
using System.IO;

namespace WebLink.Contracts.Models
{
    public interface IArticleRepository : IGenericRepository<IArticle>
    {
        ArticleViewModel GetFullArticle(int id);
        List<ArticleViewModel> GetFullByProjectID(ArticleByProjectFilter filter);

        List<ArticleWithLabelDTO> GetArticlesWithLabels(List<ArticleWithLabelDTO> articles, int projectID);
        List<ArticleWithLabelDTO> GetArticlesWithLabels(PrintDB ctx, List<ArticleWithLabelDTO> articles, int projectID);

        List<PackArticleViewModel> GetByPackID(int packid);
        List<PackArticleViewModel> GetByPackID(PrintDB ctx, int packid);

        IArticle GetByCode(string code);
        IArticle GetByCode(PrintDB ctx, string code);

        List<IArticle> GetByProjectID(int projectid);
        List<IArticle> GetByProjectID(PrintDB ctx, int projectid);

        IArticle GetByCodeInProject(string code, int projectID);
        IArticle GetByCodeInProject(PrintDB ctx, string code, int projectID);

        void SetArticlePreview(int id, byte[] imageContent);
        Stream GetArticlePreview(int id);
        Stream GetFixedArticlePreview(int id);
        Guid GetArticlePreviewReference(int id);

        IEnumerable<IArticle> GetRegisteredInSage();
        IEnumerable<IArticle> GetRegisteredInSage(PrintDB ctx);

        IArticle GetBySageReference(string reference, int projectID);
        IArticle GetBySageReference(PrintDB ctx, string reference, int projectID);

        IArticle GetSharedByCode(string articlecode);
        IArticle GetSharedByCode(PrintDB ctx, string articlecode);

        IArticle GetDefaultArticle();
        IArticle GetDefaultArticle(PrintDB ctx);

        IEnumerable<IArticle> GetArticleByLabelType(RequestLabelType rq);
        IEnumerable<IArticle> GetArticleByLabelType(PrintDB ctx, RequestLabelType rq);

        IEnumerable<IArticle> GetArticleCanIncludeCompo(RequestLabelType rq);
        IEnumerable<IArticle> GetArticleCanIncludeCompo(PrintDB ctx, RequestLabelType rq);


        IEnumerable<ArticleInfoDTO> GetArticlesInfo(int projectID);
        IEnumerable<ArticleInfoDTO> GetArticlesInfo(PrintDB ctx, int projectID);
        IEnumerable<ArticleInfoDTO> GetArticlesByCompanyId(int companyId);


        IEnumerable<IArticle> GetByOrder(int orderID);
        IEnumerable<IArticle> GetByOrder(PrintDB ctx, int orderID);

        ArticleCompositionConfig GetArticleCompositionConfig(int projectid, int articleid);
        void SaveArticleComposition(ArticleCompositionConfig articleCompositionConfig, string usernane);

        List<ArticleCompositionConfig> GetCompositionConfigByProjectID(int projectID);

        ArticleAccessBlockConfig GetArticleAccessBlockConfig(int articleId);
        void SaveArticleAccessBlockConfig(ArticleAccessBlockConfig config, string username);
        bool HasExportAccessBlocked(int articleId, int locationId);

    }
}
