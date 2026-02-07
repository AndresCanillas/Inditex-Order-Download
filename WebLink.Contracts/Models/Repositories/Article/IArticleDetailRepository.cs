using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IArticleDetailRepository
    {
        IArticleDetail GetById(int id);
        IEnumerable<IArticleDetail> GetByCompanyId(int companyId);
        ArticleDetailDTO AddArticleDetail(int companyId, int articleId);
        void Delete(int id);
        void DeleteByCompanyId(int companyId, int providerId);
        IEnumerable<ArticleDetailDTO> GetByProviderId(int providerId, int companyId);

    }
}
