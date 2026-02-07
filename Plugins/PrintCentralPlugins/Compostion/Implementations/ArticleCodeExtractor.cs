using SmartdotsPlugins.Compostion.Abstractions;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class ArticleCodeExtractor : ArticleCodeExtractorBase
    {
        public ArticleCodeExtractor(IPrinterJobRepository printerJobRepo, IArticleRepository articleRepo) : base(printerJobRepo, articleRepo)
        {
        }

        public override string Extract(int orderId, int articleID = 0)
        {
            return base.Extract(orderId, articleID);
        }
    }
}
