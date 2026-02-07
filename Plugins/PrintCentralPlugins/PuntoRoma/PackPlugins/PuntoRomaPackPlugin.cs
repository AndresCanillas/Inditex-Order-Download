using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;

namespace SmartdotsPlugins.PuntoRoma.PackPlugins
{
    [FriendlyName("PuntRoma.PackPlugin.AdhesiveQuantityCalculator")]
    [Description("PuntRoma.PackPlugin.AdhesiveQuantityCalculator")]
    public class PuntoRomaPackPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        public override void Dispose() { }
        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
            var quantityColumn = data.GetTargetColumnByName("Details.Quantity");
            var quantityDivisorColumn = data.GetTargetColumnByName("Details.Product.IsBaseData.Pack");
            var grupoColumn = data.GetTargetColumnByName("Details.Product.Grupo");

            //MEXICO = PRADH02, ISRAEL = PRADH05, NATIONAL = PRADH01, ARABIA = PRADH03, INTERNATIONAL = PRADH04, ISRAEL = PRADH05, FRANCIA = PRADH06, ADLER = PRADH07, ITALIA = PRADH08, CENTRAL = PRADH09

            data.ForEach((r) =>
            {
                var quantity = r.GetValue(quantityColumn).ToString().ToUpper();
                var quantityDivisor = r.GetValue(quantityDivisorColumn).ToString().ToUpper();
                var grupo = r.GetValue(grupoColumn).ToString().ToUpper();
                var quantityDivisor2 = Convert.ToInt32(quantityDivisor);
                var sticker = GetStickerName(grupo);
                var total = Convert.ToInt32(quantity);

                if(string.IsNullOrEmpty(sticker))
                {
                    return; // next
                }

                if(quantityDivisor2 != 0)
                {
                    total = Convert.ToInt32(quantity) / quantityDivisor2;
                }

                AddArticles(new List<string> { sticker }, articleCodes, total);

            });
        }

        public string GetStickerName(string countryCode)
        {
            switch(countryCode)
            {
                case "MEXICO":
                    return "PRADH02";
                case "ISRAEL":
                    return "PRADH05";
                case "NATIONAL":
                    return "PRADH01";
                case "ARABIA":
                    return "PRADH03";
                case "INTERNATIONAL":
                    return "PRADH04";
                case "FRANCIA":
                    return "PRADH06";
                case "ADLER":
                    return "PRADH07";
                case "ITALIA":
                    return "PRADH08";
                case "CENTRAL":
                    return "PRADH09";
                default:
                    return "";
            }
        }
    }
}
