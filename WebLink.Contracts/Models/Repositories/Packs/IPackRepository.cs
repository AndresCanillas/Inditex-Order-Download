using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IPackRepository : IGenericRepository<IPack>
    {
        List<IPack> GetByProjectID(int projectid);
        List<IPack> GetByProjectID(PrintDB ctx, int projectid);

        IPack GetByCodeInProject(string code, int projectID);
        IPack GetByCodeInProject(PrintDB ctx, string code, int projectID);

        List<IPackArticle> GetPackArticles(int packid);
        List<IPackArticle> GetPackArticles(PrintDB ctx, int packid);

        List<IPackArticle> GetPackArticlesByType(int packid, PackArticleType itemType);
        List<IPackArticle> GetPackArticlesByType(PrintDB ctx, int packid, PackArticleType itemType);

        PackArticleViewModel AddArticleToPack(int packid, int articleid);
        PackArticleViewModel AddArticleToPack(PrintDB ctx, int packid, int articleid);

        PackArticleViewModel AddArticleByData(int id, int packid, int projectId, string field, string mapping, bool allowEmptyValues);
        PackArticleViewModel AddArticleByData(PrintDB ctx, int id, int packid, int projectId, string field, string mapping, bool allowEmptyValues);

        PackArticleViewModel AddArticleByPlugin(int packid, int projectId, string pluginName);
        PackArticleViewModel AddArticleByPlugin(PrintDB ctx, int packid, int projectId, string pluginName);

        void RemoveArticleFromPack(int packid, int articleid, int id);
        void RemoveArticleFromPack(PrintDB ctx, int packid, int articleid, int id);

        void UpadtePackArticle(int packId, int articleId, int quantity, int id);
        void UpadtePackArticle(PrintDB ctx, int packId, int articleId, int quantity, int id);

        PackArticleViewModel GetPackArticle(int packId, int articleId);
        PackArticleViewModel GetPackArticle(PrintDB ctx, int packId, int articleId);

        PackArticleViewModel GetPackArticleById(int id);
        PackArticleViewModel GetPackArticleById(PrintDB ctx, int id);

        PackArticleConfigDTO GetPackArticleConfig(string packCode, int articleID);
        PackArticleConfigDTO GetPackArticleConfig(PrintDB ctx, string packCode, int articleID);
    }
}
