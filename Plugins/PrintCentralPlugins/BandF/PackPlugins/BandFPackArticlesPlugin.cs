using System;
using System.Collections.Generic;
using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.OrderPlugins.BandF
{
    [FriendlyName("B&F - Pack Articles Plugin")]
    [Description("B&F - PackArticlesPlugin")]
    public class BandFPackArticlesPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        public BandFPackArticlesPlugin() { }

        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {

            var detailsExtraItemColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ExtraItem1");
            var detailsQuantityColumn = data.GetTargetColumnByName("Details.Quantity");
            var detailsArticleCodeColumn = data.GetTargetColumnByName("Details.ArticleCode");

            
            if (string.IsNullOrEmpty(detailsArticleCodeColumn?.ToString()) == false) {

                data.ForEach((r) =>
                {
                    var extraItem = r.GetValue(detailsExtraItemColumn).ToString().ToUpper();
                    var quantity = r.GetValue(detailsQuantityColumn).ToString().ToUpper();
                    var articleCode = r.GetValue(detailsArticleCodeColumn).ToString().ToUpper();

                    if (string.IsNullOrEmpty(extraItem)) return;

                    AddArticles(new List<string> { extraItem }, articleCodes, Convert.ToInt32(quantity));

                });

            }
            
        }

        public override void Dispose() { }

    }

}
