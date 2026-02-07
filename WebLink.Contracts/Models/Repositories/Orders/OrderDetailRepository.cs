using Newtonsoft.Json;
using Service.Contracts.Database;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
	public class OrderDetailRepository: IOrderDetailRepository
	{
		private IOrderRepository orderRepo;
		private ICatalogRepository catalogRepo;
		private IDBConnectionManager connManager;

		public OrderDetailRepository(
			IOrderRepository orderRepo,
			ICatalogRepository catalogRepo,
			IDBConnectionManager connManager
		)
		{
			this.orderRepo = orderRepo;
			this.catalogRepo = catalogRepo;
			this.connManager = connManager;
		}

		public List<IOrderDetail> GetOrderDetails(int orderid)
		{
			var order = orderRepo.GetByID(orderid, true);
			return GetOrderDetails(order);
		}

		public List<IOrderDetail> GetOrderDetails(IOrder order)
		{
			ICatalog orders = catalogRepo.GetByName(order.ProjectID, "Orders");
			ICatalog details = catalogRepo.GetByName(order.ProjectID, "OrderDetails");
            var orderFields = orders.Fields.ToList();
            var fieldId = orderFields.FirstOrDefault(x => x.Name == "Details").FieldID;
            using (IDBX conn = connManager.OpenCatalogDB())
			{
				return conn.Select<IOrderDetail>($@"select b.* from REL_{orders.CatalogID}_{details.CatalogID}_{fieldId} a
				join OrderDetails_{details.CatalogID} b on a.TargetID = b.ID
				where a.SourceID = @orderid", order.OrderDataID);
			}
		}
	}

	public class OrderDetail : IOrderDetail
	{
		public int ID { get; set; }
		public string ArticleCode { get; set; }
        public string PackCode { get; set; }
		public int Quantity { get; set; }
		public int Product { get; set; }
    }
}
