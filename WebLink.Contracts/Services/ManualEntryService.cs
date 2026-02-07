using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Service.Contracts;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Repositories.OrderPool;
using WebLink.Contracts.Workflows;

namespace WebLink.Contracts.Services
{
    public partial class ManualEntryService : IManualEntryService
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


        public ManualEntryService(IOrderPoolRepository repo,
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


        public async Task<OperationResult> SaveOrder(DataEntryRq rq, string username)
        {
            var articles = CreateArticles(rq, username);
            var details = GetOrderDetail(rq, username);
            return await CreateFile(rq, details, articles);
        }

        private async Task<OperationResult> CreateFile(DataEntryRq rq,
                              List<OrderDetail> details,
                              List<OrderArticle> articles
                               )
        {
            var result = new OperationResult();
            var catalogId = GetSizeSets(rq.SeasonID);
            var Sizes = GetOrderSubSet(catalogId, rq.SizeSetID);
            List<SizesDTO> SizesData = JsonConvert.DeserializeObject<List<SizesDTO>>(Sizes);

            var parseOrder = GetParserOrderById(rq, SizesData, details, articles);

            var dto = new ManualOrderData()
            {
                CompanyID = rq.CompanyID,
                BrandID = rq.BrandID,
                ProjectID = rq.SeasonID,
                ProductionType = 1,  //IDTLocation
                IsBillable = true
            };

            if(parseOrder.Count > 0)
            {
               return await SendOrder(dto, parseOrder);
            }

            result.Success = false;
            return result; 
        }


        private List<OrderDetail> GetOrderDetail(DataEntryRq rq, string username)
        {
            var catalogId = GetSizeSets(rq.SeasonID);
            var Sizes = GetOrderSubSet(catalogId, rq.SizeSetID);
            List<SizesDTO> SizesData = JsonConvert.DeserializeObject<List<SizesDTO>>(Sizes);

            List<OrderDetail> orderDetails = new List<OrderDetail>();
            List<dynamic> lstSizes = new List<dynamic>();
            var rowsSizeTable = "{RowsSizes:" + rq.RowJsonData + "}";

            dynamic dyn = JsonConvert.DeserializeObject<ExpandoObject>(rowsSizeTable, new ExpandoObjectConverter());
            foreach(var row in ((IEnumerable<dynamic>)dyn.RowsSizes))
            {
                lstSizes.Clear();
                string ColorCode = row.ColorCode;

                foreach(var column in row)
                {
                    if(column.Key != "ColorCode" && column.Key != "TotalColor" && column.Key != "AdditionalTags")
                    {
                        var col = column.Value;
                        string[] data = col.ToString().Split("|");

                        lstSizes.Add(new { Size = column.Key.Replace("SizeID_", ""), Qty = data[0], China = data.Length > 1 ? data[1] : null });
                    }
                }

                foreach(var r in lstSizes)
                {
                    orderDetails.Add(new OrderDetail
                    {

                        ColorCode = ColorCode,
                        SizeID = int.Parse(r.Size),
                        ChinaCustom = r.China,
                        Quantity = r.Qty,
                        Barcode = CreateBarcode(ColorCode, int.Parse(r.Size), SizesData, rq.ArticleQuality1, rq.ArticleQuality2),
                        CreatedBy = username,
                        IsNew = true,
                        OrderID = rq.ID
                    });
                }
            }

            return orderDetails;

        }

        private string CreateBarcode(string ColorCode, int SizeID, List<SizesDTO> SizesData, string model, string quality)
        {
            SizesDTO size = SizesData.Find(x => x.ID == SizeID);
            return "0" + model + quality + ColorCode + size.Code + "?";
        }


        private List<OrderArticle> CreateArticles(DataEntryRq rq, string username)
        {
            var articles = new List<OrderArticle>();
            for(var i = 0; i < rq.Articles.Count; i++)
            {
                articles.Add(new OrderArticle
                {
                    ArticleID = rq.Articles[i].ArticleID,
                    ArticleCode = rq.Articles[i].ArticleCode,
                    PackCode = rq.Articles[i].PackCode,
                    LabelID = rq.Articles[i].LabelID,
                    CreatedBy = username,
                    IsNew = true,
                    OrderID = rq.ID
                });
            }
            return articles;
        }
        private List<ParserOrderDTO> GetParserOrderById(DataEntryRq rq,
                                List<SizesDTO> sizes,
                                List<OrderDetail> orderDetails,
                                List<OrderArticle> orderArticles)
        {

            var data = new List<ParserOrderDTO>();
            var orders = new List<DataEntryRq>() { rq };

            List<SizeRangeDTO> lst = new List<SizeRangeDTO>();
            var result = orderDetails
                            .Where(o => o.Quantity != "-")
                            .Select(o => new
                            {
                                SizeID = o.SizeID,
                                ColorCode = o.ColorCode
                            });

            lst = (from t in result
                   join si in sizes on t.SizeID equals si.ID
                   select new SizeRangeDTO
                   {
                       ColorCode = t.ColorCode,
                       SizeID = t.SizeID,
                       SizeTxt = si.DisplayField
                   }).ToList();



            var query = (from o in orders
                         join oa in orderArticles on o.ID equals oa.OrderID into oaJoin
                         from oa in oaJoin.DefaultIfEmpty()
                         join od in orderDetails on o.ID equals od.OrderID into odJoin
                         from od in odJoin.DefaultIfEmpty()
                         where o.ID == rq.ID &&
                               od.Quantity != "-" && od.Quantity != "0"
                         select new ParserOrderDTO()
                         {
                             OrderNumber = o.OrderNumber,
                             ProviderReference = o.ProviderReference,
                             SeasonID = o.SeasonID,
                             SectionID = o.SectionID,
                             MarketOriginID = o.MarketOriginID,
                             Model = o.ArticleQuality1,
                             Quality = o.ArticleQuality2,
                             Description = o.Description,
                             DeliveryDate = DateTime.Now,
                             Price1 = o.Price1,
                             Currency1 = o.Currency1,
                             Price2 = o.Price2,
                             Currency2 = o.Currency2,
                             Price3 = o.Price3,
                             Currency3 = o.Currency3,
                             Price4 = o.Price4,
                             Currency4 = o.Currency4,
                             //Price5 = o.Price5,
                             //Currency5 = o.Currency5,
                             ArticleCode = oa.ArticleCode,
                             SizeSetID = o.SizeSetID,
                             SizeID = od.SizeID,
                             SizeRange = "",
                             ColorCode = od.ColorCode,
                             Quantity = Math.Ceiling(Convert.ToDecimal(od.Quantity) * ((Convert.ToDecimal(o.AdditonalTagsPercentage) / 100) + 1)),
                             Barcode = od.Barcode,
                             SubSectionID = o.SubSectionID,
                             ChinaCustom = od.ChinaCustom,
                             LabelID = oa.LabelID,
                             PackCode = oa.PackCode, 
                             RequiresChineseColor = rq.RequiresChineseColor
                         }).ToList();
            data = query.ToList();
            data.ForEach(r =>
            {
                var currentSizes = lst.Where(w => w.ColorCode == r.ColorCode).Select(s => s.SizeTxt);
                r.SizeRange = string.Join(" ", currentSizes);
            });

            List<ParserOrderDTO> d1 = data.Where(x => x.LabelID != null || x.PackCode != null).ToList();

            List<ParserOrderDTO> d2 = data.Where(x => x.LabelID == null && x.PackCode == null)
                         .GroupBy(p => p.ArticleCode)
                         .Select(g => g.First())
                         .ToList();

            //List<ParserOrderDTO> d2 = data.Where(x => x.LabelID == null && x.PackCode == null)
            // .ToList();

            var List = (from d in data
                        where d.LabelID == null
                        group d by d.ArticleCode
                        into grp
                        select new { ArticleCode = grp.Key, Quantity = grp.Sum(x => x.Quantity) }).ToList();

            //foreach(var o in List)
            //{
            //    foreach(var item in d2.Where(x => x.ArticleCode == o.ArticleCode))
            //    {
            //        item.Quantity = o.Quantity;
            //    }
            //}

            // d1.AddRange(d2);

            return data.Where(x => x.Quantity > 0).ToList();

        }




        private async Task<OperationResult> SendOrder(ManualOrderData dto, List<ParserOrderDTO> data)
        {
            var path = temp.GetTempDirectory();
            var fileName = $"Order-{data[0].OrderNumber}.csv";
            var tempFileName = Path.Combine(path, fileName);

            using(FileStream fs = new FileStream(tempFileName, FileMode.OpenOrCreate))
            {
                fs.SetLength(0L);
                string delimiterChar = ";";

                using(StreamWriter writer = new StreamWriter(fs))
                {
                    data.ForEach(item =>
                        writer.WriteLine(
                            item.OrderNumber + delimiterChar
                            + item.ProviderReference + delimiterChar
                            + item.SeasonID + delimiterChar
                            + item.SectionID + delimiterChar
                            + item.MarketOriginID + delimiterChar
                            + item.Model + delimiterChar
                            + item.Quality + delimiterChar
                            + item.Description + delimiterChar
                            + item.DeliveryDate + delimiterChar
                            + item.Price1 + delimiterChar
                            + item.Currency1 + delimiterChar
                            + item.Price2 + delimiterChar
                            + item.Currency2 + delimiterChar
                            + item.Price3 + delimiterChar
                            + item.Currency3 + delimiterChar
                            + item.Price4 + delimiterChar
                            + item.Currency4 + delimiterChar
                            + item.Price5 + delimiterChar
                            + item.Currency5 + delimiterChar
                            + item.ArticleCode + delimiterChar
                            + item.SizeSetID + delimiterChar
                            + item.SizeID + delimiterChar
                            + item.SizeRange + delimiterChar
                            + item.ColorCode + delimiterChar
                            + item.Quantity + delimiterChar
                            + item.SubSectionID + delimiterChar
                            + item.ChinaCustom + delimiterChar 
                            + item.RequiresChineseColor
                     ));
                }

                string orderData = JsonConvert.SerializeObject(dto);
                var result = await IntakeFtpUpload2(orderData, tempFileName);
                return result; 
            }


        }

        private async Task<OperationResult> IntakeFtpUpload2(string orderData, string filepath)
        {
            OperationResult result = new OperationResult();


            try
            {
                var dto = JsonConvert.DeserializeObject<UploadOrderDTO>(orderData);
                var project = await projectRepo.GetByIDAsync(dto.ProjectID, true);
                var brand = await brandRepo.GetByIDAsync(project.BrandID,true);
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



        private async Task IntakeFtpUpload(string orderData, string filepath)
        {
            var dto = JsonConvert.DeserializeObject<UploadOrderDTO>(orderData);
            var project = await projectRepo.GetByIDAsync(dto.ProjectID);
            var brand = await brandRepo.GetByIDAsync(project.BrandID);
            try
            {
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
                    Source = DocumentSource.FTP,
                    UserName = userData.Principal.Identity.Name,
                    FileName = filepath,
                    InputFile = dstfile.FileGUID,
                    WorkflowFileID = dstfile.FileID
                };

                var wf = await apm.GetWorkflowAsync("OrderIntake");
                await wf.InsertItemAsync(new OrderFileItem(input));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
        }

        private int GetSizeSets(int projectId)
        {
            var catalog = printDB.Catalogs.FirstOrDefault(c => c.ProjectID == projectId && c.Name == "SizesSet");
            if(catalog == null) return 0;
            return catalog.CatalogID;
        }

        private string GetOrderSubSet(int catalog, int sizeSetId)
        {
            return catalogDataRepository.GetSubset(catalog, sizeSetId, "Sizes");
        }

        public async Task<OrderPoolDTO> UploadFileOrder(Stream src, ManualEntryOrderFileDTO manualEntryOrderFileDTO)
        {
            var serializer = new JsonSerializer();
            using(var sr = new StreamReader(src))
            using(var jsonTextReader = new JsonTextReader(sr))
            {
                var JsonOrder = serializer.Deserialize<ManualOrderEntry>(jsonTextReader);
                var orderPoolDto = new OrderPoolDTO()
                {
                    CompanyID = manualEntryOrderFileDTO.CompanyID,
                    CreatedBy = "toni.esteve",
                    BrandID = manualEntryOrderFileDTO.BrandID,
                    SeasonID = -1,
                    OrderNumber = JsonOrder.OrderNumber,
                    Price1 = GetPrice(JsonOrder?.BluePrice),
                    Price2 = GetPrice(JsonOrder?.RedPrice),
                    Price3 = GetPrice(JsonOrder?.BlackPrice),
                    Price4 = GetPrice(JsonOrder?.USAPrice),
                    Price5 = GetPrice(JsonOrder?.UKPrice),
                    Currency1 = JsonOrder?.BlueCurrency,
                    Currency2 = JsonOrder?.RedCurrency,
                    Currency3 = JsonOrder?.BlackCurrency,
                    Currency4 = JsonOrder?.USACurrency,
                    Currency5 = JsonOrder?.UKCurrency,

                    ArticleQuality1 = JsonOrder.Article,
                    ArticleQuality2 = JsonOrder.Quality,
                    OrderJsonData = JsonConvert.SerializeObject(JsonOrder),
                    IsNew = true
                };


                return await Task.FromResult(orderPoolDto);
            }
        }

        private string GetPrice(string price)
        {
            if(!string.IsNullOrEmpty(price) && price != "," && price != ".")
            {
                price = price.Replace(",", ".");
            }
            else
            {
                price = null;
            }
            return price;
        }

        public Task DeleteOrderPool(DeleteOrderPoolDTO dto)
        {
            throw new NotImplementedException();
        }

        protected class ParserOrderDTO
        {
            public string OrderNumber { get; set; }
            public string ProviderReference { get; set; }
            public int SeasonID { get; set; }
            public int SectionID { get; set; }
            public int SubSectionID { get; set; }
            public int MarketOriginID { get; set; }
            public string Model { get; set; }
            public string Quality { get; set; }
            public string Description { get; set; }
            public DateTime DeliveryDate { get; set; }
            public string Price1 { get; set; }
            public string Price2 { get; set; }
            public string Price3 { get; set; }
            public string Price4 { get; set; }
            public string Price5 { get; set; }
            public string Currency1 { get; set; }
            public string Currency2 { get; set; }
            public string Currency3 { get; set; }
            public string Currency4 { get; set; }
            public string Currency5 { get; set; }
            public string ArticleCode { get; set; }
            public int SizeSetID { get; set; }
            public int SizeID { get; set; }
            public string SizeRange { get; set; }
            public string ColorCode { get; set; }
            public decimal Quantity { get; set; }
            public string Barcode { get; set; }
            public string ChinaCustom { get; set; }
            public int? LabelID { get; set; }
            public string PackCode { get; set; }
            public bool RequiresChineseColor { get; set; }
        }

        protected class SizeRangeDTO
        {
            public string ColorCode { get; set; }
            public int SizeID { get; set; }
            public string SizeTxt { get; set; }
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


        protected class SizesDTO
        {
            public int ID { get; set; }
            public string Code { get; set; }
            public bool IsActive { get; set; }
            public string DisplayField { get; set; }
            public int SizeOrder { get; set; }
        }

        protected class OrderDetail
        {
            public int ID { get; set; }
            public int OrderID { get; set; }
            public int SizeID { get; set; }
            public string ColorCode { get; set; }
            public string Quantity { get; set; }
            public string Barcode { get; set; }
            public string ChinaCustom { get; set; }

            public string CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }
            public string UpdatedBy { get; set; }
            public DateTime UpdatedDate { get; set; }
            internal bool IsNew;
        }

    }

    public class ManualOrderEntry
    {
        public string Brand { get; set; }
        public string Season { get; set; }
        public string Section { get; set; }
        public string SubSection { get; set; }
        public string Provider { get; set; }
        public string MadeIn { get; set; }
        public string OrderNumber { get; set; }
        public string Article { get; set; }
        public string Quality { get; set; }
        public string BluePrice { get; set; }
        public string BlueCurrency { get; set; }
        public string RedPrice { get; set; }
        public string RedCurrency { get; set; }
        public string BlackPrice { get; set; }
        public string BlackCurrency { get; set; }
        public string SizeRange { get; set; }
        public List<Size> Sizes { get; set; }

        public string UKCurrency { get; set; }
        public string UKPrice { get; set; }
        public string USACurrency { get; set; }
        public string USAPrice { get; set; }

        

    }

    public class Size
    {
        public string ColorCode { get; set; }
        public string CurrentSize { get; set; }
        public string CurrentSizePdf { get; set; }
        public string Barcode { get; set; }
        public List<Equivalence> Equivalences { get; set; }
        public string BarcodeZalando { get; set; }

    }
    public class Equivalence
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

}

