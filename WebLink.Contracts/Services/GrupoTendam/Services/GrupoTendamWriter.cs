using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;
using WebLink.Contracts.Workflows;

namespace WebLink.Contracts.Services
{
    public class GrupoTendamWriter : IGrupoTendamWriter
    {
        private IUserData userData;
        private IAppConfig config;
        private ILocalizationService g;
        private ILogService log;
        private IOrderPoolRepository repo;
        private ITempFileService temp;
        private IBrandRepository brandRepo;
        private IProjectRepository projectRepo;
        private IAutomatedProcessManager apm;
        private IRemoteFileStore workflowStore;
        private readonly IFactory factory;
        private readonly PrintDB printDB;
        private ICatalogRepository catalogRepo;
        private ICatalogDataRepository catalogDataRepository;
        private IFileStoreManager storeManager;

        public GrupoTendamWriter(IOrderPoolRepository repo,
                                IUserData userData,
                                IAppConfig config,
                                ILocalizationService g,
                                ILogService log,
                                ITempFileService temp,
                                IBrandRepository brandRepo,
                                IProjectRepository projectRepo,
                                IAutomatedProcessManager apm,
                                IRemoteFileStore workflowStore,
                                IFactory factory,
                                ICatalogRepository catalogRepo,
                                ICatalogDataRepository catalogDataRepository,
                                IFileStoreManager storeManager)
        {
            this.repo = repo;
            this.userData = userData;
            this.config = config;
            this.g = g;
            this.log = log;
            this.temp = temp;
            this.brandRepo = brandRepo;
            this.projectRepo = projectRepo;
            this.apm = apm;
            this.workflowStore = workflowStore;
            this.printDB = factory.GetInstance<PrintDB>();
            this.catalogRepo = catalogRepo;
            this.catalogDataRepository = catalogDataRepository;
            this.storeManager = storeManager;
            this.workflowStore = storeManager.OpenStore("WorkflowStore");
        }

        protected class ManualOrderData
        {
            public int ProductionType { get; set; }
            public int PrinterID { get; set; }
            public int FactoryID { get; set; }
            public int ProjectID { get; set; }
            public int CompanyID { get; set; }
            public int BrandID { get; set; }
            public bool IsStopped { get; set; }
            public bool IsBillable { get; set; }
            public string MDOrderNumber { get; set; }
            public string OrderCategoryClient { get; set; }

        }
        public async Task<OperationResult> WriteTendamFile(DataEntryRq rq, string fileName, List<TendamMapping> mappings)
        {
            var result = new OperationResult();

            var path = temp.GetTempDirectory();
            var tempFileName = Path.Combine(path, fileName);

            var dto = new ManualOrderData()
            {
                CompanyID = rq.CompanyID,
                BrandID = rq.BrandID,
                ProjectID = rq.SeasonID,
                ProductionType = 1,  //IDTLocation
                IsBillable = true
            };

            using(FileStream fs = new FileStream(tempFileName, FileMode.OpenOrCreate))
            {
                using(StreamWriter writer = new StreamWriter(fs))
                {
                    foreach(var mapping in mappings)
                    {
                        var line = FormatTendamLine(mapping);
                        // writer.WriteLine(line.Trim());
                        writer.WriteLine(line);
                    }
                }
            }
            string orderData = JsonConvert.SerializeObject(dto);
            result = await IntakeFtpUpload2(orderData, tempFileName);
            
            return result;
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
                    result.Message = g["There has been an error when trying to complete the order, please contact customer support."];
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

        private static string FormatTendamLine(TendamMapping mapping)
        {
            var sb = new StringBuilder();

            // Get all properties with FixedWidthField attribute
            var properties = typeof(TendamMapping).GetProperties()
                .Select(p => new
                {
                    Property = p,
                    FieldAttribute = (FixedWidthFieldAttribute)p.GetCustomAttributes(typeof(FixedWidthFieldAttribute), false)
                        .FirstOrDefault(),
                    PaddingAttribute = (FixedWidthPaddingAttribute)p.GetCustomAttributes(typeof(FixedWidthPaddingAttribute), false)
                        .FirstOrDefault()
                })
                .Where(x => x.FieldAttribute != null)
                .OrderBy(x => x.FieldAttribute.Start)
                .ToList();

            int currentPosition = 1;

            foreach(var prop in properties)
            {
                var fieldAttr = prop.FieldAttribute;
                var paddingAttr = prop.PaddingAttribute;
                var value = prop.Property.GetValue(mapping) != null ? prop.Property.GetValue(mapping).ToString() : null;

                // Add the formatted field with padding configuration
                sb.Append(FormatField(value, fieldAttr.Length, paddingAttr));
                currentPosition = fieldAttr.Start + fieldAttr.Length;
            }

            return sb.ToString();
        }

        private static string FormatField(string value, int length, FixedWidthPaddingAttribute paddingAttr)
        {
            if(value == null)
            {
                value = string.Empty;
            }

            // Truncate if too long
            if(value.Length >= length)
            {
                return value.Substring(0, length);
            }

            // Apply padding based on attribute or default to PadRight with space
            var direction = paddingAttr != null ? paddingAttr.Direction : PaddingDirection.Right;
            var paddingChar = paddingAttr != null ? paddingAttr.PaddingChar : ' ';

            return direction == PaddingDirection.Left
                ? value.PadLeft(length, paddingChar)
                : value.PadRight(length, paddingChar);
        }
    }
}
