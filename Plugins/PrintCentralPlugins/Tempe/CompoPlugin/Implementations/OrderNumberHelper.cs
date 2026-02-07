using Service.Contracts.Database;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Tempe.CompoPlugin.Implementations
{
    public static class OrderNumberHelper
    {
        public static string GetOrderNumberOld(int orderGroupID, Catalog fullTranslatedCompositionCatalog, DynamicDB dynDb)
        {



            var orderNumber =  dynDb.Select(
                fullTranslatedCompositionCatalog.CatalogID,
                $"SELECT TOP (1) OrderNumber FROM #TABLE o WHERE o.OrderGroupID=@OrderGroupID",
                orderGroupID
            );

            return orderNumber[0]["OrderNumber"]?.ToString();
        }
    }
}
