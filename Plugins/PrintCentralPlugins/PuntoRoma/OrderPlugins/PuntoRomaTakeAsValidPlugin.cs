using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.OrderPlugins
{
    [FriendlyName("PuntoRoma.TakeAsValidPlugin")]
    [Description("PuntoRoma.TakeAsValidPlugin")]
    public class PuntoRomaTakeAsValidPlugin: IOrderSetValidatorPlugin
    {

        private readonly IOrderRepository orderRepo;
        private readonly IConnectionManager connManager;
        private readonly ICatalogRepository catalogRepo;
        private static readonly List<string> TAKE_AS_VALID_PACKTYPE = new List<string>(capacity: 2) { "RO", "AC" };

        public PuntoRomaTakeAsValidPlugin(
            IOrderRepository orderRepo,
            
            IConnectionManager connManager,
            ICatalogRepository catalogRepo)
        {
            this.orderRepo = orderRepo;
            this.connManager = connManager;
            this.catalogRepo = catalogRepo;
        }

        public int TakeAsValidated(OrderPluginData orderData)
        {
            var allCatalogs = catalogRepo.GetByProjectID(orderData.ProjectID);
            var orderCt = allCatalogs.Single(s => s.Name == Catalog.ORDER_CATALOG);
            var detailCt = allCatalogs.Single(s => s.Name == Catalog.ORDERDETAILS_CATALOG);
            var varDataCt = allCatalogs.Single(s => s.Name == Catalog.VARIABLEDATA_CATALOG);
            var baseDataCt = allCatalogs.Single(s => s.Name == Catalog.BASEDATA_CATALOG);

            var order = orderRepo.GetByID(orderData.OrderID);

            var relField = orderCt.Fields.Single(s => s.Name == "Details");


            var takeAsValid = 0;// NO -> validation required

            using(var dynamicDB = connManager.OpenDB("CatalogDB"))
            {
                var productData = dynamicDB.Select<VariableDataDTO>(
                    $@"SELECT v.ID, v.PackType
                    FROM {orderCt.TableName} o
                    INNER JOIN REL_{orderCt.CatalogID}_{detailCt.CatalogID}_{relField.FieldID} as r1 ON o.ID = r1.SourceID
                    INNER JOIN {detailCt.TableName} d ON d.ID = r1.TargetID
                    INNER JOIN {varDataCt.TableName} v ON v.ID = d.Product
                    WHERE o.ID = @orderID",
                    order.OrderDataID);



                var grouped = productData.GroupBy(x => x.PackType).ToList();

                if(grouped.Count() != 1) throw new Exception($"Punto Roma Orders can't contains mixed PackType: [{string.Join(',', grouped.Select(s => s.Key))}]. There is {grouped.Count()} PacksType.");


                var packType = grouped.First().Key;

                takeAsValid = (TAKE_AS_VALID_PACKTYPE.Contains(packType)) ? 1 : 0;

            }

            return takeAsValid;
        }

        public void Dispose()
        {

        }
    }


    public class VariableDataDTO
    {
        public int ID;
        public string PackType;
    }
}
