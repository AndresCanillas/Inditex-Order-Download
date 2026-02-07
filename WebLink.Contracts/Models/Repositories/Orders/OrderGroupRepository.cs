using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;

namespace WebLink.Contracts.Models
{
	public class OrderGroupRepository : GenericRepository<IOrderGroup, OrderGroup>, IOrderGroupRepository
	{
        private ILogService log;
        private IRemoteFileStore store;
        private IFileStoreManager storeManager;

        public OrderGroupRepository(IFactory factory, ILogService log, IFileStoreManager storeManager)
			: base(factory, (ctx) => ctx.OrderGroups)
		{
			this.factory = factory;
            this.log = log;
            this.storeManager = storeManager;
            store = storeManager.OpenStore("OrderGroupFileStore");
        }


		protected override string TableName => "OrderGroups";


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, OrderGroup actual, IOrderGroup data)
		{
			actual.OrderNumber = data.OrderNumber;
			actual.ProjectID = data.ProjectID;
			actual.SendToAddressID = data.SendToAddressID;
			actual.SendToCompanyID = data.SendToCompanyID;
			actual.BillToCompanyID = data.BillToCompanyID;
			actual.SyncWithSage = data.SyncWithSage;
			actual.SageReference = data.SageReference;
			actual.IsCompleted = data.IsCompleted;
			actual.IsActive = data.IsActive;
			actual.IsRejected = data.IsRejected;
			actual.CompletedDate = data.CompletedDate;
			actual.ERPReference = data.ERPReference;

		}



