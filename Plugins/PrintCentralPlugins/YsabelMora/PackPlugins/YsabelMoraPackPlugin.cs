using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;

namespace SmartdotsPlugins.YsabelMora.PackPlugins
{
    [FriendlyName("Ysabel Mora - Especial Labels I, II, III")]
    [Description("Ysabel Mora - Especial Labels I, II, III")]
    public class YsabelMoraPackPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        public YsabelMoraPackPlugin()
        {
        }

        public override void Dispose() { }
        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
            var specialIColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ETIQ_ESP_I");
            var specialIIColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ETIQ_ESP_II");
            var specialIIIColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.ETIQ_ESP_III");
            var quantityColumn = data.GetTargetColumnByName("Details.Quantity");

            data.ForEach((r) =>
            {
                var specialI = r.GetValue(specialIColumn).ToString().ToUpper();
                var specialII = r.GetValue(specialIIColumn).ToString().ToUpper();
                var specialIII = r.GetValue(specialIIIColumn).ToString().ToUpper();
                var quantity = r.GetValue(quantityColumn).ToString().ToUpper();


                if(!string.IsNullOrEmpty(specialI))
                    AddArticles(new List<string> { specialI }, articleCodes, Convert.ToInt32(quantity));

                if(!string.IsNullOrEmpty(specialII))
                    AddArticles(new List<string> { specialII }, articleCodes, Convert.ToInt32(quantity));

                if(!string.IsNullOrEmpty(specialIII))
                    AddArticles(new List<string> { specialIII }, articleCodes, Convert.ToInt32(quantity));


            });


        }
    }
}
