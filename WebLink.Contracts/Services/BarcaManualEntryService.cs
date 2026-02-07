using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Repositories.OrderPool;
using WebLink.Contracts.Workflows;
using static WebLink.Contracts.Services.ZaraManualEntryHelper;


namespace WebLink.Contracts.Services
{


    public class BarcaManualEntryService : IManualEntryService
    {
        private readonly IUserData userData;
        private readonly OrderDetailsRetriever orderDetailsRetriever;
        private readonly OrderFileBuilder orderFileBuilder;
        private ITempFileService temp;
        private IBrandRepository brandRepo;
        private IProjectRepository projectRepo;
        private ILogService log;
        private IRemoteFileStore workflowStore;
        private IFileStoreManager storeManager;
        private IAutomatedProcessManager apm;
        private ILocalizationService g;
        private IFactory factory;

        public BarcaManualEntryService(IUserData userData,
                                      OrderDetailsRetriever orderDetailsRetriever,
                                      OrderFileBuilder orderFileBuilder,
                                      ITempFileService temp,
                                      IBrandRepository brandRepo,
                                      IProjectRepository projectRepo,
                                      ILogService log,
                                      IRemoteFileStore workflowStore,
                                      IFileStoreManager storeManager,
                                      IAutomatedProcessManager apm,
                                      ILocalizationService g,
                                      IFactory factory)
        {
            this.userData = userData;
            this.orderDetailsRetriever = orderDetailsRetriever;
            this.orderFileBuilder = orderFileBuilder;
            this.temp = temp;
            this.brandRepo = brandRepo;
            this.projectRepo = projectRepo;
            this.log = log;
            this.workflowStore = workflowStore;
            this.storeManager = storeManager;
            this.workflowStore = storeManager.OpenStore("WorkflowStore");
            this.apm = apm;
            this.g = g;
            this.factory = factory;
        }

        public Task<OperationResult> SaveOrder(DataEntryRq rq, string username)
        {
            return SendOrder(rq);    
        }

        public Task<OrderPoolDTO> UploadFileOrder(Stream src, ManualEntryOrderFileDTO manualEntryOrderFileDTO)
        {
            throw new NotImplementedException();
        }

        private async Task<OperationResult> SendOrder(DataEntryRq rq)
        {
            var path = temp.GetTempDirectory();
            var fileName = $"Order-{rq.OrderNumber}.csv";
            var tempFileName = Path.Combine(path, fileName);

            using(FileStream fs = new FileStream(tempFileName, FileMode.OpenOrCreate))
            {
                fs.SetLength(0L);
                string delimiterChar = ";";

                using(StreamWriter writer = new StreamWriter(fs))
                {
                    //writer.WriteLine("PO" + delimiterChar
                    //                + "COMPANYNAME" + delimiterChar
                    //                + "ISSUEPO" + delimiterChar
                    //                + "ETA" + delimiterChar
                    //                + "REF BLM" + delimiterChar
                    //                + "EAN" + delimiterChar
                    //                + "SIZE" + delimiterChar
                    //                + "DESCRIPTION" + delimiterChar
                    //                + "QTY" + delimiterChar
                    //                + "UN" + delimiterChar
                    //                + "COST PRICE" + delimiterChar
                    //                + "CURRENCY" + delimiterChar
                    //                + "RETAIL PRICE" + delimiterChar
                    //                + "CURRENCY" + delimiterChar);

                    rq.Sizes.ForEach(item =>
                        writer.WriteLine(
                            rq.OrderNumber + delimiterChar
                            + rq.ProviderReference + delimiterChar
                            + rq.CompanyName + delimiterChar
                            + rq.CreatedDate + delimiterChar
                            + rq.ExpectedProductionDate + delimiterChar
                            + rq.ArticleCode + delimiterChar
                            + item.EAN + delimiterChar
                            + item.Size + delimiterChar
                            + item.Description + delimiterChar
                            + item.Quantity + delimiterChar
                            + item.UN + delimiterChar
                            + item.Price1 + delimiterChar
                            + item.Currency1 + delimiterChar
                            + item.Price2 + delimiterChar
                            + item.Currency2 + delimiterChar
                     ));
                }
                var dto = new ManualOrderData()
                {
                    CompanyID = rq.CompanyID,
                    BrandID = rq.BrandID,
                    ProjectID = rq.SeasonID,
                    ProductionType = 1,  //IDTLocation
                    IsBillable = true
                };
                string orderData = JsonConvert.SerializeObject(dto);
                var result = await IntakeFtpUpload2(orderData, tempFileName);
                if(result.Success)
                {
                    UpdateProcessedOrder(rq.OrderNumber, rq.ArticleCode);
                }
                return result;
            }
        }

