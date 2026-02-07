using Newtonsoft.Json.Linq;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class ExtractSizeRangeService : IExtractSizeRangeService
    {

        private IDBConnectionManager connManager;
        private IFactory factory;

        public ExtractSizeRangeService(IDBConnectionManager connManager, IFactory factory)
        {
            this.connManager = connManager;
            this.factory = factory;
            ctx = factory.GetInstance<PrintDB>();

        }

        private PrintDB ctx;
        [Obsolete("Eliminar este metodo XXXXX - DELETE")]
        public List<string> ExtractOrderSizesListByLines(IEnumerable<OrderDetailDTO> detail, int projectId)
        {
            var listOfSizes = new List<string>();

            using(var dynamicDB = connManager.CreateDynamicDB())
            {
                var detailCatalog = (from c in ctx.Catalogs where c.ProjectID == projectId && c.Name == Catalog.ORDERDETAILS_CATALOG select c).FirstOrDefault();
                var productCatalog = (from c in ctx.Catalogs where c.ProjectID == projectId && c.Name == Catalog.VARIABLEDATA_CATALOG select c).FirstOrDefault();
                var productDataList = detail.Select(s => s.ProductDataID);

                var productDetails = dynamicDB.Select(detailCatalog.CatalogID, $@"
                                            SELECT Details.ID AS ProductDataID ,Product.Barcode,Product.TXT1,Product.Size,Product.Color
                                            FROM #TABLE Details
                                            INNER JOIN {productCatalog.Name}_{productCatalog.CatalogID} Product ON Details.Product = Product.ID
                                            WHERE Details.ID in ({string.Join(",", productDataList.ToArray())})
                                            ");

                foreach(var d in detail)
                {
                    var product = productDetails.Where(w => ((JObject)w).GetValue<int>("ProductDataID").Equals(d.ProductDataID)).First();
                    var size = ((JObject)product).GetValue<string>("Size");
                    if(!string.IsNullOrEmpty(size) && !listOfSizes.Any(s => s == size))
                    {
                        listOfSizes.Add(size);
                    }
                }
            }
            return listOfSizes;

        }

        public List<string> ExtractOrderSizesListByUseInSizes(IEnumerable<OrderDetailDTO> detail, int projectId)
        {
            var listOfSizes = new List<string>();

            using (var dynamicDB = connManager.CreateDynamicDB())
            {
                var detailCatalog = (from c in ctx.Catalogs where c.ProjectID == projectId && c.Name == Catalog.ORDERDETAILS_CATALOG select c).FirstOrDefault();
                var productCatalog = (from c in ctx.Catalogs where c.ProjectID == projectId && c.Name == Catalog.BASEDATA_CATALOG select c).FirstOrDefault();
                var productDataList = detail.Select(s => s.ProductDataID);

                //var productDetails = dynamicDB.Select(detailCatalog.CatalogID, $@"
                //                            SELECT Details.ID AS ProductDataID ,Product.Barcode,Product.TXT1,Product.Size,Product.Color, Product.UseInSize
                //                            FROM #TABLE Details
                //                            INNER JOIN {productCatalog.Name}_{productCatalog.CatalogID} Product ON Details.Product = Product.ID
                //                            WHERE Details.ID in ({string.Join(",", productDataList.ToArray())})
                //                            ");

                var productDetails = dynamicDB.Select(detailCatalog.CatalogID, $@"
                                            SELECT Details.ID AS ProductDataID,Product.UseInSizes
                                            FROM #TABLE Details
                                            INNER JOIN {productCatalog.TableName} Product ON Details.Product = Product.ID
                                            WHERE Details.ID in ({string.Join(",", productDataList.ToArray())})
                                            ");

                foreach (var d in detail)
                {
                    var product = productDetails.Where(w => ((JObject)w).GetValue<int>("ProductDataID").Equals(d.ProductDataID)).First();
                    var sizeRange = ((JObject)product).GetValue<string>("UseInSizes");
                    if (!string.IsNullOrEmpty(sizeRange))
                    {
                        var sizes = sizeRange.Split('-');
                        foreach (var size in sizes)
                        {
                            if (!listOfSizes.Contains(size))
                                listOfSizes.Add(size);
                        }
                    }
                }
            }
            return listOfSizes;
        }
    }
}
