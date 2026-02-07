using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.LabelService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
	public partial class OrderRepository : GenericRepository<IOrder, Order>, IOrderRepository
	{
        public IComparerConfiguration GetComparerType(int orderId)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetComparerType(ctx, orderId);
			}
		}


		public IComparerConfiguration GetComparerType(PrintDB ctx, int orderId)
		{
			var comparerConfiguration = new ComparerConfiguration();
			var orderInfo = GetProjectInfo(ctx, orderId);

			// verify exist OrderComparer configured
			if (orderInfo == null) return comparerConfiguration;

            // configuration by project
            comparerConfiguration = ctx.ComparerConfiguration
                    .FirstOrDefault(w => w.ProjectID.Equals(orderInfo.ProjectID));

            // configuration by brand
            if (comparerConfiguration == null)
                comparerConfiguration = ctx.ComparerConfiguration
                    .FirstOrDefault(w => w.BrandID.Equals(orderInfo.BrandID));

            // configuration by Company
            if (comparerConfiguration == null)
                comparerConfiguration = ctx.ComparerConfiguration
                    .FirstOrDefault(w => w.CompanyID.Equals(orderInfo.CompanyID));


            if (comparerConfiguration == null)
                comparerConfiguration = new ComparerConfiguration() { Method = ConflictMethod.Default };

            return comparerConfiguration;
        }


		public OrderData GetBaseData(int id, int prevOrderId, bool showDataId)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetBaseData(ctx, id, prevOrderId, showDataId);
			}
		}


		public OrderData GetBaseData(PrintDB ctx, int id, int prevOrderId, bool showDataId)
        {
            var orderData = new OrderData();

            var order = ctx.CompanyOrders.FirstOrDefault(c => c.ID == id);
            var prevOrder = ctx.CompanyOrders.FirstOrDefault(c => c.ID == prevOrderId);
            var catalog = ctx.Catalogs.FirstOrDefault(c => c.ProjectID == order.ProjectID && c.Name.Equals("OrderDetails"));


            // Agregando Article Product Data ID para filtrar la data, se debe obtener la informacion de un articulo , no toda la orden
            // los pedidos que en un archivo traen varios productos se guardan igual en el PrintData pero separados en PrintCentral
            // 2020-12-03, debido a mayoral

            var newDetailOrder =ctx.PrinterJobs
                .Join(ctx.PrinterJobDetails, j => j.ID, d => d.PrinterJobID, (job, detail) => new { PrinterJobs = job, PrinterJobDetails = detail })
                .Where(w => w.PrinterJobs.CompanyOrderID == id)
                .Select(s => s.PrinterJobDetails)
                .ToList();

            var prevDetailOrder = ctx.PrinterJobs
                .Join(ctx.PrinterJobDetails, j => j.ID, d => d.PrinterJobID, (job, detail) => new { PrinterJobs = job, PrinterJobDetails = detail })
                .Where(w => w.PrinterJobs.CompanyOrderID.Equals(prevOrderId))
                .Select(s => s.PrinterJobDetails)
                .ToList();

            if (catalog != null)
            {
                List<TableData> newTables = catalogDataRepo.ExportData(order.ProjectID, "Orders", true, new NameValuePair("ID", order.OrderDataID))
                    .Where(w => !w.Name.StartsWith("REL_")).ToList();

                var newOrderData = newTables.FirstOrDefault(x => x.Name.Equals("OrderDetails"));

                List<TableData> prevTables = catalogDataRepo.ExportData(prevOrder.ProjectID, "Orders", true, new NameValuePair("ID", prevOrder.OrderDataID))
                    .Where(w => !w.Name.StartsWith("REL_")).ToList();

                var prevOrderData = prevTables.FirstOrDefault(x => x.Name.Equals("OrderDetails"));
                log.LogMessage("Get Flatten Object for OrderID {0}", prevOrder.ID);
                orderData.PrevData = GetOrderData(prevOrderData, prevOrder.ProjectID, prevDetailOrder, showDataId);
                log.LogMessage("Get Flatten Object for OrderID {0}", order.ID);
                orderData.NewData = GetOrderData(newOrderData, order.ProjectID, newDetailOrder, showDataId);
            }

            return orderData;
        }


        public void CompareByRow(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates)
        {
            orderCompareService.GetDifferencesByRow(newOrderData, prevOrderData, updates);
        }


		public void CompareByColumn(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates, string column, List<string> insertRows)
        {
            orderCompareService.GetDifferencesByColumn(newOrderData, prevOrderData, updates, column, insertRows);
        }


		public OrderComparerViewModel Compare(int id, int prevOrderId, bool showDataId, int labelId)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return Compare(ctx, id, prevOrderId, showDataId, labelId);
			}
		}


		public OrderComparerViewModel Compare(PrintDB ctx, int id, int prevOrderId, bool showDataId, int labelId)
        {
            var orderData = new OrderData();
            var orderComparerVM = new OrderComparerViewModel();
            var count = 0;

            var comparerConfiguration = GetComparerType(ctx, id);

            var comparerType = comparerConfiguration != null && comparerConfiguration.ID != 0 ? comparerConfiguration.Type : ComparerType.Row;

            orderData = GetBaseData(ctx, id, prevOrderId, showDataId);
            count = orderData.NewData.Count >= orderData.PrevData.Count ? orderData.NewData.Count : orderData.PrevData.Count;

            if (count == 0)
            {
                return orderComparerVM;
            }

            if (comparerType.Equals(ComparerType.Row))
            {
                CompareByRow(orderData.NewData, orderData.PrevData, orderComparerVM.NewDataUpdates);
                CompareByRow(orderData.PrevData, orderData.NewData, orderComparerVM.PrevDataUpdates);
            }
            else if (comparerType.Equals(ComparerType.Column))
            {
                var column = string.Empty;

                if (labelId > 0)
                {
                    var label = ctx.Labels.Where(l => l.ID == labelId).Single();
                     column = "Product." + (label.ComparerField ?? "Barcode");
                }
                else
                {
                    column = "Product.Barcode";
                }

                List<string> prevInsert = new List<string>();
                List<string> newInsert = new List<string>();

                CompareByColumn(orderData.NewData, orderData.PrevData, orderComparerVM.NewDataUpdates, column, prevInsert);
                CompareByColumn(orderData.PrevData, orderData.NewData, orderComparerVM.PrevDataUpdates, column, newInsert);

                foreach (var e in prevInsert)
                {
                    orderComparerVM.PrevDataUpdates.Insert(int.Parse(e), new List<string>() { "dataId" });
                }

                foreach (var e in newInsert)
                {
                    orderComparerVM.NewDataUpdates.Insert(int.Parse(e), new List<string>() { "dataId" });
                }
            }
            
            orderComparerVM.NewData = JsonConvert.SerializeObject(orderData.NewData);
            orderComparerVM.PrevData = JsonConvert.SerializeObject(orderData.PrevData);

            return orderComparerVM;
        }


        private List<Dictionary<string, string>> GetOrderData(TableData data, int projectId, IEnumerable<IPrinterJobDetail> productDetails, bool showDataId = false)
        {
            var dicData = new List<Dictionary<string, string>>();

            if (data != null)
            {
                Stopwatch stopWatch = new Stopwatch();
                TimeSpan ts;
                
                stopWatch.Start();

                var variableDataIds = JArray.Parse(data.Records)
                .Where(r => productDetails.Any(w => w.ProductDataID.Equals( int.Parse(((JObject)r).Property("ID").Value.ToString()) )))
                .Select(s => int.Parse(((JObject)s).Property("ID").Value.ToString()))
                .ToList();

                // TODO: se podria pasar directo el identificador que vienen en productDetails.ProductDataID
                dicData = variableDataRepo.GetAllByDetailID(projectId, true, showDataId, variableDataIds.ToArray()).Select(s => s.Data).ToList();

                stopWatch.Stop();
                ts = stopWatch.Elapsed;
                log.LogMessage("Flattent Object for {0} details - {1}", variableDataIds.Count, string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10));
            }

            return dicData;
        }


        public async Task<Stream> GetComparerPreviews(Guid preview1, Guid preview2, int dataId)
        {
			var tempStore = storeManager.OpenStore("TempStore");
			var resultFile = await tempStore.CreateFileAsync($"Comparer_Temp_{dataId}.png");
            var request = new ComparePreviewsRequest()
            {
                Preview1FileGUID = preview1,
                Preview2FileGUID = preview2,
                ResultFileGUID = resultFile.FileGUID
            };

            var response = await labelService.ComparePreviewsAsync(request);

            if (!response.Success)
            {
                throw new Exception(response.ErrorMessage);
            }
            resultFile = await storeManager.GetFileAsync(resultFile.FileGUID) as IRemoteFile;
			return await resultFile.GetContentAsStreamAsync();
        }

        public void SaveComposition()
        {
            throw new NotImplementedException();
        }
    }
}
