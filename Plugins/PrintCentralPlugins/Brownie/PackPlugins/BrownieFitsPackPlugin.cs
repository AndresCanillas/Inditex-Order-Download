using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartdotsPlugins.Brownie.PackPlugins
{
    [FriendlyName("Brownie - Calculate Fits Label")]
    [Description("Brownie - Calculate Fits Label")]
    public class BrownieFitsPackPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        public BrownieFitsPackPlugin() { }

        public override void Dispose() { }

        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {

            var fitsColMapping = data.GetTargetColumnByName("Details.Product.IsBaseData.Fits");
            var quantityColumnMapping = data.GetTargetColumnByName("Details.Quantity");

            data.ForEach((row) =>
            {
                
                var fitsValue = row.GetValue(fitsColMapping).ToString();
                var productCode = GetFitsProductCode(fitsValue);
                var quantity = row.GetValue(quantityColumnMapping).ToString();

                if(!string.IsNullOrEmpty(productCode))
                    AddArticles(new List<string> { productCode }, articleCodes, Convert.ToInt32(quantity));
            });
        }


        private string GetFitsProductCode(string retailProductCode)
        {
            string productCode = string.Empty;
            Dictionary<string, string> articles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) 
            {
                { "VINTAGE", "ETCA007903" },
                { "CAPRI", "ETCA007904" },
                { "CROPPED", "ETCA007898" },
                { "CULOTTE", "ETCA007902" },
                { "FLARE", "ETCA007897" },
                { "LOW RISE", "ETCA007913" },
                { "WOW", "ETCA007899" },
                { "RELAXED", "ETCA007900" },
                { "SKINNY", "ETCA007915" },
                { "SLIM", "ETCA007896" },
                { "STRAIGHT", "ETCA007901" },
                { "WIDE LEG", "ETCA007914" }
            };

            if(articles.TryGetValue(retailProductCode, out productCode))
                return productCode;
            return productCode;
        }
    }
}
