using Service.Contracts;
using System.Linq;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class ArticleCompositionConfigurationBase
    {
        private readonly IFactory factory;

        public ArticleCompositionConfigurationBase(IFactory factory)
        {
            this.factory = factory;
        }
        public virtual ArticleCompositionConfig Retrieve(string artCode, int projectID)
        {
            using(PrintDB context = factory.GetInstance<PrintDB>())
            {
                var article = context.ArticleCompositionConfigs.FirstOrDefault(x => x.ArticleCode == artCode && x.ProjectID == projectID);
                if(article == null)
                {
                    return null;
                }

                return article;
            }
        }
    }
}
