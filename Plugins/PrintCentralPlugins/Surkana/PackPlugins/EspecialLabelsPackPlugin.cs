using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartdotsPlugins.PackPlugins.Surkana
{
    [FriendlyName("Surkana - Especial Labels I, II, III")]
    [Description("Surkana - Especial Labels I, II, III")]
    public class EspecialLabelsPackPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        public EspecialLabelsPackPlugin() { }
        public override void Dispose() { }

        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
            var specialIColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ETIQ_ESP_I");
            var specialIIColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ETIQ_ESP_II");
            var specialIIIColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ETIQ_ESP_III");
            var coinColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.Coin");
            var quantityColumn = data.GetTargetColumnByName("Details.Quantity");
            var coinArticleCode = "10"; // CHAPA METALICA SURKANA 11X11MM 10

            data.ForEach((r) =>
            {
                var specialI = r.GetValue(specialIColumn).ToString().ToUpper();
                var specialII = r.GetValue(specialIIColumn).ToString().ToUpper();
                var specialIII = r.GetValue(specialIIIColumn).ToString().ToUpper();
                var coin = r.GetValue(coinColumn).ToString().ToUpper();
                var quantity = r.GetValue(quantityColumn).ToString().ToUpper();
                

                if (!string.IsNullOrEmpty(specialI))
                    AddArticles(new List<string> { specialI }, articleCodes, Convert.ToInt32(quantity));

                if (!string.IsNullOrEmpty(specialII))
                    AddArticles(new List<string> { specialII }, articleCodes, Convert.ToInt32(quantity));

                if (!string.IsNullOrEmpty(specialIII))
                    AddArticles(new List<string> { specialIII }, articleCodes, Convert.ToInt32(quantity));

                if (!string.IsNullOrEmpty(coin))
                    AddArticles(new List<string> { coinArticleCode }, articleCodes, Convert.ToInt32(quantity));

            });


        }
    }
}
