using Service.Contracts;
using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Inditex.Util;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class ArticleCalculatorManager : ArticleCalculatorBase
    {
        public ArticleCalculatorManager(IPrinterJobRepository printerJobRepo, IFactory factory, INotificationRepository notificationRepo, IArticleRepository articleRepo) : base(printerJobRepo, factory, notificationRepo, articleRepo)
        {
        }
        public override List<ArticleSizeCategory> Calculate(ArticleCalulatorParams parameters)
        {
            return base.Calculate(parameters);
        }
    }
}
