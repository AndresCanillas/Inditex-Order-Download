using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;
using WebLink.Contracts.Services;
using WebLink.Services.Sage;

namespace WebLink.Services
{
    public class OrderRegisterInERP : IOrderRegisterInERP
    {
        private IFactory factory;
        private IAppConfig config;
        private ILogSection log;
        private IAddressRepository addressRepo;
        private IArtifactRepository artifactRepo;
        private ICountryRepository countryRepo;
        private IDBConnectionManager connManager;
        private IERPCompanyLocationRepository erpConfigRepo;
        private ILocationRepository factoryRepo;
        private IOrderRepository orderRepo;
        private IOrderLogRepository orderLog;
        private IProviderRepository providerRepo;
        private ISageClientService sageClient;
        private IOrderGroupRepository orderGroupRepo;
        private IOrderWithArticleDetailedService orderWithArticleDetailedService;

        public OrderRegisterInERP(
            IFactory factory,
            IAppConfig config,
            ILogService log,
            IAddressRepository addressRepo,
            IArtifactRepository artifactRepo,
            ICountryRepository countryRepo,
            IDBConnectionManager connManager,
            IERPCompanyLocationRepository erpConfigRepo,
            ILocationRepository factoryRepo,
            IOrderRepository orderRepo,
            IOrderLogRepository orderLog,
            IProviderRepository providerRepo,
            ISageClientService sageClient,
            IOrderGroupRepository orderGroupRepo,
            IOrderWithArticleDetailedService orderWithArticleDetailedService)
        {
            this.factory = factory;
            this.orderRepo = orderRepo;
            this.sageClient = sageClient;
            this.orderLog = orderLog;
            this.connManager = connManager;
            this.providerRepo = providerRepo;
            this.addressRepo = addressRepo;
            this.countryRepo = countryRepo;
            this.factoryRepo = factoryRepo;
            this.artifactRepo = artifactRepo;
            this.config = config;
            this.log = log.GetSection("ERP");
            this.erpConfigRepo = erpConfigRepo;
            this.orderGroupRepo = orderGroupRepo;
            this.orderWithArticleDetailedService = orderWithArticleDetailedService;
        }

        public bool DisabledBilling { get => config.GetValue<bool>("WebLink.DisableBilling"); }


        public bool CanBill(OrderInfoDTO orderInfo)
        {
            if(orderInfo.IsBillable == false || orderInfo.IsBilled == true)
            {
                return false;
            }

            return true;
        }


