using Service.Contracts;
using SmartdotsPlugins.Compostion.Abstractions;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class ArticleCompositionConfigManager : ArticleCompositionConfigurationBase
    {
        public ArticleCompositionConfigManager(IFactory factory) : base(factory)
        {
        }
        public override ArticleCompositionConfig Retrieve(string artCode, int projectID)
        {
            return base.Retrieve(artCode, projectID);
        }
    }
}