        private void UpdateProcessedOrder(string orderNumber, string articleCode)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                ctx.OrderPools.Where(o=> o.OrderNumber.Equals(orderNumber) && o.ArticleCode.Equals(articleCode)).ToList().ForEach(o =>
                {
                    o.ProcessedBy = userData.UserName; 
                    o.ProcessedDate = DateTime.Now;  
                    ctx.SaveChanges();
                });
            }
        }

        private async Task<OperationResult> IntakeFtpUpload2(string orderData, string filepath)
        {
            OperationResult result = new OperationResult();


            try
            {
                var dto = JsonConvert.DeserializeObject<UploadOrderDTO>(orderData);
                var project = await projectRepo.GetByIDAsync(dto.ProjectID, true);
                var brand = await brandRepo.GetByIDAsync(project.BrandID, true);
                log.LogMessage($"Received file from FTPWatcher: {filepath}");

                IRemoteFile dstfile = await workflowStore.CreateFileAsync(filepath);
                using(var stream = System.IO.File.OpenRead(filepath))
                    await dstfile.SetContentAsync(stream);

                var input = new IntakeWorkflowInput()
                {
                    CompanyID = dto.CompanyID,
                    BrandID = brand.ID,
                    ProjectID = project.ID,
                    IsBillable = dto.IsBillable,
                    IsStopped = dto.IsStopped,
                    IsTestOrder = false,
                    ProductionType = dto.ProductionType,
                    ProductionLocationID = dto.FactoryID > 0 ? (int?)dto.FactoryID : null,
                    ERPReference = dto.MDOrderNumber,
                    ClientCategory = dto.OrderCategoryClient,
                    Source = DocumentSource.Web,
                    UserName = userData.Principal.Identity.Name,
                    FileName = filepath,
                    InputFile = dstfile.FileGUID,
                    WorkflowFileID = dstfile.FileID
                };



                var wf = await apm.GetWorkflowAsync("OrderIntake");
                var item = new OrderFileItem(input);
                item.MaxTries = 1;
                await wf.InsertItemAsync(item);
                var i = await wf.WaitForItemStatus(item.ItemID, ItemStatus.Completed | ItemStatus.Rejected, TimeSpan.FromSeconds(120));

                // TODO: REPOND WHEN ORDERPROCESSING WORKFLOW SET THE ORDER INTO INFLOW STATE OR READYTOPRINT STATE


                if(i.ItemStatus == ItemStatus.Rejected)
                {
                    result.Success = false;
                    result.Message = g["There has been an error when trying to complete the order, please contact customer support. {0}", item.ItemID];
                }
                else
                {
                    var itemState = await i.GetSavedStateAsync<OrderFileItem>();
                    result.Data = itemState.CreatedOrders;
                    result.Success = true;
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                result.Success = false;
                result.Message = g["There has been an error when trying to complete the order, please contact customer support."];
            }
            return result;

        }

        public Task DeleteOrderPool(DeleteOrderPoolDTO dto)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                var orders = ctx
                    .OrderPools
                    .Where(o => o.ProjectID == dto.ProjectID && o.OrderNumber == dto.OrderNumber && o.ArticleCode == dto.ArticleCode);
                    
                if(orders != null)
                {
                    foreach(var  order in orders)
                    {
                       order.DeletedBy = userData.UserName;
                        order.DeletedDate = DateTime.Now;
                    }

                    ctx.SaveChanges();
                }
            }
            return Task.CompletedTask;
        }
    }
}
