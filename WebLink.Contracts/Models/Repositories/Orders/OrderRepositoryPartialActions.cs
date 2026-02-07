using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.CodeAnalysis.Operations;
using Newtonsoft.Json;
using Remotion.Linq.Clauses;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.WF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Sage;
using WebLink.Contracts.Workflows;

namespace WebLink.Contracts.Models
{
    public partial class OrderRepository : GenericRepository<IOrder, Order>, IOrderRepository
    {
        //  states only can be set by IDT Users
        public IOrder ChangeStatus(int orderID, OrderStatus newStatus)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
				return ChangeStatus(ctx, orderID, newStatus);
            }
        }

        public IOrder ChangeStatusWF(int orderID, OrderStatus newStatus, bool repeatTask)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
				var order = GetByID(ctx,orderID);
				if (order.HasOrderWorkflow)
                {
                    var wf = apm.GetWorkflowAsync("Order Processing").Result;
                    var item = wf.FindItemAsync((long)order.ItemID).Result;
                    var canMove = wf.CanMoveAsync(item).Result;

                    if (!canMove)
                    {
                        throw new Exception("There is an active workflow task, can't change order status");
                    }
                }

                var oldStatus = order.OrderStatus;
				order = ChangeStatus(ctx, orderID, newStatus);
                 
                if (oldStatus != OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
                {
                    SetWorkflowTask(order, repeatTask);
                }
                return order;
            }
        }

        public void SetWorkflowTask(int orderID, OrderStatus newStatus, bool repeatTask)
        {
			using(var ctx = factory.GetInstance<PrintDB>())
			{
                var currentOrder = GetByID(ctx, orderID, true);
                
                SetWorkflowTask(currentOrder, repeatTask);
			}
		}

        public bool ChangeDueDate(OrderDueDateDTO entity, IOrderRegisterInERP orderRegisterInERP)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                var order = GetByID(entity.OrderID);
                order.DueDate = entity.OrderDueDate;
                if (order.SyncWithSage)
                {
                    orderRegisterInERP.UpdateDueDate(ctx, new OrderInfoDTO { OrderID = entity.OrderID, DueDate = entity.OrderDueDate, OrderGroupID = order.OrderGroupID });
                }
                Update(order);
                return true;
            }
        }


        public IOrder ChangeStatus(PrintDB ctx, int orderID, OrderStatus newStatus)
        {
            var wzdRepo = factory.GetInstance<IWizardRepository>();
            var userData = factory.GetInstance<IUserData>();
            var orderLogRepo = factory.GetInstance<IOrderLogRepository>();
            var providerRepo = factory.GetInstance<IProviderRepository>();

            var currentOrder = GetByID(ctx, orderID, true);
            var oldStatus = currentOrder.OrderStatus;
            var provider = providerRepo.GetByID(currentOrder.ProviderRecordID.Value);
            currentOrder.OrderStatus = newStatus;

            switch (newStatus)
            {
                case OrderStatus.InFlow:
                    currentOrder.SageReference = null;
                    currentOrder.SageStatus = SageOrderStatus.Unknow;
                    currentOrder.InvoiceStatus = SageInvoiceStatus.NoInvoiced;
                    currentOrder.DeliveryStatus = SageDeliveryStatus.NoShipped;
                    currentOrder.CreditStatus = SageCreditStatus.Unknow;
                    currentOrder.ValidationDate = null;
                    currentOrder.ValidationUser = null;
                    currentOrder.DueDate = null;
                    currentOrder.SendToAddressID = null;
                    currentOrder.IsBilled = false;

                    // reset wizard
                    var wizard = wzdRepo.GetByOrder(ctx, orderID);

                    if (wizard != null)
                    {
                        wzdRepo.Reset(ctx, wizard.ID);
                    }

                    break;

                case OrderStatus.Validated:
                    var cfgRepository = factory.GetInstance<IConfigurationRepository>();
                    currentOrder.ValidationDate = DateTime.Now;
                    currentOrder.ValidationUser = userData.UserName;
                    if (currentOrder.ProductionType == ProductionType.IDTLocation)
                        currentOrder.DueDate = cfgRepository.GetOrderDueDate(currentOrder.CompanyID, provider.ClientReference, currentOrder.ProjectID);

                    // Determine production location, this can be set at different levels: company, project and the provider (SendToCompanyID). The search goes from more specific (provider) to more general (company), and returns the first production location that is found.
                    if (!currentOrder.LocationID.HasValue && (currentOrder.ProductionType == ProductionType.IDTLocation))
                        currentOrder.LocationID = cfgRepository.FindProductionLocationID(currentOrder.CompanyID, provider.ClientReference, currentOrder.ProjectID);

                    // Determine delivery address, in case the validation workflow is not enabled this field will be null, in that case we use the default delivery location configured in the system.
                    // This is setup in the company that will receive the order, if no address has been setup, then an error is thrown and the process will not be able to continue until that problem is fixed.
                    if (!currentOrder.SendToAddressID.HasValue || currentOrder.SendToAddressID < 1)
                        currentOrder.SendToAddressID = cfgRepository.FindDefaultDeliveryAddress(currentOrder.SendToCompanyID);

                    break;

                case OrderStatus.Cancelled:
                    //Get current active companyorders in the same group
                    var q = ctx.CompanyOrders
                        .Where(o => o.OrderGroupID == currentOrder.OrderGroupID)
                        .Where(o => o.OrderStatus != OrderStatus.Cancelled)
                        .Where(o => o.ID != currentOrder.ID)
                        .ToList();

                    // current order is last active order, cancel group too
                    if (q.Count() < 1)
                    {
                        var currentGroup = ctx.OrderGroups.FirstOrDefault(x => x.ID == currentOrder.OrderGroupID);
                        currentGroup.IsActive = false;

                        ctx.SaveChanges();
                    }

                    ctx.PrinterJobs.Where(w => w.CompanyOrderID.Equals(orderID)).ToList().ForEach(j =>
                    {

                        j.Status = JobStatus.Cancelled;

                    });

                    // mark as rejected - TODO: review this update before apply
                    //var prop = ctx.OrderUpdateProperties.FirstOrDefault(w => w.OrderID.Equals(currentOrder.ID));
                    //if (prop != null)
                    //	prop.IsRejected = true;

                    ctx.SaveChanges();
					SetWorkflowTask(currentOrder,false);
                    break;

                case OrderStatus.Billed:
                    currentOrder.IsBilled = true;
                    break;

                case OrderStatus.ProdReady:
                    // this value is required for PrintLocal Application
                    if (currentOrder.ValidationDate == null)
                    {
                        currentOrder.ValidationDate = DateTime.Now;
                        currentOrder.ValidationUser = "System"; // TODO: how to set same username in all project, now use hard code username
                    }

                    break;

            } // end switch

			// Reactivate order workflow if old status was cancelled 
			if (currentOrder.HasOrderWorkflow && oldStatus == OrderStatus.Cancelled && currentOrder.OrderStatus != oldStatus)
			{
				SetWorkflowTask(currentOrder,false);
			}

			// activate ordergroup if at leasts one companyorder is active
			if (oldStatus == OrderStatus.Cancelled && currentOrder.OrderStatus != oldStatus)
            {
                var grpToActive = ctx.OrderGroups.First(w => w.ID.Equals(currentOrder.OrderGroupID));
                grpToActive.IsActive = true;
                grpToActive.IsRejected = false;

                ctx.SaveChanges();
            }

            var updatedOrder = Update(ctx, currentOrder);

            if (updatedOrder.OrderStatus == OrderStatus.Cancelled)
            {
                // Send Notification -- other notifications for the rest of statuses was send in another places
                SendCancelNotification(currentOrder);

            }

            if (newStatus != OrderStatus.Cancelled)
            {
                var prop = ctx.OrderUpdateProperties.FirstOrDefault(w => w.OrderID.Equals(currentOrder.ID));
                if (prop != null)
                {
                    prop.IsRejected = false;
                    prop.IsActive = true;
                }
                ctx.SaveChanges();
            }

            orderLogRepo.Info(ctx, new OrderLog()
            {
                OrderID = currentOrder.ID,
                Level = OrderLogLevel.INFO,
                Message = $"Order is moved to : {newStatus.GetText()}"

            });

            // Trigger OrderUpdateStatusEvent when status change
            if(oldStatus != newStatus )
            {
                // Only for Cancelled and Completed status send specific event 
                // This restriction must be removed when PrintLocal is already deployed in all factories to support all status events    
                if(newStatus == OrderStatus.Cancelled || newStatus == OrderStatus.Completed)
                {
                    events.Send(new OrderUpdateStatusEvent() { OrderID = orderID, OrderStatus = (int)newStatus });
                }
            }

            return updatedOrder;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>First Pending  WizardStep</returns>
        public IWizardStep GetNextStep(int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetNextStep(ctx, orderID);
            }
        }


        public IWizardStep GetNextStep(PrintDB ctx, int orderID)
        {
            var result = ctx.WizardSteps
                //.Include(w => w.Wizard)
                .Where(w => w.Wizard.OrderID.Equals(orderID))
                .Where(w => w.IsCompleted.Equals(false))
                .OrderBy(w => w.Position)
                .Select(s => s)
                .FirstOrDefault();

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>Last Completed WizardStep</returns>
        public IWizardStep GetBackStep(int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetBackStep(ctx, orderID);
            }
        }


        public IWizardStep GetBackStep(PrintDB ctx, int orderID)
        {
            var result = ctx.WizardSteps
                .Where(w => w.Wizard.OrderID.Equals(orderID))
                .Where(w => w.IsCompleted.Equals(true))
                .OrderByDescending(w => w.Position)
                .Select(s => s)
                .FirstOrDefault();

            if (result == null)
            {
                return GetNextStep(orderID);
            }

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="orderID"></param>
        /// <returns>WizardStep object in position selected</returns>
        public IWizardStep GetStep(int position, int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetStep(ctx, position, orderID);
            }
        }


        public IWizardStep GetStep(PrintDB ctx, int position, int orderID)
        {
            var result = ctx.WizardSteps
                .Where(w => w.Wizard.OrderID.Equals(orderID) && w.Position.Equals(position))
                .Select(s => s)
                .FirstOrDefault();

            return result;
        }


        public void ResetStatusEvent(int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                ResetStatusEvent(ctx, orderID);
            }
		}
        
        public void MoveItemToTask(long itemID,string taskName,IUserData userData, ItemStatus itemstatus, bool repeatTask )
		{
			const string reason = "Order status updated by user";
			var wf = apm.GetWorkflowAsync("Order Processing").Result;
			var task = wf.GetTask(taskName);
			var item = wf.FindItemAsync(itemID).Result;

            var state = item.GetSavedStateAsync<OrderItem>().Result;

            switch (taskName) 
            {
                case "OrderSetValidator":
                    state.OrderSetValidatorCompleted = state.OrderSetValidatorCompleted && !repeatTask;
                    state.SendOrderValidatedEmailCompleted = state.SendOrderValidatedEmailCompleted && !repeatTask;
                    state.CheckIsBillableCompleted = state.CheckIsBillableCompleted && !repeatTask;
                    state.PerformOrderBillingCompleted = state.PerformOrderBillingCompleted && !repeatTask;
                    state.MarkAsBilledCompelted = state.MarkAsBilledCompelted && !repeatTask;
                    state.CreateOrderDetailDocumentCompleted = state.CreateOrderDetailDocumentCompleted && !repeatTask;
                    state.CreateOrderPreviewDocumentCompleted = state.CreateOrderPreviewDocumentCompleted && !repeatTask;
                    state.CreateProdSheetDocumentCompleted = state.CreateProdSheetDocumentCompleted && !repeatTask;
                    state.CreatePrintPackageCompleted = state.CreatePrintPackageCompleted && !repeatTask;
                    state.RunOrderValidatedPluginsCompleted = state.RunOrderValidatedPluginsCompleted && !repeatTask;
                    state.RunReadyToPrintPluginsCompleted = state.RunReadyToPrintPluginsCompleted && !repeatTask;
                    state.RunOrderReceivedPluginsCompleted = state.RunOrderReceivedPluginsCompleted && !repeatTask;
                    state.RunReverseFlowCompleted = state.RunReverseFlowCompleted && !repeatTask;
                    state.FileDropEventCount = 0;
                    break;
                case "SendOrderValidatedEmail":
                    state.SendOrderValidatedEmailCompleted = state.SendOrderValidatedEmailCompleted && !repeatTask;
                    state.CheckIsBillableCompleted = state.CheckIsBillableCompleted && !repeatTask;
                    state.PerformOrderBillingCompleted = state.PerformOrderBillingCompleted && !repeatTask;
                    state.MarkAsBilledCompelted = state.MarkAsBilledCompelted && !repeatTask;
                    state.CreateOrderDetailDocumentCompleted = state.CreateOrderDetailDocumentCompleted && !repeatTask;
                    state.CreateOrderPreviewDocumentCompleted = state.CreateOrderPreviewDocumentCompleted && !repeatTask;
                    state.CreateProdSheetDocumentCompleted = state.CreateProdSheetDocumentCompleted && !repeatTask;
                    state.CreatePrintPackageCompleted = state.CreatePrintPackageCompleted && !repeatTask;
                    state.RunReadyToPrintPluginsCompleted = state.RunReadyToPrintPluginsCompleted && !repeatTask;
                    state.RunOrderReceivedPluginsCompleted = state.RunOrderReceivedPluginsCompleted && !repeatTask;
                    state.RunReverseFlowCompleted = state.RunReverseFlowCompleted && !repeatTask;
                    state.FileDropEventCount = 0;
                    break;
                case "CreateOrderDetailDocument":
                    state.CreateOrderDetailDocumentCompleted = state.CreateOrderDetailDocumentCompleted && !repeatTask;
                    state.CreateOrderPreviewDocumentCompleted = state.CreateOrderPreviewDocumentCompleted && !repeatTask;
                    state.CreateProdSheetDocumentCompleted = state.CreateProdSheetDocumentCompleted && !repeatTask;
                    state.CreatePrintPackageCompleted = state.CreatePrintPackageCompleted && !repeatTask;
                    state.RunOrderReceivedPluginsCompleted = state.RunOrderReceivedPluginsCompleted && !repeatTask;
                    state.RunReverseFlowCompleted = state.RunReverseFlowCompleted && !repeatTask;
                    break;
                case "RunReverseFlow":
                    state.RunReverseFlowCompleted = state.RunReverseFlowCompleted && !  repeatTask;
                    break;
            }

            item.UpdateSavedStateAsync(state).Wait();

            if (item.ItemStatus == ItemStatus.Cancelled || item.ItemStatus == ItemStatus.Completed) 
            {
				wf.ReactivateAsync(item, task.TaskID, itemstatus, TimeSpan.FromSeconds(1), reason, userData.Principal.Identity).Wait();
			}
            else
            {
				wf.MoveAsync(item, task.TaskID, itemstatus, TimeSpan.FromSeconds(1), reason, userData.Principal.Identity).Wait();
			}
		}

		public void SetWorkflowTask(IOrder orderInfo, bool repeatTask)
        {
			// Set corresponding task to the new state in the workflow
			if (orderInfo.HasOrderWorkflow)
			{
				var userData = factory.GetInstance<IUserData>();
				var wf = apm.GetWorkflowAsync("Order Processing").Result;
				var item = wf.FindItemAsync(orderInfo.ItemID.Value).Result;

                // Can't move active items 
                if (item.ItemStatus == ItemStatus.Active) return;

                var itemState = item.GetSavedStateAsync().Result;

                var orderItem = JsonConvert.DeserializeObject<OrderItem>(itemState);

                switch (orderInfo.OrderStatus)
				{
					case OrderStatus.Received:
						MoveItemToTask(orderInfo.ItemID.Value, "InsertItem", userData, ItemStatus.Delayed,repeatTask);
						break;
					case OrderStatus.InFlow:
					case OrderStatus.Processed:
						MoveItemToTask(orderInfo.ItemID.Value, "OrderSetValidator", userData, ItemStatus.Delayed, repeatTask);
						break;
					case OrderStatus.Validated:
						MoveItemToTask(orderInfo.ItemID.Value, "SendOrderValidatedEmail", userData, ItemStatus.Delayed, repeatTask);
						break;
					case OrderStatus.Billed:
						MoveItemToTask(orderInfo.ItemID.Value, "CreateOrderDetailDocument", userData, ItemStatus.Delayed,repeatTask);
						break;
                    case OrderStatus.Completed:
                        if(orderItem.HasReverseFlowStrategy)
                        {
                            MoveItemToTask(orderInfo.ItemID.Value, "RunReverseFlow", userData, ItemStatus.Delayed, repeatTask);
                        }
                        else if(item.ItemStatus != ItemStatus.Cancelled && item.ItemStatus != ItemStatus.Completed)
                        { 
                            wf.CompleteAsync(item, "Completed by user", userData.Principal.Identity).Wait();
                        }
                        break;
                    case OrderStatus.Printing:
                        break;
                    case OrderStatus.Cancelled:
                        if (item.ItemStatus!=ItemStatus.Cancelled && item.ItemStatus!=ItemStatus.Completed)
                            wf.CancelAsync(item, "Cancelled by user", userData.Principal.Identity).Wait();
						break;
					default:
						log.LogWarning($"Cant Reset Status Event to orderID: {orderInfo.ID} - OrderStatus: {orderInfo.OrderStatus.GetText()}, action or event is not defined");
						break;
				}
			}
		}

        public void ResetStatusEvent(PrintDB ctx, int orderID)
        {
            var orderInfo = GetProjectInfo(ctx, orderID);

            // Consultar ??
            if (orderInfo.HasOrderWorkflow) return; // 

            switch (orderInfo.OrderStatus)
            {
                case OrderStatus.Received:
                    events.Send(new OrderFileReceivedEvent(orderInfo.OrderGroupID, orderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                    break;

                case OrderStatus.Processed:
                    events.Send(new OrderExistVerifiedEvent(orderInfo.OrderGroupID, orderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID, true));
                    break;

                case OrderStatus.Validated:
                    events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                    break;

                case OrderStatus.Billed:
                    events.Send(new OrderBillingCompletedEvent(orderInfo.OrderGroupID, orderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                    break;

                default:
                    log.LogWarning($"Cant Reset Status Event to orderID: {orderID} - OrderStatus: {orderInfo.OrderStatus.GetText()}, action or event is not defined");
                    break;

            }

        }

        #region Wizard Validation Grouped



        public IWizardStep GetNextStepBySelection(List<OrderGroupSelectionDTO> selection)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetNextStepBySelection(ctx, selection);
            }
        }


        public IWizardStep GetNextStepBySelection(PrintDB ctx, List<OrderGroupSelectionDTO> selection)
        {
            // join all order ids 
            //var orders = new List<int>();

            //selection.ForEach(e => orders.AddRange(e.Orders));

            //var result = this.ctx.WizardSteps
            //    //.Include(w => w.Wizard)
            //    .Where(w => w.IsCompleted.Equals(false))
            //    .Where(w => orders.Contains(w.Wizard.OrderID))
            //    .OrderBy(t => t.Wizard.Progress)
            //    .ThenBy(w => w.Position)
            //    .Select(s => s);

            //return result.FirstOrDefault();

            return GetAllWizardSteps(ctx, selection)
                .Where(w => !w.IsCompleted)
                .OrderBy(o => o.Position)
                .FirstOrDefault();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="orderID"></param>
        /// <returns>WizardStep object in position selected</returns>
        public IWizardStep GetStepBySelection(int position, List<OrderGroupSelectionDTO> selection)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetStepBySelection(ctx, position, selection);
            }
        }


        public IWizardStep GetStepBySelection(PrintDB ctx, int position, List<OrderGroupSelectionDTO> selection)
        {
            var orders = new List<int>();

            selection.ForEach(e => orders.AddRange(e.Orders));

            var result = ctx.WizardSteps
                .Join(ctx.Wizards,
                    st => st.WizardID,
                    wz => wz.ID,
                    (step, wizard) => new { Wizard = wizard, WizardStep = step }
                ).Join(ctx.PrinterJobs,
                    join1 => join1.Wizard.OrderID,
                    pj => pj.CompanyOrderID,
                    (j1, job) => new { j1.Wizard, j1.WizardStep, PrinterJob = job }
                ).Join(ctx.Articles,
                    join2 => join2.PrinterJob.ArticleID,
                    a => a.ID,
                    (j2, article) => new { j2.Wizard, j2.WizardStep, j2.PrinterJob, Article = article }
                    )
                .Where(w =>
                    orders.Contains(w.Wizard.OrderID)
                    && w.WizardStep.Position.Equals(position)
                ).OrderByDescending(o => o.Article.LabelID)

                .Select(s => s.WizardStep)
                .ToList();

            return result.FirstOrDefault();
        }


        // get step for wizard with less progress
        public IEnumerable<IWizardStep> GetAllWizardSteps(List<OrderGroupSelectionDTO> selection)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetAllWizardSteps(ctx, selection).ToList();
            }
        }


        public IEnumerable<IWizardStep> GetAllWizardSteps(PrintDB ctx, List<OrderGroupSelectionDTO> selection)
        {
            var orders = new List<int>();  // join all order ids 

            selection.ForEach(e => orders.AddRange(e.Orders));

            var result = ctx.WizardSteps
                //.Include(w => w.Wizard)
                //.Where(w => w.IsCompleted.Equals(false))
                .Where(w => orders.Contains(w.Wizard.OrderID))
                .OrderBy(t => t.Wizard.Progress)
                .ThenBy(w => w.Position)
                .Select(s => s);


            var first = result.FirstOrDefault();

            if(first != null)
            {
                result = result.Where(w => w.WizardID.Equals(first.WizardID));
            }

            return result;
        }


        #endregion Wizard Validation Grouped



        public void ChangeProductionType(int orderID, ProductionType productionType)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                ChangeProductionType(ctx, orderID, productionType);
            }
        }


        public void ChangeProductionType(PrintDB ctx, int orderID, ProductionType productionType)
        {
            var order = GetByID(ctx, orderID, true);

            order.ProductionType = productionType;

            var billedStatus = new List<OrderStatus>() { OrderStatus.Billed, OrderStatus.ProdReady, OrderStatus.Printing };

            if (productionType == ProductionType.IDTLocation)
            {
                order.IsBillable = true;

                if (billedStatus.Contains(order.OrderStatus))
                {
                    ChangeStatus(ctx, orderID, OrderStatus.Validated);
                }
                else
                {
                    Update(ctx, order);
                }
            }
            else
            {
                // TODO: required to delete sage order 
                order.IsBillable = false;

                Update(ctx, order);
            }
        }


        internal class FileArticle
        {
            public OrderDetailDTO Article;
            public IFSFile File;
        }
        public async Task<string> PackMultiplePreviewDocument(IEnumerable<int> orderids)
        {
            IOrderDocumentService docSrv = factory.GetInstance<IOrderDocumentService>();

            List<FileArticle> files = new List<FileArticle>();

            var createdTasks = new List<Task<Guid>>();
            List<OrderDetailDTO> articles = GetOrderArticles(new OrderArticlesFilter() { OrderID = orderids.ToList() }, ProductDetails.None).ToList();

            var groupedArticles = articles.GroupBy(x => x.OrderID)
                .Select(s => new OrderDetailDTO()
                {
                    OrderID = s.Key,
                    ArticleCode = s.Max(m => m.ArticleCode),
                    OrderNumber = s.Max(m => m.OrderNumber)
                })
                .ToList();



            foreach (var article in groupedArticles)
            {
                // create PDF
                var tmp = docSrv.CreatePreviewDocument(article.OrderID);

                createdTasks.Add(tmp);

            }

            await Task.WhenAll(createdTasks);

            foreach (var article in groupedArticles)
            {
                files.Add(new FileArticle()
                {
                    Article = article,
                    File = docSrv.GetPreviewDocument(article.OrderID)
                });
            }

            // temp file to create zip
            var orderNumber = String.Join('-', articles.GroupBy(g => g.OrderNumber).Select(g => g.Key));
            var d = DateTime.Now;
            var tempFileService = factory.GetInstance<ITempFileService>();
            string baseFileName = tempFileService.SanitizeFileName($"Previews-{orderNumber}-{d.Year}{d.Month}{d.Day}{d.Hour}{d.Minute}{d.Second}.zip");
            var fileName = tempFileService.GetTempFileName(baseFileName, true);

            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                fs.SetLength(0L);
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    foreach (var file in files)
                    {
                        using (var srcStream = file.File.GetContentAsStream())
                        {

                            string entryName = tempFileService.SanitizeFileName($"Previews-{file.Article.OrderNumber}-{file.Article.ArticleCode}-{d.Year}{d.Month}{d.Day}{d.Hour}{d.Minute}{d.Second}.pdf");
                            var entry = archive.CreateEntry(entryName);
                            using (var dstStream = entry.Open())
                            {
                                srcStream.CopyTo(dstStream, 4096);
                            }
                        }
                    }
                }
            }

            return fileName;
        }

        private void SendCancelNotification(IOrder order)
        {
            var notificationMng = factory.GetInstance<IOrderNotificationManager>();

            notificationMng.RegisterCancelledNotification(order);


        }

        public IEnumerable<IOrder> SetConflictStatusByShareData(bool IsInConflict, int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return SetConflictStatusByShareData(ctx, IsInConflict, orderID);
            }
        }

        public IEnumerable<IOrder> SetConflictStatusByShareData(PrintDB ctx, bool IsInConflict, int orderID)
        {
            IOrder order = ctx.CompanyOrders.First(w => w.ID == orderID);

            var sharedDataOrders = ctx.CompanyOrders
                .Join(ctx.OrderUpdateProperties,
                    ord => ord.ID,
                    pps => pps.OrderID,
                    (o, p) => new { CompanyOrders = o, OrderUpdateProperties = p })

                .Where(w => w.CompanyOrders.OrderDataID == order.OrderDataID)
                .Where(w => w.OrderUpdateProperties.IsActive && !w.OrderUpdateProperties.IsRejected)
                .Select(s => s.CompanyOrders)
                .ToList();

            sharedDataOrders.ForEach(e =>
            {
                e.IsInConflict = IsInConflict;
                Update(e);
            });


            return sharedDataOrders;

        }
    }
}
