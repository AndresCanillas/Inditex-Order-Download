using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

//using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
    public partial class AllocateEpcsTempe
    {
        public void AddPrintPackageData(IPrintPackage printPackage, int orderid, string orderNumber, List<TableData> variableData)
        {
            var orderStatus = repo.GetOrder(orderid);
            if(orderStatus == null)
                orderStatus = CreateOrderRecord(orderid, orderNumber);

            if(orderStatus.AllocationStatus == AllocationStatus.Pending)
            {
                if(config.EncodingApi is EpcApi epcApi)
                    ValidateEncodingOrder(epcApi, orderStatus, variableData);
                else if(config.EncodingApi is PreencodingApi preencodingApi)
                    ProcessPreencodingOrder(preencodingApi, orderStatus, variableData);
                else
                    throw new NotSupportedException("Selected Encoding API is not supported");
            }

            if(orderStatus.AllocationStatus == AllocationStatus.Validated)
            {
                if(config.EncodingApi is EpcApi epcApi)
                    AllocateEncodingOrder(epcApi, orderStatus);
                else if(config.EncodingApi is PreencodingApi preencodingApi)
                    AllocatePreencodingOrder(preencodingApi, orderStatus);
                else
                    throw new NotSupportedException("Selected Encoding API is not supported");
            }

            var data = new PrintPackageEpcInfo()
            {
                Order = orderStatus,
                Details = repo.GetOrderDetails(orderid),                    // empty if preencoding process
                PreencodingDetails = repo.GetPreencodingDetails(orderid),   // empty if normal encoding process
                Locks = repo.GetLockInfo(orderid)
            };

            printPackage.AddFile("Data/epcs.json", JsonConvert.SerializeObject(data));
        }

        public void ExtractPrintPackageData(IPrintPackage printPackage)
        {
            var fileContent = printPackage.GetFile("Data/epcs.json");
            var data = JsonConvert.DeserializeObject<PrintPackageEpcInfo>(fileContent);
            var order = repo.GetOrder(data.Order.OrderID);
            if(order == null)
            {
                repo.InsertOrder(data.Order);
                if(data.Details != null && data.Details.Count > 0)
                    repo.InsertOrderDetails(data.Order.OrderID, data.Details);
                if(data.PreencodingDetails != null && data.PreencodingDetails.Count > 0)
                    repo.InsertPreencodingOrderDetails(data.Order.OrderID, data.PreencodingDetails);
                repo.InsertLockInfo(data.Locks);
            }
            else
            {
                var orderDetails = repo.GetOrderDetails(data.Order.OrderID);
                foreach(var detail in data.Details)
                {
                    var existing = orderDetails.Where(p => p.DetailID == detail.DetailID).FirstOrDefault();
                    if(existing == null)
                        repo.InsertOrderDetail(detail);
                    else
                        repo.UpdateOrderDetail(detail);
                }

                var preencodingDetails = repo.GetPreencodingDetails(data.Order.OrderID);
                foreach(var detail in data.PreencodingDetails)
                {
                    var existing = preencodingDetails.Where(p => p.DetailID == detail.DetailID).FirstOrDefault();
                    if(existing == null)
                        repo.InsertPreencodingOrderDetail(detail);
                    else
                        repo.UpdatePreencodingOrderDetail(detail);
                }

                var lockInfo = repo.GetLockInfo(order.OrderID);
                if(lockInfo == null)
                    repo.InsertLockInfo(data.Locks);
                else
                    repo.UpdateLockInfo(data.Locks);
            }
        }


        #region Normal Encoding Process

        private void ValidateEncodingOrder(EpcApi api, OrderStatus order, List<TableData> variableData)
        {
            var modelQualityPairs = new Dictionary<string, string>();
            List<OrderDetail> details = GetOrderDetailsFromOrderData(api, order.OrderID, variableData);

            foreach(var detail in details)
            {
                var key = $"{detail.Model}/{detail.Quality}";
                var existing = repo.GetOrderDetail(order.OrderID, detail.DetailID);
                if(existing == null)
                {
                    if(!modelQualityPairs.ContainsKey(key))
                    {
                        var validationResponse = epcService.ValidateOrder(new ValidateOrderRequest()
                        {
                            PurchaseOrder = order.OrderNumber,
                            Model = detail.Model,
                            Quality = detail.Quality
                        });

                        if(validationResponse.HasErrors())
                            throw new Exception($"Error while validating order {order.OrderNumber}, TempeEpcService errors: \r\n {validationResponse.GetErrors()}");

                        modelQualityPairs.Add(key, null);
                    }

                    repo.InsertOrderDetail(detail);
                }
            }

            order.AllocationStatus = AllocationStatus.Validated;
            order.AllocationDate = DateTime.Now;
            repo.UpdateOrder(order);
        }

        private List<OrderDetail> GetOrderDetailsFromOrderData(EpcApi api, int orderid, List<TableData> orderData)
        {
            var result = new List<OrderDetail>();

            var detailsCatalog = orderData.FirstOrDefault(p => p.Name == "OrderDetails");
            if(detailsCatalog == null)
                throw new Exception($"OrderDetails Catalog could not be found.");

            var variableDataCatalog = orderData.FirstOrDefault(p => p.Name == "VariableData");
            if(variableDataCatalog == null)
                throw new Exception($"VariableData Catalog could not be found.");

            var details = JArray.Parse(detailsCatalog.Records);
            var variableData = JArray.Parse(variableDataCatalog.Records);
            foreach(JObject row in details)
            {
                var detailID = row.GetValue<int>("ID");
                var quantity = row.GetValue<int>("Quantity");
                var productID = row.GetValue<int>("Product");
                var productData = variableData.First(p => (p as JObject).GetValue<int>("ID") == productID) as JObject;
                var model = api.Config.Model.GetValue<int>(productData);
                var quality = api.Config.Quality.GetValue<int>(productData);
                var color = api.Config.Color.GetValue<int>(productData);
                var size = api.Config.Size.GetValue<int>(productData);
                var tagType = api.Config.TagType.GetValue<int>(productData);
                var tagSubType = api.Config.TagSubType.GetValue<int>(productData);
                result.Add(new OrderDetail()
                {
                    OrderID = orderid,
                    DetailID = detailID,
                    Quantity = quantity,
                    Model = model,
                    Quality = quality,
                    Color = color,
                    Size = size,
                    TagType = tagType,
                    TagSubType = tagSubType
                });
            }
            return result;
        }

        private void AllocateEncodingOrder(EpcApi epcApi, OrderStatus order)
        {
            var orderDetails = repo.GetOrderDetails(order.OrderID);
            foreach(var detail in orderDetails)
            {
                if(detail.Allocated)
                    continue;

                if(detail.RfidRequest == 0)
                {
                    var allocateResponse = epcService.AllocateEpcs(new AllocateEpcsRequest()
                    {
                        PurchaseOrder = order.OrderNumber,
                        SupplierId = config.SuppliedId,
                        TagType = detail.TagType,
                        TagSubType = detail.TagSubType,
                        Model = detail.Model,
                        Quality = detail.Quality,
                        Colors = new List<ColorInfo>()
                        {
                            new ColorInfo()
                            {
                                ColorCode = detail.Color,
                                QuantityBySize = new List<QuantityBySizeInfo>()
                                {
                                    new QuantityBySizeInfo()
                                    {
                                        SizeCode = detail.Size,
                                        Quantity = 1, // The objective here is ensure Tempe will respond without giving a validation error in future calls
									}
                                }
                            }
                        }
                    });

                    if(allocateResponse == null || allocateResponse.Count() == 0)
                        throw new Exception($"Error while allocating epcs for order {order.OrderNumber}, Model/Quality/Color/Size: {detail.Model}/{detail.Quality}/{detail.Color}/{detail.Size}");

                    detail.RfidRequest = allocateResponse.ElementAt(0).RfidRequestId;
                    repo.UpdateOrderDetail(detail);
                }

                GetEpcs(order, detail.RfidRequest, detail.DetailID);

                detail.Allocated = true;
                repo.UpdateOrderDetail(detail);
            }
            order.AllocationStatus = AllocationStatus.Allocated;
            order.AllocationDate = DateTime.Now;
            repo.UpdateOrder(order);
        }

        #endregion


        #region Preencoding Process

        private void ProcessPreencodingOrder(PreencodingApi api, OrderStatus order, List<TableData> variableData)
        {
            var modelQualityPairs = new Dictionary<string, string>();
            List<PreencodingOrderDetail> details = GetPreencodingOrderDetailsFromOrderData(api, order.OrderID, variableData);
            foreach(var detail in details)
            {
                var existing = repo.GetPreencodingDetail(order.OrderID, detail.DetailID);
                if(existing == null)
                    repo.InsertPreencodingOrderDetail(detail);
            }

            order.AllocationStatus = AllocationStatus.Validated;
            order.AllocationDate = DateTime.Now;
            repo.UpdateOrder(order);
        }

        private List<PreencodingOrderDetail> GetPreencodingOrderDetailsFromOrderData(PreencodingApi api, int orderid, List<TableData> orderData)
        {
            var result = new List<PreencodingOrderDetail>();

            var detailsCatalog = orderData.FirstOrDefault(p => p.Name == "OrderDetails");
            if(detailsCatalog == null)
                throw new Exception($"OrderDetails Catalog could not be found.");

            var variableDataCatalog = orderData.FirstOrDefault(p => p.Name == "VariableData");
            if(variableDataCatalog == null)
                throw new Exception($"VariableData Catalog could not be found.");

            var details = JArray.Parse(detailsCatalog.Records);
            var variableData = JArray.Parse(variableDataCatalog.Records);
            foreach(JObject row in details)
            {
                var detailID = row.GetValue<int>("ID");
                var quantity = row.GetValue<int>("Quantity");
                var productID = row.GetValue<int>("Product");
                var productData = variableData.First(p => (p as JObject).GetValue<int>("ID") == productID) as JObject;
                var brandId = api.Config.BrandId.GetValue<int>(productData);
                var productType = api.Config.ProductType.GetValue<int>(productData);
                var color = api.Config.Color.GetValue<int>(productData);
                var size = api.Config.Size.GetValue<int>(productData);
                var tagType = api.Config.TagType.GetValue<int>(productData);
                var tagSubType = api.Config.TagSubType.GetValue<int>(productData);
                result.Add(new PreencodingOrderDetail()
                {
                    OrderID = orderid,
                    DetailID = detailID,
                    Quantity = quantity,
                    BrandId = brandId,
                    ProductTypeCode = productType,
                    Color = color,
                    Size = size,
                    TagType = tagType,
                    TagSubType = tagSubType
                });
            }
            return result;
        }

        private void AllocatePreencodingOrder(PreencodingApi api, OrderStatus order)
        {
            var details = repo.GetPreencodingDetails(order.OrderID);
            foreach(var detail in details)
            {
                if(detail.Allocated)
                    continue;

                if(detail.RfidRequest == 0)
                {
                    var request = new PreEncodeRequest()
                    {
                        BrandId = detail.BrandId,
                        ProductTypeCode = detail.ProductTypeCode,
                        ColorCode = detail.Color,
                        QuantityBySize = new List<QuantityBySizeInfo>()
                        {
                            new QuantityBySizeInfo()
                            {
                                SizeCode = detail.Size,
                                Quantity = 1, // This call is just to ensure Tempe will not give a validation error in the future...
							}
                        },
                        TagType = detail.TagType,
                        TagSubType = detail.TagSubType,
                        SupplierId = config.SuppliedId,
                    };

                    var response = epcService.PreEncode(request);
                    if(response == null)
                        throw new Exception($"Error while preecoding epcs for order {order.OrderNumber}, Request: {JsonConvert.SerializeObject(request)}");

                    detail.RfidRequest = response.RfidRequestId;
                    repo.UpdatePreencodingOrderDetail(detail);
                }

                GetEpcs(order, detail.RfidRequest, detail.DetailID);

                detail.Allocated = true;
                repo.UpdatePreencodingOrderDetail(detail);
            }
            order.AllocationStatus = AllocationStatus.Allocated;
            order.AllocationDate = DateTime.Now;
            repo.UpdateOrder(order);
        }

        #endregion


        private OrderStatus CreateOrderRecord(int orderid, string orderNumber)
        {
            OrderStatus orderStatus = new OrderStatus()
            {
                OrderID = orderid,
                OrderNumber = orderNumber,
                AllocationStatus = AllocationStatus.Pending,
                AllocationDate = DateTime.Now
            };
            repo.InsertOrder(orderStatus);
            return orderStatus;
        }

        private void CreateLockRecord(OrderStatus order, SecurityBits securityBits)
        {
            var locks = repo.GetLockInfo(order.OrderID);
            if(locks == null)
            {
                locks = new LockInfo()
                {
                    OrderID = order.OrderID,
                    EpcLock = GetLockValue(securityBits.EpcLock, securityBits.EpcPermaLock),
                    UserMemoryLock = GetLockValue(securityBits.UserMemoryLock, securityBits.UserMemoryPermaLock),
                    KillPasswordLock = GetLockValue(securityBits.KillPasswordLock, securityBits.KillPasswordPermaLock),
                    AccessPasswordLock = GetLockValue(securityBits.AccessPasswordLock, securityBits.AccessPasswordPermaLock)
                };
                repo.InsertLockInfo(locks);
            }
        }

        private RFIDLockType GetLockValue(int lockFlag, int permaLockFlag)
        {
            if(permaLockFlag == 1)
                return RFIDLockType.PermaLock;
            else if(lockFlag == 1)
                return RFIDLockType.Lock;
            else
                return RFIDLockType.Mask;
        }

        private void GetEpcs(OrderStatus order, int rfidRequestId, int detailid)
        {
            var read = repo.GetOrderDetailEpcCount(order.OrderID, detailid);
            var locks = repo.GetLockInfo(order.OrderID);
            var lockRecordCreated = locks != null;
            GetEpcsResponse response;

            do
            {
                response = epcService.GetEpcs(new GetEpcsRequest()
                {
                    RfidRequestId = rfidRequestId,
                    Offset = read,
                    Limit = 1000
                });

                if(response == null)
                    throw new Exception($"Error while reading EPCs from Epc Service for order {order.OrderNumber}, DetailID {detailid}.");

                if(!lockRecordCreated)
                {
                    CreateLockRecord(order, response.MetadataTagPage.SecurityBits);
                    lockRecordCreated = true;
                }

                if(response.Results.Count == 0)
                    throw new Exception("TempeEpcService did not return any EPC records");

                read += response.Results.Count;
            } while(read < response.MetadataTagPage.ResultSet.Total);
        }
    }


    public class PrintPackageEpcInfo
    {
        public OrderStatus Order { get; set; }
        public List<OrderDetail> Details { get; set; }
        public List<PreencodingOrderDetail> PreencodingDetails { get; set; }
        public LockInfo Locks { get; set; }
    }
}