		public IOrderGroup GetGroupFor(IOrderGroup data)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetGroupFor(ctx, data);
			}
		}


		/// <summary>
		/// Create if not exist group, and retrive Identifier
		/// </summary>
		public IOrderGroup GetGroupFor(PrintDB ctx, IOrderGroup data)
		{
            //TODO: Revisar y cambiar para que encuentre un solo registro unico
			IOrderGroup group = ctx.OrderGroups.Where(w =>
				w.OrderNumber.Equals(data.OrderNumber)
				&& w.ProjectID.Equals(data.ProjectID)
				&& w.IsActive.Equals(true)
				&& w.IsRejected.Equals(false)
				&& w.BillToCompanyID.Equals(data.BillToCompanyID)
				&& w.SendToCompanyID.Equals(data.SendToCompanyID)
				&& w.IsCompleted.Equals(false)
				).FirstOrDefault();

			var projectRepo = factory.GetInstance<IProjectRepository>();
			var project = projectRepo.GetByID(data.ProjectID, true);

			// not exist or always as a new order, register new one, never enter in conflicts
			if (group == null || project.UpdateType == UpdateHandlerType.AlwaysNew)
			{
				return Insert(data);
			}

			return group; // use conflict system to check updates
		}



		/// <summary>
		/// Create if not exist group, and retrive Identifier
		/// </summary>
		public IOrderGroup GetOrCreateGroup(PrintDB ctx, IOrderGroup data,int? ProviderRecordId)
		{

            IOrderGroup group = (from c in ctx.CompanyOrders
                         join g in ctx.OrderGroups on c.OrderGroupID equals g.ID
                         where c.ProviderRecordID == ProviderRecordId && c.OrderNumber == data.OrderNumber && c.ProjectID == data.ProjectID && c.OrderStatus != OrderStatus.Cancelled && g.IsActive.Equals(true)
                         && g.IsRejected.Equals(false) && g.IsCompleted.Equals(false)
                         select g).FirstOrDefault();


            //IOrderGroup group = ctx.OrderGroups.Where(w=> w.ID == query).FirstOrDefault();


            /* IOrderGroup group = ctx.OrderGroups.Where(w =>
                 w.OrderNumber.Equals(data.OrderNumber)
                 && w.ProjectID.Equals(data.ProjectID)
                 && w.IsActive.Equals(true)
                 && w.IsRejected.Equals(false)
                 && w.BillToCompanyID.Equals(data.BillToCompanyID)
                 && w.SendToCompanyID.Equals(data.SendToCompanyID)
                 && w.IsCompleted.Equals(false)
                 ).FirstOrDefault();*/

            var projectRepo = factory.GetInstance<IProjectRepository>();
			var project = projectRepo.GetByID(ctx, data.ProjectID, true);

			// not exist or always as a new order, register new one, never enter in conflicts
			if (group == null || project.UpdateType == UpdateHandlerType.AlwaysNew)
			{
                var ret = Insert(ctx, data);
                log.LogMessage("IOrderGroupRepository - grupo creado");
                return ret;
			}
			else if (group.OrderCategoryClient != data.OrderCategoryClient || group.ERPReference != data.ERPReference)
			{
                
                group.OrderCategoryClient = data.OrderCategoryClient;
				group.ERPReference = data.ERPReference;

				group = Update(ctx, group);
                log.LogMessage("IOrderGroupRepository - grupo actualizado");

            }

			return group; // use conflict system to check updates
		}


		public OrderInfoDTO GetProjectInfo(int orderGroupID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetProjectInfo(ctx, orderGroupID);
			}
		}


		public OrderInfoDTO GetProjectInfo(PrintDB ctx, int orderGroupID)
		{
			var q = ctx.OrderGroups
					.Join(ctx.Projects,
						g => g.ProjectID,
						p => p.ID,
						(group, project) => new { OrderGroup = group, Project = project })
					.Join(ctx.Brands,
						j => j.Project.BrandID,
						b => b.ID,
						(j, brand) => new
						{
							OrderGroup = j.OrderGroup,
							Project = j.Project,
							Brand = brand
						}
						)
					.Where(w => w.OrderGroup.ID.Equals(orderGroupID))
					.Select(s => new OrderInfoDTO()
					{
						OrderGroupID = s.OrderGroup.ID,
						OrderNumber = s.OrderGroup.OrderNumber,
						ProjectID = s.OrderGroup.ProjectID,
						BrandID = s.Project.BrandID,
						CompanyID = s.Brand.CompanyID
					})
					.FirstOrDefault();

			return q;
		}



		public OrderInfoDTO GetBillingInfo(int orderGroupID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetBillingInfo(ctx, orderGroupID);
			}
		}


		public OrderInfoDTO GetBillingInfo(PrintDB ctx, int orderGroupID)
		{
			var q = ctx.OrderGroups
					.Join(ctx.Projects,
						g => g.ProjectID,
						p => p.ID,
						(group, project) => new { OrderGroup = group, Project = project })
					.Join(ctx.Brands,
						j => j.Project.BrandID,
						b => b.ID,
						(j, brand) => new
						{
							OrderGroup = j.OrderGroup,
							Project = j.Project,
							Brand = brand
						}
						)
					.Where(w => w.OrderGroup.ID.Equals(orderGroupID))
					.Select(s => new OrderInfoDTO()
					{
						OrderGroupID = s.OrderGroup.ID,
						OrderNumber = s.OrderGroup.OrderNumber,
						ProjectID = s.OrderGroup.ProjectID,
						BrandID = s.Brand.ID,
						CompanyID = s.Brand.CompanyID,
						SendToCompanyID = s.OrderGroup.SendToCompanyID,
						BillToCompanyID = s.OrderGroup.BillToCompanyID,
						SendToAddressID = s.OrderGroup.SendToAddressID
					})
					.FirstOrDefault();


			return q;
		}


		public Project GetProjectBy(OrderArticlesFilter filter)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetProjectBy(ctx, filter);
			}
		}


		public Project GetProjectBy(PrintDB ctx, OrderArticlesFilter filter)
		{
			var q = ctx.Projects
				.Join(ctx.OrderGroups,
					p => p.ID,
					g => g.ProjectID,
					(project, group) => new { Project = project, OrderGroup = group }
				)
				.Join(ctx.CompanyOrders,
					pg => pg.OrderGroup.ID,
					c => c.OrderGroupID,
					(projectGroup, order) => new {
						projectGroup.Project,
						projectGroup.OrderGroup,
						Order = order
					}

				)
				.Where(w =>
				(string.IsNullOrEmpty(filter.OrderNumber) || w.OrderGroup.OrderNumber.Equals(filter.OrderNumber))
				&&
                ( 
                    filter.OrderID.Count > 0 &&  filter.OrderID.Contains(w.Order.ID))
				    || (filter.OrderID.Count <= 0 && filter.OrderGroupID > 0 && w.OrderGroup.ID.Equals(filter.OrderGroupID))  
				)
				.Select(s => s.Project)
				.FirstOrDefault();

			return q;
		}


		public IEnumerable<IOrder> ChangeProvider(int orderGroupID, int providerRecordID)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return ChangeProvider(ctx, orderGroupID, providerRecordID);
			}
		}


		public IEnumerable<IOrder> ChangeProvider(PrintDB ctx, int orderGroupID, int providerRecordID)
		{
			var grp = GetByID(ctx, orderGroupID, true);

			//var currentSendTo = ctx.Companies.Where(w => w.ID.Equals(grp.SendToCompanyID)).First();
			var orders = ctx.CompanyOrders.Where(w => w.OrderGroupID.Equals(orderGroupID)).ToList();

			// who is the broker ?
			//var broker = currentSendTo.IsBroker ? currentSendTo.ID : orders[0].CompanyID;

            var providerRel = ctx.CompanyProviders.Find(providerRecordID);

			var provider = ctx.Companies.Where(w => w.ID.Equals(providerRel.ProviderCompanyID)).First();

			// set sentto and billto at the same time always to the same provider
            // only sendtto require update
            // TODO: create a combined foreing key bettwen CompanyOrders and Providers:
            // CompanyOrders CompanyID,  SendTod, ProviderRecordID -> CompanyProviders: CompanyID, ProviderCompanyID, ID
			grp.BillToCompanyID = provider.ID;
			grp.SendToCompanyID = provider.ID;

			orders.ForEach((o) => {
				o.BillToCompanyID = provider.ID;
				o.BillTo = provider.CompanyCode;
				o.SendToCompanyID = provider.ID;
				o.SendTo = provider.CompanyCode;
				o.ProviderRecordID = providerRel.ID;
			});

			ctx.SaveChanges();

			// this changes is not reflected into labels
			return orders;
		}

		/// <summary>
		/// use for check for company orders registerd in Sage
		/// return order open in sage in the las 30 days
		/// </summary>
		public IEnumerable<OrderGroupDetailDTO> GetRegisteredInSage()
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetRegisteredInSage(ctx);
			}
		}

		public IEnumerable<OrderGroupDetailDTO> GetRegisteredInSage(PrintDB ctx)
		{
			var now = DateTime.Now;
			var maxDaysToTracking = 30;

			var q = ctx.OrderGroups
				.Where(w => w.SyncWithSage.Equals(true))
				.Where(w => w.SageStatus == SageOrderStatus.Open)
				.Where(w => w.RegisteredOn != null && w.RegisteredOn.Value.AddDays(maxDaysToTracking) >= now)
				.Select(s => new OrderGroupDetailDTO()
				{
					OrderGroupID = s.ID,
					SageReference = s.SageReference
				});

			return q.ToList();
		}

        public IEnumerable<IOrder> GetAllErpOrderReferencesInGroup(OrderInGroupFilter filter)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetAllErpOrderReferencesInGroup(ctx, filter).ToList();
            }
        }

        /// <summary>
        /// Looking for orders inner the same group sync with SAGE on SageStatus is OPEN
        /// if ordernumber is include inner the filter only looking orders with the same ordernumber -> this apply for repetitions, to group every rep. inner the same sage order
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IEnumerable<IOrder> GetAllErpOrderReferencesInGroup(PrintDB ctx, OrderInGroupFilter filter)
        {

            var orders = ctx.CompanyOrders
                .Where(w => w.OrderGroupID == filter.OrderGroupID)
                .Where(w => w.SyncWithSage)
                .Where(w => w.OrderStatus != OrderStatus.Cancelled)
                .Where(w => w.InvoiceStatus == SageInvoiceStatus.NoInvoiced)
                .Where(w => w.SageStatus == SageOrderStatus.Open)
                .Where(w => w.DeliveryStatus == SageDeliveryStatus.NoShipped)
                .Where(w => w.LocationID == filter.LocationID);


            if (!string.IsNullOrEmpty(filter.OrderNumber))
            {
                orders = orders
                    // .Where(w => System.Text.RegularExpressions.Regex.IsMatch(w.OrderNumber, Order.REPEAT_PATTERN))
                    .Where(w => w.OrderNumber == filter.OrderNumber);
                
            }

            orders = orders.Select(s => s);

            return orders;
        }

        public void SetOrderGroupAttachment(int ordergroupid, string attachmentCategory, string fileName, Stream content)
        {
            var Grp = GetByID(ordergroupid, true);

            var container = store.GetOrCreateFile(ordergroupid, Grp.OrderNumber);

            var category = container.GetAttachmentCategory(attachmentCategory);

            if (!category.TryGetAttachment(fileName, out var attachment))
                attachment = category.CreateAttachment(fileName);
            attachment.SetContent(content);
        }

        public OrderPdfResult GetOrderPdf(int ordergroupid,string orderNumber, string attachmentCategory = "SupportFiles")
        {
            if(store.TryGetFile(ordergroupid, out var file))
            {
                var categoryAttach = file.GetAttachmentCategory(attachmentCategory);
                foreach(var item in categoryAttach)
                {
                    if(item.FileName.Contains(orderNumber.Substring(0,6)) && Path.GetExtension(item.FileName).ToLower() == ".pdf")
                    {
                        return new OrderPdfResult
                        {
                            Filename = item.FileName,
                            Content = item.GetContentAsStream()
                        };
                    }
                }
            }
            return null;
        }

        public void ChangeOrderNumber(int orderGroupID, string orderNumber)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                ChangeOrderNumber(ctx, orderGroupID, orderNumber);
            }

        }

        public void ChangeOrderNumber(PrintDB ctx,int orderGroupID, string orderNumber)
        {

            using(var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromMinutes(1)))
            {
                var orderGroup = GetByID(orderGroupID);
                ctx.Entry(orderGroup).State = EntityState.Modified;
                orderGroup.OrderNumber = orderNumber;
                

                var orders = ctx.CompanyOrders.Where(o => o.OrderGroupID == orderGroupID);
                
                foreach(var order in orders)
                {
                    ctx.Entry(order).State = EntityState.Modified;
                    order.OrderNumber = orderNumber;
                }

                ctx.SaveChanges();
                scope.Complete();
            }
        }

        public OperationResult AttachOrderGroupDocument(OrderAttachDocumentRequest attachRequest)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<OrderGroup> GetGroupByOrderNumberList(string orderNumber,int projectID, int days)
        {
            var now = DateTime.Now;
            var from = now.AddDays(-days);
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return ctx.OrderGroups.Where(w =>
                w.OrderNumber.Equals(orderNumber)
                && w.ProjectID.Equals(projectID)
                && w.IsActive.Equals(true)
                && w.IsRejected.Equals(false)
                && w.IsCompleted.Equals(false)
                && w.CreatedDate >= from 
                && w.CreatedDate <= now
                ).ToList();
            }
        }
    }

    public class OrderPdfResult
    {
        public string Filename { get; set; }
        public Stream Content { get; set; }
    }


    // helper to filter orders in group
    public class OrderInGroupFilter
    {
        public int OrderGroupID { get; set; }
        public SageOrderStatus SageOrderStatus { get; set; }
        public string OrderNumber { get; set; }
        public bool OnlyRepetitions { get; set; }
        public int? LocationID { get; set; }    


        public OrderInGroupFilter() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OrderGroupID"></param>
        /// <param name="SageOrderStatus"> default SageOrderStatus.Unknow to ignore filter</param>
        public OrderInGroupFilter(int OrderGroupID, SageOrderStatus SageOrderStatus = SageOrderStatus.Unknow)
        {
            this.OrderGroupID = OrderGroupID;
            this.SageOrderStatus = SageOrderStatus; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="group"></param>
        /// <param name="SageOrderStatus">default SageOrderStatus.Unknow to ignore filter</param>
        public OrderInGroupFilter (OrderGroup group, SageOrderStatus SageOrderStatus = SageOrderStatus.Unknow)
        {
            this.OrderGroupID = group.ID;
            this.SageOrderStatus = SageOrderStatus;
        }
    }
}
