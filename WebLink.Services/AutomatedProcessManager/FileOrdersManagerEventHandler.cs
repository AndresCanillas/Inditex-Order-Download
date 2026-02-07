using Service.Contracts;
using Services.Core;
using System;
using System.IO;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
    public class FileOrdersManagerEventHandler : EQEventHandler<FileOrdersManagerEvent>
    {
        private IRemoteFileStore projectFileStore;
        private IFileStoreManager storeManager;
        private IFactory factory;
        private IOrderRepository orderRepository;
        private ILogService log;
        private readonly IOrderActionsService actionsService;
        public FileOrdersManagerEventHandler(IFileStoreManager store, IOrderRepository orderRepository, IFactory factory, ILogService log, IOrderActionsService actionsService)
        {
            this.storeManager = store;
            projectFileStore = storeManager.OpenStore("ProjectStore");
            this.orderRepository = orderRepository;
            this.factory = factory;
            this.log = log;
            this.actionsService = actionsService;
        }
        public override EQEventHandlerResult HandleEvent(FileOrdersManagerEvent e)
        {
            var stream = GetFileStream(e.ProjectID, e.FileName);
            bool result = true;
            log.LogWarning($"Armand Thiery - Handle event filename {e.FileName}");

            if (stream == null)
            {
                log.LogWarning($"Armand Thiery - File stream not found for {e.FileName} in project {e.ProjectID}");
                return EQEventHandlerResult.OK;
            }

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (!TryProcessLine(line, e.ProjectID, e.FileName, out bool lineProcessed))
                    {
                        result = false;
                    }
                }
            }

            return result ? EQEventHandlerResult.OK : EQEventHandlerResult.Delay5;
        }

        // Helper method to process individual lines
        private bool TryProcessLine(string line, int projectId, string fileName, out bool processed)
        {
            processed = false;
            
            var parts = line.Split('-');
            if (parts.Length < 2)
            {
                log.LogWarning($"Armand Thiery - malformed line format '{line}' in file {fileName} - missing separator");
                return true; // Continue processing other lines
            }

            string firstPart = parts[0]?.Trim();
            string secondPart = parts[1]?.Trim();

            if (string.IsNullOrWhiteSpace(firstPart) || string.IsNullOrWhiteSpace(secondPart))
            {
                log.LogWarning($"Armand Thiery - malformed line format '{line}' in file {fileName} - empty parts");
                return true; // Continue processing other lines
            }

            // Validate minimum lengths before substring operations
            if (firstPart.Length < 9)
            {
                log.LogWarning($"Armand Thiery - Insufficient length for order number extraction in file {fileName}, line: '{line}'");
                return true; // Continue processing other lines
            }

            if (secondPart.Length < 2)
            {
                log.LogWarning($"Armand Thiery - Insufficient length for batch number extraction in file {fileName}, line: '{line}'");
                return true; // Continue processing other lines
            }

            try
            {
                string orderNumber = firstPart.Substring(3, 6);
                string batchNumber = secondPart.Substring(1);

                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    log.LogWarning($"Armand Thiery - No order found in file {fileName}, line: '{line}'");
                    return true; // Continue processing other lines
                }

                processed = true;
                return ProcessOrderNumber(orderNumber, projectId, batchNumber);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                log.LogWarning($"Armand Thiery - Error extracting data from line '{line}' in file {fileName}: {ex.Message}");
                return true; // Continue processing other lines
            }
        }

        private bool ProcessOrderNumber(string ordernumber, int projetid, string batchNumber)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var orders = ctx.CompanyOrders.Where(x => x.OrderNumber.Contains(ordernumber) && x.ProjectID == projetid).ToList();
                
                if (!orders.Any()) 
                {
                    log.LogWarning($"Armand Thiery - No orders found for order number {ordernumber} and project {projetid}");
                    return false;
                }

                bool hasChanges = false;
                foreach(var order in orders)
                {
                    var orderArticleName = GetArticleName(order.ID);
                    
                    if (ShouldCancelOrder(order.OrderStatus))
                    {
                        order.AllowRepeatedOrders = false;
                        hasChanges = true;
                        orderRepository.ChangeStatus(order.ID, OrderStatus.Cancelled);
                        AddSystemChangedOrder(order, orderArticleName, batchNumber, SystemOrderAction.Cancelled);
                    }
                    else if (ShouldStopOrder(order.OrderStatus) && !order.IsStopped)
                    {
                        order.AllowRepeatedOrders = false;
                        hasChanges = true;
                        actionsService.StopOrder(order.ID);
                        AddSystemChangedOrder(order, orderArticleName, batchNumber, SystemOrderAction.Stoped);
                    }
                }

                if (hasChanges)
                {
                    ctx.SaveChanges();
                }

                return true;
            }
        }

        private bool ShouldCancelOrder(OrderStatus status)
        {
            return status == OrderStatus.InFlow ||
                   status == OrderStatus.Processed || 
                   status == OrderStatus.Received;
        }

        private bool ShouldStopOrder(OrderStatus status)
        {
            return status == OrderStatus.ProdReady ||
                   status == OrderStatus.Validated ||
                   status == OrderStatus.Printing || 
                   status == OrderStatus.Billed ||
                   status == OrderStatus.Completed;
        }
        private void AddSystemChangedOrder(IOrder order, string articleName, string batchNumber , SystemOrderAction action)
        {

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                if (string.IsNullOrWhiteSpace(order?.OrderNumber) || string.IsNullOrWhiteSpace(articleName))
                {
                    log.LogWarning("Armand Thiery - Invalid order or article name for system changed order log");
                    return;
                }

                var systemChangedOrder = ctx.SystemChangedOrdersLog.FirstOrDefault(x => 
                    x.OrderNumber == order.OrderNumber && 
                    x.ArticleName == articleName && 
                    x.ProjectID == order.ProjectID);
                
                if (systemChangedOrder != null) 
                {
                    systemChangedOrder.ActionID = action;
                    systemChangedOrder.BatchNumber = batchNumber; // Update batch number as well
                }
                else
                {
                    ctx.SystemChangedOrdersLog.Add(new SystemChangedOrdersLog()
                    {
                        OrderNumber = order.OrderNumber,    
                        ArticleName = articleName,   
                        ActionID = action, 
                        CreatedDate = DateTime.Now, 
                        BatchNumber = batchNumber,
                        ProjectID = order.ProjectID,
                    });
                }
                
                ctx.SaveChanges(); // Single SaveChanges call for both update and insert operations
            }

        }
        private string GetArticleName(int orderID)
        {
            if (orderID <= 0)
            {
                log.LogWarning($"Armand Thiery - Invalid orderID: {orderID}");
                return null;
            }

            try
            {
                using (var ctx = factory.GetInstance<PrintDB>())
                {
                    var articleName = ctx.Articles
                        .Join(ctx.PrinterJobs, 
                              a => a.ID, 
                              p => p.ArticleID, 
                              (a, p) => new { Article = a, PrinterJob = p })
                        .Where(x => x.PrinterJob.CompanyOrderID == orderID)
                        .Select(x => x.Article.Name)
                        .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(articleName))
                    {
                        log.LogWarning($"Armand Thiery - No article found for orderID: {orderID}");
                    }

                    return articleName;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"Armand Thiery - Error retrieving article name for orderID {orderID}: {ex.Message}");
                return null;
            }
        }

        private Stream GetFileStream(int projectID, string fileName)
        {
            if(!projectFileStore.TryGetFile(projectID, out var container))
                throw new Exception($"Image container for project {projectID} was not found.");
            if(fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);
            var images = container.GetAttachmentCategory("Images");
            if(!images.TryGetAttachment(fileName, out var image))
                return null;
            return image.GetContentAsStream();
        }
    }
}
