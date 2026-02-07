using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;


namespace WebLink.Services.Wizards
{
    public class AlvaroMorenoOrderValidationService : IAlvaroMorenoOrderValidationService
    {
        private IFactory factory;
        private IOrderRepository orderRepo;
        private IDBConnectionManager connManager;
        private ICatalogRepository catalogRepo;
        private IPrinterJobRepository printerJobRepo;
        private IProjectRepository projectRepository;

        public AlvaroMorenoOrderValidationService(IFactory factory, IOrderRepository orderRepo, IDBConnectionManager connManager, ICatalogRepository catalogRepo, IPrinterJobRepository printerJobRepo, IProjectRepository projectRepository)
        {
            this.factory = factory;
            this.orderRepo = orderRepo;
            this.connManager = connManager;
            this.catalogRepo = catalogRepo;
            this.printerJobRepo = printerJobRepo;
            this.projectRepository = projectRepository;
        }

        public List<OrderGroupSelectionDTO> GetOrderData(PrintDB contextPrintDB, List<OrderGroupSelectionDTO> selection)
        {
            var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Label, ActiveFilter = OrderActiveFilter.Active, OrderStatus = OrderStatusFilter.None };

            var result = orderRepo.GetArticleDetailSelection(contextPrintDB, selection, filter, true, new List<string> {
                "MadeIn",
                "FullMadeIn"
            });

            return result;
        }

        public List<OrderGroupSelectionDTO> GetOrderData(List<OrderGroupSelectionDTO> selection)
        {
            using (var contextPrintDB = factory.GetInstance<PrintDB>())
                return GetOrderData(contextPrintDB, selection);
        }

        public void UpdateOrdersMadeIn(List<OrderGroupQuantitiesDTO> rq)
        {
            using (var contextPrintDB = factory.GetInstance<PrintDB>())
                UpdateOrdersMadeIn(contextPrintDB, rq);
        }

        public void UpdateOrdersMadeIn(PrintDB contextPrintDB, List<OrderGroupQuantitiesDTO> orderGroupQuantitiesList)
        {
            var project = projectRepository.GetByID(orderGroupQuantitiesList[0].ProjectID);
            var orderIDs = new List<int>();

            if (project.AllowUpdateMadeIn == 0)
                return;


            orderGroupQuantitiesList.ForEach(e =>
            {
                if (e.Quantities == null) return;

                orderIDs.AddRange(e.Quantities.Select(s => s.OrderID).Distinct());

                // TODO: create a mehtod of SetVariableData for multiple order at the same time whe all order contain the same value
                orderIDs.ForEach(orderID =>
                {
                    var order = orderRepo.GetByID(orderID);

                    var baseOrder = orderRepo.GetByID(orderID);

                    SetVariableData(contextPrintDB, e, baseOrder);
                });


            });

        }

        private void SetVariableData(PrintDB contextPrintDB, OrderGroupQuantitiesDTO orderGroupQuantities, IOrder baseOrder)
        {
            var catalogs = catalogRepo.GetByProjectID(contextPrintDB, baseOrder.ProjectID, true);
            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            var madeInCatalog = catalogs.First(f => f.Name.Equals("MadeIn"));

            var printerJobs = printerJobRepo.GetByOrderID(baseOrder.ID, true).ToList();

            var printerDetails = contextPrintDB.PrinterJobDetails
                                .Join(contextPrintDB.PrinterJobs, ptjd => ptjd.PrinterJobID, ptj => ptj.ID, (pjd, pj) => new { PrinterJobDetail = pjd, PrinterJob = pj })
                                .Where(w => w.PrinterJob.CompanyOrderID == baseOrder.ID)
                                .Select(s => s.PrinterJobDetail)
                                .ToList();


            using (DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {
                var listfield = madeInCatalog.Fields.Where(x => x.Name != "ID" && x.Name != "IsActive" && x.Name != "English" && x.Name != "Cod");

                // bulk update
                var allIds = printerDetails.Select(s => s.ProductDataID);

                if (allIds.Count() > 0)
                {

                    var jsonMadeIn = dynamicDB.SelectOne(madeInCatalog.CatalogID, int.Parse(orderGroupQuantities.MadeIn));
                    var madeInConcat = "";
                    var concatChar = Environment.NewLine;

                    foreach (var madein in listfield)
                    {
                        madeInConcat += jsonMadeIn.GetValue<string>(madein.ToString()) + concatChar;

                    }

                    dynamicDB.Conn.ExecuteNonQuery(
                       $@"UPDATE v
                    SET
                        MadeIn = @MadeIn,
                        FullMadeIn = @madeInConcat
                    FROM {variableDataCatalog.TableName} v
                    INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                    WHERE d.ID in  ({string.Join(',', allIds)})", orderGroupQuantities.MadeIn, madeInConcat.TrimEnd('\r', '\n'));

                }
            }

        }


    }
}
