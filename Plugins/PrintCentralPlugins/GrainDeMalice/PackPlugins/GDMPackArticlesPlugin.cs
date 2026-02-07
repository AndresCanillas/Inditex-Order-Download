using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartdotsPlugins.GrainDeMalice.PackPlugins
{
    [FriendlyName("GDM - Pack Articles Plugin")]
    [Description("GDM - PackArticlesPlugin")]
    public class GDMPackArticlesPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
            var labelWithoutHeart = "GMHT";
            var labelWithHeart = "GHMT-V";

            var greenHeart = data.GetTargetColumnByName("Details.Product.IsBaseData.ADZN26");
            var detailsQuantityColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ADETIQ");
            var detailsArticleCodeColumn = data.GetTargetColumnByName("Details.ArticleCode");

            if (string.IsNullOrEmpty(detailsArticleCodeColumn?.ToString()) == false)
            {
                data.ForEach((r) =>
                {
                    var extraItem = r.GetValue(greenHeart).ToString().ToUpper();
                    var quantity = r.GetValue(detailsQuantityColumn).ToString().ToUpper();
                    var articleCode = r.GetValue(detailsArticleCodeColumn).ToString().ToUpper();

                    if (extraItem == "0")
                        AddArticles(new List<string> { labelWithoutHeart }, articleCodes, Convert.ToInt32(quantity));
                    if (extraItem == "1")
                        AddArticles(new List<string> { labelWithHeart }, articleCodes, Convert.ToInt32(quantity));

                });

            }
        }
        public override void Dispose(){}

    }
}
