using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Service.Contracts.Database;
using Microsoft.Extensions.Configuration;
using Service.Contracts;
using Service.Contracts.Documents;
using WebLink.Contracts;
using Microsoft.Extensions.DependencyInjection;
using WebLink.Contracts.Models;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Transactions;
using Newtonsoft.Json;

namespace WebLink.Services
{
    [Obsolete("Order are importer from Intake Workflow")]
	public class OrderImportService : IOrderImportService
	{
		private IFactory factory;
		private IAppConfig config;
		private IProductionTypeManagerService productionTypeManager;
		private IOrderUpdateService orderUpdateService;
		private IAppLog log;

		public OrderImportService(
			IFactory factory,
			IAppConfig config,
			IProductionTypeManagerService productionTypeManager,
			IOrderUpdateService orderUpdateService,
			IAppLog log)
		{
			this.factory = factory;
			this.config = config;
			this.productionTypeManager = productionTypeManager;
			this.orderUpdateService = orderUpdateService;
			this.log = log;
		}


		public string CreateOrder(IUserData user, CreateOrderDTO data)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return CreateOrder(ctx, user, data);
			}
		}


		public string CreateOrder(PrintDB ctx, IUserData user, CreateOrderDTO data)
		{
			var tempFileSrv = factory.GetInstance<ITempFileService>();
			var catalogRepo = factory.GetInstance<ICatalogRepository>();
			var companyRepo = factory.GetInstance<ICompanyRepository>();

			int quantity;
			OrderTemplate order;
			JArray rows = new JArray(JArray.Parse(data.Data).OrderBy(r => (string)r["ArticleCode"]));
			var connStr = config["Databases.CatalogDB.ConnStr"];

			var variableDataCatalog = catalogRepo.GetByName(ctx, user.SelectedProjectID, "VariableData");
			var ordersCatalog = catalogRepo.GetByName(ctx, user.SelectedProjectID, "Orders");
			var detailsCatalog = catalogRepo.GetByName(ctx, user.SelectedProjectID, "OrderDetails");
			var company = companyRepo.GetByID(ctx, user.SelectedCompanyID);

			var orderFields = JsonConvert.DeserializeObject<List<FieldDefinition>>(ordersCatalog.Definition);
			var fieldId = orderFields.FirstOrDefault(x => x.Name == "Details").FieldID;

			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromMinutes(5)))
			{
				using (var db = factory.GetInstance<DynamicDB>())
				{
					db.Open(connStr);
					order = InsertOrder(db, company.CompanyCode, ordersCatalog.CatalogID, detailsCatalog.CatalogID, variableDataCatalog.CatalogID, rows, fieldId);
					scope.Complete();
				}
			}
			quantity = GetTotalQuantity(rows);

			var tempFile = tempFileSrv.GetTempFileName(order.OrderNumber + ".json");
			try
			{
				var serializedContent = rows.ToString();
				File.WriteAllBytes(tempFile, Encoding.UTF8.GetBytes(serializedContent));

				CreateOrderGroupRecord(ctx, user, order, data, quantity, tempFile);
			}
			finally
			{
				tempFileSrv.DeleteTempFile(tempFile);
			}
			return order.OrderNumber;
		}


		private int GetTotalQuantity(JArray rows)
		{
			int quantity = 0;
			foreach (var p in rows)
			{
				JObject o = p as JObject;
				quantity += o.GetValue<int>("Quantity");
			}
			return quantity;
		}


		private OrderTemplate InsertOrder(DynamicDB db, string companyCode, int ordersCatalogID, int detailsCatalogID, int variableDataCatalogID, JArray rows, int fieldId)
		{
			OrderTemplate o = new OrderTemplate();
			o.OrderDate = DateTime.Now;
			o.BillTo = companyCode;
			o.SendTo = companyCode;
			o.OrderNumber = $"L{db.Conn.GetNextValue("LocalOrderSQ").ToString("D6")}";
			var json = db.Insert(ordersCatalogID, JsonConvert.SerializeObject(o));
			var order = JObject.Parse(json);
			var orderID = order.GetValue<int>("ID");
			InsertOrderDetail(db, orderID, ordersCatalogID, detailsCatalogID, variableDataCatalogID, rows, fieldId);
			o.ID = orderID;
			return o;
		}


		private void InsertOrderDetail(DynamicDB db, int orderID, int ordersCatalogID, int detailsCatalogID, int variableDataCatalogID, JArray rows, int fieldId)
		{
			foreach (var d in rows)
			{
				var detail = d as JObject;
				db.Insert(variableDataCatalogID, detail);
				OrderDetailTemplate odt = new OrderDetailTemplate();
				odt.ArticleCode = detail.GetValue<string>("ArticleCode");
				odt.PackCode = detail.GetValue<string>("PackCode");
				odt.Quantity = detail.GetValue<int>("Quantity");
				odt.Product = detail.GetValue<int>("ID");
				var json = db.Insert(detailsCatalogID, JsonConvert.SerializeObject(odt));
				var detailRow = JObject.Parse(json);
				var detailID = detailRow.GetValue<int>("ID");
				db.InsertRel(ordersCatalogID, detailsCatalogID, fieldId, orderID, detailID);
			}
		}


		public void CreateOrderGroupRecord(PrintDB ctx, IUserData user, OrderTemplate orderData, CreateOrderDTO instructions, int quantity, string fileName)
		{
			var events = factory.GetInstance<IEventQueue>();
			var orderRepo = factory.GetInstance<IOrderRepository>();
			var jobRepo = factory.GetInstance<IPrinterJobRepository>();
			var projectRepo = factory.GetInstance<IProjectRepository>();

			int projectID = orderData.ProjectID.HasValue ? orderData.ProjectID.Value : user.SelectedProjectID;

			var project = projectRepo.GetByID(ctx, projectID);

			var orderGroup = GetOrderGroup(ctx, orderData.OrderNumber, projectID, orderData.BillTo, orderData.SendTo);

			OrderProductionDetail dataDetails = orderRepo.GetOrderProductionDetail(ctx, orderData.ID, orderData.OrderNumber, projectID, user.SelectedCompanyID);

			// esto se repite
			var groupByArticles = dataDetails.Details.GroupBy(g => g.ArticleCode).ToList();

			int? locationID = jobRepo.GetLocationBy(ctx, instructions.PrinterID);

			foreach (var articleCode in groupByArticles)
			{
				var articlesInGroup = articleCode.AsEnumerable();

				var partialQuantity = articlesInGroup.Where(w => w.ArticleCode.Equals(articleCode.Key)).Sum(a => a.Quantity);

				var order = CreatePartialOrder(ctx, orderGroup, user, orderData.ID, orderData.OrderNumber, orderData.BillTo, orderData.SendTo, instructions.ProductionType, instructions.PrinterID, quantity, DocumentSource.Web, instructions);
				// add file reference
				orderRepo.SetOrderFile(order.ID, File.OpenRead(fileName));

				if (instructions.ProductionType == ProductionType.CustomerLocation)
				{
					jobRepo.CreateArticleOrder(ctx, order, articlesInGroup, locationID, dataDetails.SLADays, instructions.PrinterID, false);
				}
				else
				{
					jobRepo.CreateArticleOrder(ctx, order, articlesInGroup, locationID, dataDetails.SLADays, null, false);
				}

				var orderInfo = orderRepo.GetProjectInfo(ctx, order.ID);

				if (instructions.ProductionType == ProductionType.CustomerLocation && ((int)project.UpdateType < (int)UpdateHandlerType.RequestConfirm || project.UpdateType == UpdateHandlerType.AlwaysNew))
				{
					//orderRepo.ChangeStatus(ctx, order.ID, OrderStatus.ProdReady);
					orderUpdateService.Accept(ctx, order.ID, OrderStatus.ProdReady);
				}

				events.Send(new OrderFileReceivedEvent(orderGroup.ID, order.ID, order.OrderNumber, order.CompanyID, orderInfo.BrandID, order.ProjectID));


			}
		}


		private IOrder CreatePartialOrder(PrintDB ctx, IOrderGroup group, IUserData user, int orderDataID, string orderNumber, string billToCode, string sendToCode, ProductionType prodType, int? printerID, int quantity, DocumentSource getFrom, UploadOrderDTO dto)
		{
			var orderRepo = factory.GetInstance<IOrderRepository>();
			var companyRepo = factory.GetInstance<ICompanyRepository>();
			var provRepo = factory.GetInstance<IProviderRepository>();
			var groupRepo = factory.GetInstance<IOrderGroupRepository>();



			var partialOrder = orderRepo.Create();
			partialOrder.CompanyID = user.SelectedCompanyID;
			partialOrder.ProjectID = user.SelectedProjectID;
			partialOrder.OrderDataID = orderDataID;
			partialOrder.OrderNumber = orderNumber;
			partialOrder.OrderDate = DateTime.Now;// TODO: La fecha de la orden viene en algunos casos en el archivo
			partialOrder.UserName = user.UserName;
			partialOrder.Source = getFrom;
			partialOrder.ProductionType = prodType;

			partialOrder.Quantity = quantity;
			partialOrder.ConfirmedByMD = false;
			partialOrder.PreviewGenerated = false;

			var billtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, user.SelectedProjectID, billToCode);

			// find provider  - first level always
			partialOrder.BillTo = billtocompany.CompanyCode;
			partialOrder.BillToCompanyID = billtocompany.ID;

			int? ProviderRecordID;
			var sendtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, user.SelectedProjectID, sendToCode, out ProviderRecordID);

			partialOrder.SendTo = sendtocompany.CompanyCode;
			partialOrder.SendToCompanyID = sendtocompany.ID;
			partialOrder.ProviderRecordID = ProviderRecordID;

			partialOrder.OrderGroupID = group.ID;
			partialOrder.OrderStatus = OrderStatus.Received;

			// set default production location, this value is required for conflict resolver
			int? productionFactory = sendtocompany.DefaultProductionLocation;

			if (ProviderRecordID.HasValue)
			{
				var provider = provRepo.GetByID(ctx, ProviderRecordID.Value);
				if (provider.DefaultProductionLocation.HasValue)
				{
					productionFactory = provider.DefaultProductionLocation.Value;
				}
			}

			partialOrder.LocationID = productionFactory;

			// TODO: revisar esto
			if (prodType == ProductionType.CustomerLocation)
			{
				//partialOrder.OrderStatus = OrderStatus.Printing;
				partialOrder.AssignedPrinterID = printerID;
				partialOrder.IsBillable = false;
			}

			// Assing Extra data from DTO Object
			if (dto != null)
			{
				if (dto.FactoryID > 0)
				{
					partialOrder.LocationID = dto.FactoryID;
				}

				partialOrder.IsStopped = dto.IsStopped;
				partialOrder.IsBillable = dto.IsBillable;

				group.ERPReference = dto.MDOrderNumber;
				group.OrderCategoryClient = dto.OrderCategoryClient;

				groupRepo.Update(group);
			}

			partialOrder = orderRepo.Insert(ctx, partialOrder);

			return partialOrder;
		}


		public async Task CompleteOrderUpload(DataImportJobInfo job)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				await CompleteOrderUpload(ctx, job);
			}
		}

		public async Task CompleteOrderUpload(PrintDB ctx, DataImportJobInfo job)
		{
			var projectRepo = factory.GetInstance<IProjectRepository>();
			var brandRepo = factory.GetInstance<IBrandRepository>();
			var importService = factory.GetInstance<IDataImportService>();

			var project = projectRepo.GetByID(ctx, job.ProjectID);
			var brand = brandRepo.GetByID(ctx, project.BrandID);

			var companyid = brand.CompanyID;
			var importedData = await importService.GetImportedDataAsync(job.User);

			var ids = importedData.GetImportedRecordIDs();

			log.LogMessage($"Retrieved {importedData.Rows} rows from DocumentService, processing {ids.Count} records...");

			foreach (int rowid in ids)
			{
				CreateOrderGroupFromUpload(ctx, job, importedData, companyid, rowid);
			}
		}


		// This methos is called when order is upload manually
		private void CreateOrderGroupFromUpload(PrintDB ctx, DataImportJobInfo job, ImportedData importedData, int companyid, int orderDataID)
		{
			var events = factory.GetInstance<IEventQueue>();
			var orderRepo = factory.GetInstance<IOrderRepository>();
			var jobRepo = factory.GetInstance<IPrinterJobRepository>();
			var projectRepo = factory.GetInstance<IProjectRepository>();

			var project = projectRepo.GetByID(ctx, job.ProjectID);
			var orderNumber = importedData.GetRecordValue(orderDataID, "OrderNumber");
			var billTo = importedData.GetRecordValue(orderDataID, "BillTo");
			var sendTo = importedData.GetRecordValue(orderDataID, "SendTo");
			var orderGroup = GetOrderGroup(ctx, orderNumber, project.ID, billTo, sendTo);
			UploadOrderDTO dto = (UploadOrderDTO)job.UserData;

			OrderProductionDetail dataDetails = orderRepo.GetOrderProductionDetail(ctx, orderDataID, orderNumber, project.ID, companyid);
			var groupByArticles = dataDetails.Details.GroupBy(g => g.ArticleCode).ToList();

			int? printerID = null;
			if (dto.ProductionType == ProductionType.CustomerLocation)
			{
				printerID = dto.PrinterID;
			}

			int? locationID = jobRepo.GetLocationBy(ctx, printerID);

			IUserData user = new UserData()
			{
				SelectedCompanyID = companyid,
				SelectedProjectID = project.ID,
				UserName = job.User,
			};

			foreach (var articleCode in groupByArticles)
			{
				var articlesInGroup = articleCode.AsEnumerable();

				var partialQuantity = articlesInGroup.Where(w => w.ArticleCode.Equals(articleCode.Key)).Sum(a => a.Quantity);

				// override productiontype
				if (job.Source == DocumentSource.FTP)
				{
					dto.ProductionType = productionTypeManager.GetProductyonType(sendTo, project, articleCode.Key);
				}

				var order = CreatePartialOrder(ctx, orderGroup, user, orderDataID, orderNumber, billTo, sendTo, dto.ProductionType, printerID, partialQuantity, job.Source, dto);
				// add file reference
				orderRepo.SetOrderFile(order.ID, job.FileGUID);

				// if (printerID.HasValue || printerID.Value != null)
				if (dto.ProductionType == ProductionType.CustomerLocation)
				{
					jobRepo.CreateArticleOrder(ctx, order, articlesInGroup, locationID, dataDetails.SLADays, printerID, false);
				}
				else
				{
					jobRepo.CreateArticleOrder(ctx, order, articlesInGroup, locationID, dataDetails.SLADays, null, false);
				}

				var orderInfo = orderRepo.GetProjectInfo(ctx, order.ID);

				if (dto.ProductionType == ProductionType.CustomerLocation && ((int)project.UpdateType < (int)UpdateHandlerType.RequestConfirm || project.UpdateType == UpdateHandlerType.AlwaysNew))
				{
					orderUpdateService.Accept(ctx, order.ID, OrderStatus.ProdReady);
				}
				else
				{
					events.Send(new OrderFileReceivedEvent(orderGroup.ID, order.ID, order.OrderNumber, order.CompanyID, orderInfo.BrandID, order.ProjectID));
				}
			}
		}


		private IOrderGroup GetOrderGroup(PrintDB ctx, string orderNumber, int projectId, string billToCode, string sendToCode)
		{
			var orderGroupRepo = factory.GetInstance<IOrderGroupRepository>();
			var companyRepo = factory.GetInstance<ICompanyRepository>();

			var sendtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, projectId, sendToCode);
			var billtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, projectId, billToCode);

			IOrderGroup og = new OrderGroup()
			{
				OrderNumber = orderNumber,
				// TODO, revisar por que es int? el campo projectID en OrderTemplate
				ProjectID = projectId,
				BillToCompanyID = billtocompany.ID,
				SendToCompanyID = sendtocompany.ID,
				IsActive = true,
				IsRejected = false
			};

			return orderGroupRepo.GetGroupFor(ctx, og);
		}

		//================================================== Workflow Refactor ===============================================


		public IOrderGroup GetOrCreateOrderGroup(string orderNumber, int projectid, string billTo, string sendTo)
		{
			var orderGroupRepo = factory.GetInstance<IOrderGroupRepository>();
			var companyRepo = factory.GetInstance<ICompanyRepository>();

			using (var ctx = factory.GetInstance<PrintDB>())
			{
				var sendtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, projectid, sendTo);
				var billtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, projectid, billTo);

				IOrderGroup og = new OrderGroup()
				{
					OrderNumber = orderNumber,
					ProjectID = projectid,
					BillToCompanyID = billtocompany.ID,
					SendToCompanyID = sendtocompany.ID,
					IsActive = true,
					IsRejected = false
				};

				return orderGroupRepo.GetGroupFor(ctx, og);
			}
		}

		//================================================== Workflow Refactor ===============================================
	}
}
