using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;

namespace SmartdotsPlugins.NiceThings.PackPlugins
{
    [FriendlyName("NiceThings - Additional Articles Pack Plugin")]
    [Description("Reads ARTICULO_ADICIONAL_X and adds aggregated standard item labels.")]
    public class NiceThingsAdditionalArticlesPackPlugin : AbstractPackArticlesPlugin, IPackArticlesPlugin
    {
        private const string ADD_PREFIX = "Details.Product.IsBaseData.ARTICULO_ADICIONAL_";

        public override void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {
            var qtyCol =
                data.GetColumnByName("Details.Quantity", throwIfNotFound: false)
                ?? data.GetColumnByName("Details.Product.IsBaseData.CANTIDAD", throwIfNotFound: false);

            if(qtyCol == null)
                return;

            var addCols = GetAdditionalColsDynamic(data);
            if(addCols.Count == 0)
                return;

            var acc = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            data.ForEach(row =>
            {
                var qtyRaw = row.GetValue(qtyCol)?.ToString();
                if(!int.TryParse(qtyRaw, out int qty) || qty <= 0)
                    return;

                foreach(var col in addCols)
                {
                    var code = row.GetValue(col)?.ToString()?.Trim();

                    if(string.IsNullOrWhiteSpace(code) || code == "0")
                        continue;

                    if(!acc.ContainsKey(code))
                        acc[code] = 0;

                    acc[code] += qty;
                }
            });

            foreach(var kv in acc)
                AddArticles(new List<string> { kv.Key }, articleCodes, kv.Value);
        }

        private static List<ImportedCol> GetAdditionalColsDynamic(ImportedData data)
        {
            var cols = new List<ImportedCol>();

            int i = 1;
            int missesInARow = 0;

            const int MAX_MISSES_IN_A_ROW = 3; // tolerar huecos
            const int MAX_SAFE_LIMIT = 200;    // límite para no hacer loop infinito

            while(i <= MAX_SAFE_LIMIT && missesInARow < MAX_MISSES_IN_A_ROW)
            {
                var col = data.GetColumnByName(ADD_PREFIX + i, throwIfNotFound: false);

                if(col != null)
                {
                    cols.Add(col);
                    missesInARow = 0;
                }
                else
                {
                    missesInARow++;
                }

                i++;
            }

            return cols;
        }

        public override void Dispose() { }
    }
}