        public void MarkAsBilled(OrderInfoDTO orderInfo)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                MarkAsBilled(ctx, orderInfo);
            }
        }


        public void MarkAsBilled(PrintDB ctx, OrderInfoDTO orderInfo)
        {
            orderRepo.ChangeStatus(ctx, orderInfo.OrderID, OrderStatus.Billed);
        }

        public int Execute(int orderGroupID, int orderID, string orderNumber, int projectID, int brandID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                //var orderLog = sp.GetRequiredService<IOrderLogService>();
                var orderInfo = orderRepo.GetBillingInfo(ctx, orderID);
                //var billingFactory = GetBillingLocation(ctx, orderInfo);
                var erpConfig = erpConfigRepo.GetByOrder(ctx, orderInfo);

                log.LogMessage($"OrderID [{orderInfo.OrderID}] - OrderStatus: [{orderInfo.OrderStatus.GetText()}] - ERPInstanceID: [{erpConfig.ERPInstanceID}] - IsBillable: [{orderInfo.IsBillable}] - IsBilled:[{orderInfo.IsBilled}] - ProductionType: [{orderInfo.ProductionType}] - BillingSyncWithSage: [{orderInfo.BillToSyncWithSage}] - SageRef: [{orderInfo.BillToSageRef}]");

                if(CheckOrderStatus(orderInfo) != true)
                {
                    // cancel event, order is not in correct status
                    return -1;
                }

                if(!CanBill(orderInfo))
                {
                    orderLog.Debug(orderID, $"Order will not be registered in the ERP, Order IsBillable [{orderInfo.IsBillable}],  Order alredy Billed [{orderInfo.IsBilled}],  Enable Billing: [{!DisabledBilling}]");
                    return 1; // billing disabled for this order, continue
                }

                // check if billto is registered in Sage 
                if(
                    erpConfig.ERPInstanceID == 1 &&
                    orderInfo.ProductionType == ProductionType.IDTLocation &&
                    orderInfo.BillToSyncWithSage == true &&
                    !string.IsNullOrEmpty(orderInfo.BillToSageRef))
                {
                    var ok = false;

                    //try
                    //{
                    var registeredOrder = RegisterInSageByGroup(ctx, orderInfo, erpConfig);
                    if(!string.IsNullOrEmpty(registeredOrder.SageReference))
                    {
                        MarkAsBilled(ctx, orderInfo);
                        orderLog.Debug(ctx, orderID, "Order Registered in ERP");
                    }
                    ok = true;

                    if(!ok)
                    {
                        return -2;
                    }

                }
                else
                {
                    // No register in ERP
                    UpdateOrderReference(ctx, orderInfo);
                    MarkAsBilled(ctx, orderInfo);
                    orderLog.Debug(ctx, orderID, "Order no require to be registered in ERP");
                }

                return 0;
            }
        }

        public IOrder UpdateOrderReference(OrderInfoDTO orderInfo)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return UpdateOrderReference(ctx, orderInfo);
            }
        }

        /// <summary>
        /// Update individually Line order MDReference
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="orderInfo"></param>
        /// <returns></returns>
        public IOrder UpdateOrderReference(PrintDB ctx, OrderInfoDTO orderInfo)
        {
            // No register in ERP
            var oData = orderRepo.GetByID(ctx, orderInfo.OrderID, true);
            oData.MDOrderNumber = GetMDReference(ctx, orderInfo, new List<string>());
            string projectPrefix = GetProjectCodeSharedByGroup(ctx, orderInfo);
            oData.ProjectPrefix = projectPrefix;
            oData = orderRepo.Update(ctx, oData);

            // Update Order on Print_Data
            // TODO: quest to the team about this requirement

            // ignore ITEMS, item orders not required Variable DATA, hardcode - ordergroupid = 0 for items from intakeworflow
            if(orderInfo.OrderDataID != 0)
            {

                try
                {
                    var catalogRepo = factory.GetInstance<ICatalogRepository>();
                    var orderCatalog = catalogRepo.GetByProjectID(ctx, orderInfo.ProjectID, true).FirstOrDefault(x => x.Name == "Orders");

                    using(var dynamicDB = connManager.CreateDynamicDB())
                    {
                        var order = dynamicDB.SelectOne(orderCatalog.CatalogID, orderInfo.OrderDataID);
                        order["MDOrderNumber"] = oData.MDOrderNumber;
                        dynamicDB.Update(orderCatalog.CatalogID, Newtonsoft.Json.JsonConvert.SerializeObject(order));
                    }
                }
                catch(Exception ex)
                {
                    log.LogException($"No se puedo registrar MD-ORDERNUMBER en PrintDATA OrderID [{orderInfo.OrderID}]", ex);
                    throw;
                }
            }

            return oData;
        }

        public bool CheckOrderStatus(OrderInfoDTO orderInfo)
        {
            var canBillStates = new List<OrderStatus>() { OrderStatus.Validated, OrderStatus.Billed, OrderStatus.ProdReady, OrderStatus.Printing, OrderStatus.Completed };

            if(!canBillStates.Contains(orderInfo.OrderStatus))
            {
                return false;
            }

            return true;
        }

        public string GetMDReference(OrderInfoDTO orderInfo, IEnumerable<string> articleCodes)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetMDReference(ctx, orderInfo, articleCodes);
            }
        }

        public string GetMDReference(PrintDB ctx, OrderInfoDTO orderInfo, IEnumerable<string> articleCodes)
        {
            var identifier = orderInfo.OrderID.ToString("D6");

            List<string> keys = new List<string>() {
                orderInfo.BillToCompanyCode ,
                orderInfo.OrderNumber,
                identifier
            };

            var ret = string.Join('-', keys.Where(w => !string.IsNullOrEmpty(w)).ToArray());
            int max = 35; // project field in sage

            if(ret.Length > max) // sage max length
            {
                orderLog.Debug(ctx, orderInfo.OrderID, $"MDReference was truncated, [{ret}] has a length of [{ret.Length}] characters, is greater than [{max}] allowed for SAGE");
                ret = ret.Substring(0, max);
            }

            return ret;
        }

        public string GetProjectCodeShared(OrderInfoDTO orderInfo)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetProjectCodeShared(ctx, orderInfo);
            }
        }

        public string GetProjectCodeShared(PrintDB ctx, OrderInfoDTO orderInfo)
        {
            var identifier = orderInfo.OrderID.ToString("D6");

            List<string> keys = new List<string>() {
                orderInfo.CompanyCode,
                orderInfo.ProjectCode,
                identifier
            };

            var ret = string.Join('-', keys.Where(w => !string.IsNullOrEmpty(w)).ToArray());
            var max = 20;// sage max length

            if(ret.Length > 20)
            {

                var startIdx = ret.Length - 20;// keep ordergroup id, to avoid losing identifier
                ret = ret.Substring(startIdx, max);

                if(ret != orderInfo.MDOrderNumber)
                    orderLog.Debug(ctx, orderInfo.OrderID, $"Shared ProjectCode [{ret}] has a length of [{ret.Length}] characters, is greater than [{max}] allowed");
            }

            return ret;
        }

        #region Register in group

        private IOrder RegisterInSageByGroup(PrintDB ctx, OrderInfoDTO orderInfo, IERPCompanyLocation erpConfig)
        {
            var groupRecord = orderGroupRepo.GetByID(orderInfo.OrderGroupID);


            // find inner all order from the same group a SageReference open to add line
            var soh = GetOpenedOrderInERP(ctx, orderInfo, erpConfig);



            // new or update
            if(soh == null)
            {
                return NewOrder(ctx, orderInfo, erpConfig, groupRecord);
            }
            else
            {
                return UpdateOrder(ctx, orderInfo, erpConfig, groupRecord, soh);
            }
        }

        private ISageOrder GetOpenedOrderInERP(PrintDB ctx, OrderInfoDTO orderInfo, IERPCompanyLocation erpConfig)
        {
            var filter = new OrderInGroupFilter()
            {
                OrderGroupID = orderInfo.OrderGroupID,
                SageOrderStatus = SageOrderStatus.Open,
                LocationID = orderInfo.LocationID
                //OrderNumber = orderInfo.OrderNumber
            }; //new OrderInGroupFilter(orderInfo.OrderGroupID, SageOrderStatus.Open);
            ISageOrder sageOrder = null;
            SageOrderStatus sohStatus = SageOrderStatus.Unknow;
            var currentDate = DateTime.Now;

            var orders = orderGroupRepo.GetAllErpOrderReferencesInGroup(ctx, filter).ToList();

            var factoriesWithSplitOrders = new List<int> { 1, 25 };

            if(orders.Count() == 0) return null;

            for(int i = 0; i < orders.Count; i++)
            {
                var e = orders[i];

                var currentFound = sageClient.GetOrderDetailAsync(e.SageReference).Result;

                if(currentFound != null)
                {
                    sohStatus = string.IsNullOrEmpty(currentFound.OrderStatus) ? SageOrderStatus.Unknow : (SageOrderStatus)Enum.Parse(typeof(SageOrderStatus), currentFound.OrderStatus);

                    if(sohStatus == SageOrderStatus.Open)
                    {
                        // reutilice open order
                        if(!factoriesWithSplitOrders.Contains(erpConfig.ProductionLocationID))
                        {
                            sageOrder = currentFound;
                            break; // BREAK Foreach current sage order is a open and valid order to add more lines
                        }
                        // TODO: create a strategy behavior inner ERPConfiguration to handle this conditio to avoid harcode factory ID
                        if(currentFound.OrderDate == currentDate.ToString("yyyyMMdd"))
                        {
                            sageOrder = currentFound;
                            break;// BREAK Foreach - Is a valid Order For Especial Factories that only use open orders inner the same day "yyyyMMdd"
                        };

                    }
                }
            }

            return sageOrder; // return null or  sage order found

        }

        //2021_inlay_isaac_I01
        // Update due date
        public IOrder UpdateDueDate(PrintDB ctx, OrderInfoDTO orderInfo)
        {
            var groupRecord = orderGroupRepo.GetByID(orderInfo.OrderGroupID);
            // awaitable method
            ISageOrder sageOrder = sageClient.GetOrderDetailAsync(groupRecord.SageReference).Result;

            // Create a New Order with Empty Data for only send an update message with requried fields and full list of articles
            ISageOrder toUpdateOrder = new Soh() { Param = new ParamObject() };

            var orderRecord = orderRepo.GetByID(ctx, orderInfo.OrderID, true);

            toUpdateOrder.CreatedFrom = sageOrder.CreatedFrom;
            toUpdateOrder.SalesFactory = sageOrder.SalesFactory;
            toUpdateOrder.OrderDate = sageOrder.OrderDate;
            toUpdateOrder.CustomerReference = sageOrder.CustomerReference;
            toUpdateOrder.CustomerOrderReference = sageOrder.CustomerOrderReference;
            toUpdateOrder.ProyectSuffix = sageOrder.ProyectSuffix;
            toUpdateOrder.DeliveryDate = orderInfo.DueDate.Value.ToString("yyyyMMdd");

            sageClient.UpdateOrderItemsAsync(toUpdateOrder, sageOrder.Reference).GetAwaiter().GetResult();

            //return orderRepo.Update(ctx, orderRecord);
            return orderRecord;
        }

        // Update Order, Add new item or update one
        private IOrder UpdateOrder(PrintDB ctx, OrderInfoDTO orderInfo, IERPCompanyLocation erpConfig, IOrderGroup groupRecord, ISageOrder sageOrder)
        {

            // awaitable method
            //ISageOrder sageOrder = sageClient.GetOrderDetailAsync(groupRecord.SageReference).Result;

            // Create a New Order with Empty Data for only send an update message with requried fields and full list of articles
            ISageOrder toUpdateOrder = new Soh() { Param = new ParamObject() };

            var orderRecord = orderRepo.GetByID(ctx, orderInfo.OrderID, true);

            toUpdateOrder.CreatedFrom = sageOrder.CreatedFrom;
            toUpdateOrder.SalesFactory = sageOrder.SalesFactory;
            toUpdateOrder.OrderDate = sageOrder.OrderDate;
            toUpdateOrder.CustomerReference = sageOrder.CustomerReference;
            toUpdateOrder.CustomerOrderReference = sageOrder.CustomerOrderReference;
            toUpdateOrder.ProyectSuffix = sageOrder.ProyectSuffix;
            toUpdateOrder.DeliveryDate = orderInfo.DueDate.Value.ToString("yyyyMMdd");

            //toUpdateOrder.
            var currentItems = sageOrder.GetItems();

            // add Items - update all items, keeping the order with the original articles
            // is possible
            AddItemToSageOrder(ctx, orderInfo, erpConfig, groupRecord, toUpdateOrder);

            if(DisabledBilling)
            {
                return orderRecord;
            }

            // -- force line number manually to update single line
            var tbl = toUpdateOrder.Param.Tables.Find(t => t.Id.Equals("SOH4_1")); // table of items

            tbl.Lines.ForEach(line => line.Num = (int.Parse(line.Num) + currentItems.Count()).ToString());// fix number line, for the current item and artifacts

            var sohResponse = sageClient.UpdateOrderItemsAsync(toUpdateOrder, sageOrder.Reference).GetAwaiter().GetResult();

            //orderRecord.MDOrderNumber = sageOrder.CustomerOrderReference;
            orderRecord.SageReference = sohResponse.Reference;// line number in SAGE article list
            orderRecord.SyncWithSage = true;
            orderRecord.SageStatus = (SageOrderStatus)Enum.Parse(typeof(SageOrderStatus), sohResponse.OrderStatus);
            orderRecord.InvoiceStatus = (SageInvoiceStatus)Enum.Parse(typeof(SageInvoiceStatus), sohResponse.Invoice);
            orderRecord.DeliveryStatus = (SageDeliveryStatus)Enum.Parse(typeof(SageDeliveryStatus), sohResponse.Delivered);
            orderRecord.CreditStatus = (SageCreditStatus)Enum.Parse(typeof(SageCreditStatus), sohResponse.Credit);
            orderRecord.RegisteredOn = DateTime.Now;
            orderRecord.ProjectPrefix = sageOrder.ProyectSuffix;

            groupRecord.SageReference = sohResponse.Reference;
            groupRecord.SyncWithSage = true;
            //groupRecord.ERPReference = string.IsNullOrEmpty(groupRecord.ERPReference) ? sageOrder.CustomerOrderReference : groupRecord.ERPReference;
            orderGroupRepo.Update(groupRecord);

            return orderRepo.Update(ctx, orderRecord);

        }

        // register new order in SAGE
        private IOrder NewOrder(PrintDB ctx, OrderInfoDTO orderInfo, IERPCompanyLocation erpConfig, IOrderGroup groupRecord)
        {
            string mdReference = GetMDReferenceByGroup(ctx, orderInfo, new List<string>());
            string projectPrefix = GetProjectCodeSharedByGroup(ctx, orderInfo);
            var orderRecord = orderRepo.GetByID(ctx, orderInfo.OrderID, true);

            groupRecord.ERPReference = string.IsNullOrEmpty(groupRecord.ERPReference) ? mdReference : groupRecord.ERPReference;

            ISageOrder sageOrder = new Soh() { Param = new ParamObject() };
            sageOrder.CreatedFrom = "WEB";
            sageOrder.SalesFactory = erpConfig.BillingFactoryCode;
            sageOrder.ExpeditionFactory = erpConfig.ProductionFactoryCode;
            sageOrder.RevisionNum = "0";
            sageOrder.OrderDate = DateTime.Now.ToString("yyyyMMdd");
            sageOrder.CustomerReference = orderInfo.BillToSageRef;
            sageOrder.CustomerOrderReference = groupRecord.ERPReference;
            sageOrder.ProyectSuffix = projectPrefix;
            sageOrder.DeliveryDate = orderInfo.DueDate.Value.ToString("yyyyMMdd");
            //sageOrder.ShipmentDate = null; // leave empty
            if(!string.IsNullOrEmpty(erpConfig.ExpeditionAddressCode))
            {
                sageOrder.ExpeditionAddressReference = erpConfig.ExpeditionAddressCode;
            }
            sageOrder.Currency = !string.IsNullOrEmpty(erpConfig.Currency) ? erpConfig.Currency : "EUR";

            // add Delivery Address
            var shippingTo = addressRepo.GetByID(ctx, orderInfo.SendToAddressID.Value);
            var country = countryRepo.GetByID(ctx, shippingTo.CountryID);

            var space = " ";

            ISageAddress sageAddress = new Add();
            sageAddress.Description = shippingTo.Name;
            sageAddress.Line1 = string.IsNullOrEmpty(shippingTo.AddressLine1) ? space : shippingTo.AddressLine1;
            sageAddress.Line2 = !string.IsNullOrEmpty(shippingTo.AddressLine2) ? shippingTo.AddressLine2 : space;
            sageAddress.Line3 = !string.IsNullOrEmpty(shippingTo.AddressLine3) ? shippingTo.AddressLine3 : space;
            sageAddress.City = !string.IsNullOrEmpty(shippingTo.CityOrTown) ? shippingTo.CityOrTown : space;
            sageAddress.CountryCode = country.Alpha2;
            sageAddress.ZipCode = !string.IsNullOrEmpty(shippingTo.ZipCode) ? shippingTo.ZipCode : space;
            sageAddress.BusinessName1 = !string.IsNullOrEmpty(shippingTo.BusinessName1) ? shippingTo.BusinessName1 : space;
            sageAddress.BusinessName2 = !string.IsNullOrEmpty(shippingTo.BusinessName2) ? shippingTo.BusinessName2 : space;
            sageAddress.Email1 = !string.IsNullOrEmpty(shippingTo.Email1) ? shippingTo.Email1 : space;
            sageAddress.Email2 = !string.IsNullOrEmpty(shippingTo.Email2) ? shippingTo.Email2 : space;
            sageAddress.Telephone1 = !string.IsNullOrEmpty(shippingTo.Telephone1) ? shippingTo.Telephone1 : space;
            sageAddress.Telephone2 = !string.IsNullOrEmpty(shippingTo.Telephone2) ? shippingTo.Telephone2 : space;

            if(erpConfig.DeliveryAddressID.HasValue)
            {
                var erpAdd = addressRepo.GetByID(erpConfig.DeliveryAddressID.Value, true);
                sageOrder.DeliveryBillingCode = erpAdd.SageRef;
            }

            sageOrder.SetDeliveryAddress(sageAddress);

            // add Items
            AddItemToSageOrder(ctx, orderInfo, erpConfig, groupRecord, sageOrder);

            if(DisabledBilling)
            {
                return orderRecord;
            }

            var sohResponse = sageClient.RegisterOrder(sageOrder).GetAwaiter().GetResult();

            //orderRecord.MDOrderNumber = mdReference;
            orderRecord.SageReference = sohResponse.Reference;// line number in SAGE article list
            orderRecord.SyncWithSage = true;
            orderRecord.SageStatus = (SageOrderStatus)Enum.Parse(typeof(SageOrderStatus), sohResponse.OrderStatus);
            orderRecord.InvoiceStatus = (SageInvoiceStatus)Enum.Parse(typeof(SageInvoiceStatus), sohResponse.Invoice);
            orderRecord.DeliveryStatus = (SageDeliveryStatus)Enum.Parse(typeof(SageDeliveryStatus), sohResponse.Delivered);
            orderRecord.CreditStatus = (SageCreditStatus)Enum.Parse(typeof(SageCreditStatus), sohResponse.Credit);
            orderRecord.RegisteredOn = DateTime.Now;
            orderRecord.ProjectPrefix = projectPrefix;

            groupRecord.SageReference = sohResponse.Reference;
            groupRecord.SyncWithSage = true;


            orderGroupRepo.Update(groupRecord);

            return orderRepo.Update(ctx, orderRecord);
        }

        private static void AddItemDetailedArticle(string wsReference, string customerRef, int quantity, ISageOrder sageOrder, OrderDetailDTO a)
        {
            ISageRequestItem itm = new SageRequestItem();
            itm.SetReference(a.SageReference);
            itm.SetCustomerRef(customerRef);
            itm.SetQuantity(quantity); // TODO: add sale of unit of the article, now default is "MIL"
            itm.SetWsReference(wsReference);
            sageOrder.AddItem(itm);
        }

        public void AddItemToSageOrder(PrintDB ctx, OrderInfoDTO orderInfo, IERPCompanyLocation erpConfig, IOrderGroup groupRecord, ISageOrder sageOrder)
        {

            orderWithArticleDetailedService.Execute(orderInfo.OrderID, orderInfo.SendToCompanyID,
                                                    (wsReference, customerref, quantity, a) => AddItemDetailedArticle(wsReference, customerref, quantity, sageOrder, a),
                                                    (quantity, a) => AddItemNotDetailedArticle(quantity, ctx, orderInfo, sageOrder, a),
                                                    (quantity, a, line) => AddArtifacts(ctx, orderInfo, sageOrder, a, quantity, line));


        }

        private void AddArtifacts(PrintDB ctx, OrderInfoDTO orderInfo, ISageOrder sageOrder, OrderDetailDTO a, int total, int line = 0)
        {
            // add artifacts
            var artifacts = artifactRepo.GetByArticle(ctx, a.ArticleID).ToList();

            if(artifacts != null || artifacts.Count > 0)
            {
                var startingLine = sageOrder.MaxNumberItemLines(orderInfo.OrderID.ToString());
                //foreach (var artifact in artifacts)
                for(int artPos = startingLine; artPos < startingLine + artifacts.Count(); artPos++)
                {
                    var artifact = artifacts.ElementAt(artPos - startingLine);

                    if(!artifact.SyncWithSage)
                    {
                        continue;
                    }

                    ISageRequestItem artifactItm = new SageRequestItem();
                    artifactItm.SetReference(artifact.SageRef);
                    artifactItm.SetCustomerRef(artifact.Name);
                    artifactItm.SetQuantity(total);
                    artifactItm.SetWsReference($"{orderInfo.OrderID.ToString()}-{(artPos + 1)}");
                    sageOrder.AddItem(artifactItm);
                }
            }
        }

        private void AddItemNotDetailedArticle(int quantity, PrintDB ctx, OrderInfoDTO orderInfo, ISageOrder sageOrder, OrderDetailDTO a)
        {
            ISageRequestItem itm = new SageRequestItem();

            itm.SetReference(a.SageReference);
            itm.SetCustomerRef(a.Article);
            itm.SetQuantity(quantity); // TODO: add sale of unit of the article, now default is "MIL"
            itm.SetWsReference(orderInfo.OrderID.ToString());
            sageOrder.AddItem(itm);


        }

        public string GetMDReferenceByGroup(PrintDB ctx, OrderInfoDTO orderInfo, IEnumerable<string> articleCodes)
        {
            var identifier = orderInfo.OrderGroupID.ToString("D6");

            List<string> keys = new List<string>() {
                orderInfo.BillToCompanyCode ,
                orderInfo.OrderNumber,
                $"G{identifier}"
            };

            var ret = string.Join('-', keys.Where(w => !string.IsNullOrEmpty(w)).ToArray());
            int max = 35; // project field in sage

            if(ret.Length > max) // sage max length
            {
                orderLog.Debug(ctx, orderInfo.OrderID, $"MDReference was truncated, [{ret}] has a length of [{ret.Length}] characters, is greater than [{max}] allowed for SAGE");
                var startIdx = ret.Length - max;// keep ordergroup id, to avoid losing identifier
                ret = ret.Substring(0, max);
            }

            return ret;

        }

        public string GetProjectCodeSharedByGroup(PrintDB ctx, OrderInfoDTO orderInfo)
        {
            var identifier = orderInfo.OrderGroupID.ToString("D6");

            List<string> keys = new List<string>() {
                orderInfo.CompanyCode,
                orderInfo.ProjectCode,
                $"G{identifier}"
            };

            var ret = string.Join('-', keys.Where(w => !string.IsNullOrEmpty(w)).ToArray());
            var max = 20;

            if(ret.Length > 20)// sage max length
            {
                orderLog.Debug(ctx, orderInfo.OrderID, $"Shared ProjectCode [{ret}] has a length of [{ret.Length}] characters, is greater than [{max}] allowed");
                var startIdx = ret.Length - max;// keep ordergroup id, to avoid losing identifier
                ret = ret.Substring(startIdx, max);
            }

            return ret;
        }

        #endregion Register in group

    }
}

