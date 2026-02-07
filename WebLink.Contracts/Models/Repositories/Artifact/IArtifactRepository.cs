using System.Collections.Generic;
using WebLink.Contracts.Models.Repositories.Artifact.DTO;

namespace WebLink.Contracts.Models
{
    public interface IArtifactRepository : IGenericRepository<IArtifact>
    {
        IEnumerable<IArtifact> GetByArticle(int articleID, bool loadLabels = false, bool loadArticles = false);
        IEnumerable<IArtifact> GetByArticle(PrintDB ctx, int articleID, bool loadLabels = false, bool loadArticles = false);
        IArtifact AddArtifactToArticle(int artcicleid, int labelid);
        IArtifact AddArtifactToArticle(PrintDB ctx, int artcicleid, int labelid);
        List<ArtifactDTO> GetByArticles(List<int> articleIds, bool loadLabels = false, bool loadArticles = false);
        List<ArtifactDTO> GetByArticles(PrintDB ctx, List<int> articleIds, bool loadLabels = false, bool loadArticles = false);

        IEnumerable<ArticleArtifactDashboardDTO> GetDashboardData(int page, int pageSize, int projectid);

        int GetDashboardDataCount(int projectid);

        IEnumerable<ArticleArtifactDashboardDTO> GetSearchArticlesArtifactsData(int projectID, int page, int pageSize, string value = null);
        int GetSearchArticlesArtifactsCount(int projectID);
    }
}
