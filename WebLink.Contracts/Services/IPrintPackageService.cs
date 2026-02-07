using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IPrintPackageService
    {
        void CreatePrintPackage(int orderID);
    }

    // ==================================================================================================
    // The "Print Package" is a .zip file which includes all the information necesary to produce the
    // order using the Local Print system.
    // ==================================================================================================
    public class PrintPackageService : IPrintPackageService
    {
        private IFactory factory;
        private IEventQueue events;
        private IAppConfig config;
        private IOrderRepository orderRepo;
        private IOrderDocumentService orderDocuments;
        private IConfigurationRepository cfgRepository;
        private ICatalogDataRepository catDataRepo;
        private IDBConnectionManager connManager;
        private IRFIDConfigRepository rfidRepo;
        private IRemoteFileStore orderStore;
        private IRemoteFileStore projectStore;
        private IRemoteFileStore articlePreviewStore;
        private ITempFileService temp;
        private ILabelRepository labelRepo;
        private ILogService log;
        private IInLayRepository inLayRepository;

        public PrintPackageService(
            IFactory factory,
            IEventQueue events,
            IAppConfig config,
            IOrderRepository orderRepo,
            IOrderDocumentService orderDocuments,
            IConfigurationRepository cfgRepository,
            ICatalogDataRepository catDataRepo,
            IDBConnectionManager connManager,
            IRFIDConfigRepository rfidRepo,
            IFileStoreManager storeManager,
            ITempFileService temp,
            ILabelRepository labelRepo,
            ILogService log,
            IInLayRepository inLayRepository)
        {
            this.factory = factory;
            this.events = events;
            this.config = config;
            this.orderRepo = orderRepo;
            this.orderDocuments = orderDocuments;
            this.cfgRepository = cfgRepository;
            this.catDataRepo = catDataRepo;
            this.connManager = connManager;
            this.rfidRepo = rfidRepo;
            this.temp = temp;
            this.labelRepo = labelRepo;
            this.log = log;
            this.inLayRepository = inLayRepository;
            orderStore = storeManager.OpenStore("OrderStore");
            projectStore = storeManager.OpenStore("ProjectStore");
            articlePreviewStore = storeManager.OpenStore("ArticlePreviewStore");
        }

        public void CreatePrintPackage(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                log.LogMessage($"Creating Print Package for order {orderID}");
                var path = temp.GetTempDirectory();
                var order = orderRepo.GetByID(ctx, orderID);

                if(!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                CreateOrderPrintPackage(ctx, order, path);

                // update order status to contine process
                order.OrderStatus = OrderStatus.ProdReady;
                orderRepo.Update(ctx, order);

                // Send event notification to inform print package has been updated
                log.LogMessage($"Print Package for order {orderID} completed.");
                events.Send(new PrintPackageReadyEvent(order.ID, order.LocationID.Value, order.ProjectPrefix));
            }
        }


        private void CreateOrderPrintPackage(PrintDB ctx, IOrder o, string path)
        {
            var fileName = $"Order-{o.ID}.zip";
            var zipFile = Path.Combine(path, fileName);
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Creating File {fileName}");
            try
            {
                using(FileStream fs = new FileStream(zipFile, FileMode.OpenOrCreate))
                {
                    fs.SetLength(0L);
                    using(ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Update))
                    {
                        AddOrderDocuments(archive, o);
                        using(var conn = connManager.OpenWebLinkDB())
                        {
                            AddOrderDataFiles(ctx, conn, archive, o);
                            AddUnitsPreviews(ctx, conn, archive, o);
                            AddLabelsAndImages(conn, archive, o);
                            AddArticlePreviews(conn, archive, o);
                        }
                    }
                }

                if(orderStore.TryGetFile(o.ID, out var file))
                {
                    var category = file.GetAttachmentCategory("PrintPackage");
                    if(!category.TryGetAttachment(fileName, out var attachment))
                        attachment = category.CreateAttachment(fileName);
                    attachment.SetContent(zipFile);
                }
            }
            finally
            {
                if(File.Exists(zipFile))
                    File.Delete(zipFile);
            }
        }

        private void AddOrderDocuments(ZipArchive archive, IOrder o)
        {
            if(orderDocuments.PreviewDocumentExists(o.ID, out _))
            {
                log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Getting preview document");
                var preview = orderDocuments.GetPreviewDocument(o.ID);
                using(var content = preview.GetContentAsStream())
                {
                    log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Adding preview document");
                    AddFileToArchive(archive, $"Documents/{Path.GetFileName(preview.FileName)}", content);
                }
            }

            if(orderDocuments.ProdSheetDocumentExists(o.ID, out _))
            {
                log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Getting prodsheet document");
                var prodSheet = orderDocuments.GetProdSheetDocument(o.ID);
                using(var content = prodSheet.GetContentAsStream())
                {
                    log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Adding prodsheet document");
                    AddFileToArchive(archive, $"Documents/{Path.GetFileName(prodSheet.FileName)}", content);
                }
            }
        }

        private void AddFileToArchive(ZipArchive archive, string entryName, Stream src)
        {
            var entry = archive.CreateEntry(entryName);
            using(Stream dst = entry.Open())
            {
                src.CopyTo(dst, 4096);
            }
        }

        public void AddFileToArchive(ZipArchive archive, string entryName, string fileContent)
        {
            var entry = archive.Entries.FirstOrDefault(x => x.Name.Equals(entryName));
            if(entry != null)
                entry.Delete();

            entry = archive.CreateEntry(entryName);
            using(StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                writer.Write(fileContent);
            }
        }

        private void AddUnitsPreviews(PrintDB ctx, IDBX conn, ZipArchive archive, IOrder o)
        {
            int i = 0;
            List<UPD> data = GetUnitPreviewData(conn, o);
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Adding {data.Count} unit previews");

            var map = new List<MapPreviewUPD>();

            Action<List<MapPreviewUPD>> WriteImagesOnFileAndClear = currentReady =>
            {
                try
                {
                    foreach(var taskResult in currentReady)
                    {
                        if(taskResult.Task.Exception != null)
                            throw taskResult.Task.Exception;// labelservice exception

                        AddFileToArchive(archive, $"UnitPreviews/L{taskResult.Product.LabelID}-{taskResult.Product.DetailID}.png", ((Task<Stream>)taskResult.Task).Result);
                    }
                }
                catch(Exception e)
                {

                    log.LogException("Error agregado archivo al printpackage", e);

                    throw;
                }
                finally
                {

                    map.Clear();
                }
            };


            foreach(var p in data)
            {
                i++;

                if(i % 50 == 0)
                    log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - {data.Count - i} unit previews remaining.");

                try
                {
                    var t = labelRepo.GetArticlePreviewAsync(p.LabelID, p.OrderID, p.DetailID);
                    map.Add(new MapPreviewUPD { Task = t, Product = p });

                }
                catch(Exception ex)
                {
                    log.LogException($"Error while creating Unit preview for: OrderID {p.OrderID}, LabelID: {p.LabelID}, DetailID: {p.DetailID}", ex);
                    throw;
                }

                if(map.Count >= 10)
                {
                    Task.WaitAll(map.Select(m => m.Task).ToArray());

                    WriteImagesOnFileAndClear(map);
                }
            }

            if(map.Count > 0)
            {
                Task.WaitAll(map.Select(m => m.Task).ToArray());

                WriteImagesOnFileAndClear(map);
            }

        }



        private void AddLabelsAndImages(IDBX conn, ZipArchive archive, IOrder o)
        {
            var labels = conn.Select<LabelData>(@"
				select l.* from Labels l 
					join Articles a on a.LabelID = l.ID
                    join PrinterJobs j on j.ArticleID = a.ID
				where j.CompanyOrderID = @orderid", o.ID);

            labels.AddRange(conn.Select<LabelData>(@"
				select distinct l.* from Labels l 
					join Artifacts art on art.LabelID = l.ID
					join PrinterJobs j on j.ArticleID = art.ArticleID
				where j.CompanyOrderID = @orderid", o.ID));

            if(labels.Count > 0)
                AddLabels(archive, labels, o);

            if(!projectStore.TryGetFile(o.ProjectID, out var container))
                throw new Exception($"Cannot find the container for project ID {o.ProjectID}");

            AddImageMetadata(archive, container, o);
        }


        // Adds label files and previews to Zip Archive
        private void AddLabels(ZipArchive archive, List<LabelData> labels, IOrder o)
        {
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Adding {labels.Count} labels.");
            int projectid;
            foreach(var labelGroup in labels.GroupBy(g => g.ID))
            {
                var label = labelGroup.First();

                projectid = label.ProjectID ?? 1;

                if(!projectStore.TryGetFile(projectid, out var container))
                    throw new Exception($"Cannot find the container for project ID {projectid}");

                // Add label files
                var labelsCategory = container.GetAttachmentCategory("Labels");
                if(!labelsCategory.TryGetAttachment(label.FileName, out var labelFile))
                    throw new Exception($"Cannot find Label File for label ID {label.ID}");

                using(var src = labelFile.GetContentAsStream())
                {
                    AddFileToArchive(archive, $"Labels/{label.FileName}", src);
                }

                // Add label previews
                var previewsCategory = container.GetAttachmentCategory("Previews");
                var previewFileName = Path.GetFileNameWithoutExtension(label.FileName) + "-preview.png";
                if(!previewsCategory.TryGetAttachment(previewFileName, out var previewFile))
                    continue;

                using(var src = previewFile.GetContentAsStream())
                {
                    AddFileToArchive(archive, $"LabelPreviews/{previewFile.FileName}", src);
                }
            }
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Done adding labels.");
        }


        // Add project images to zip archive
        private void AddImageMetadata(ZipArchive archive, IFileData container, IOrder o)
        {
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Adding image metadata.");
            var imageList = new List<FileProps>();
            var imagesCategory = container.GetAttachmentCategory("Images");
            foreach(var image in imagesCategory)
            {
                if(image.FileName.Contains("._thumb_."))
                    continue;
                imageList.Add(new FileProps()
                {
                    FileName = image.FileName,
                    UpdateDate = image.UpdatedDate
                });
            }
            var imageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(imageList));
            using(MemoryStream ms = new MemoryStream(imageContent))
            {
                AddFileToArchive(archive, "Images/images.json", ms);
            }
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Done adding image metadata.");
        }


        // Add project fonts to zip archive
        private void AddFonts(ZipArchive archive, IFileData container)
        {
            var fontList = new List<FileProps>();
            var fontsCategory = container.GetAttachmentCategory("Fonts");
            foreach(var font in fontsCategory)
            {
                fontList.Add(new FileProps()
                {
                    FileName = font.FileName,
                    UpdateDate = font.UpdatedDate
                });
            }
            var fontContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fontList));
            using(MemoryStream ms = new MemoryStream(fontContent))
            {
                AddFileToArchive(archive, "Fonts/fonts.json", ms);
            }
        }


        private void AddArticlePreviews(IDBX conn, ZipArchive archive, IOrder o)
        {
            var articles = conn.Select<Article>(@"
				    select a.* from Articles a 
                        join PrinterJobs j on j.ArticleID = a.ID
				    where j.CompanyOrderID = @orderid and a.LabelID is null", o.ID);

            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Adding {articles.Count} article previews.");
            foreach(var art in articles)
            {
                if(articlePreviewStore.TryGetFile(art.ID, out var preview))
                {
                    using(var src = preview.GetContentAsStream())
                    {
                        AddFileToArchive(archive, $"ArticlePreviews/{art.ID}/preview.png", src);
                    }
                }
            }
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Done adding article previews.");
        }


        /*
			* Adds all the json data that will be included in the print package
			*  - order.json            Record from CompanyOrders
			*  - issuer.json           Information of the company issuing the order
			*  - billto.json           Information of the company that will be billed
			*  - sendto.json           Information of the company that should receive the order once completed
			*  - sendtoAddress.json    Delivery Address
			*  - brand.json            Information of the brand
			*  - project.json          Information of the project
			*  - rfidConfig.json       RFID configuration
			*  - jobs.json             Order jobs (basically each distinct article in the order creates a "job")
			*  - details.json          Details of each job. Each job is in turn subdivided in what can be though of as subjobs, each row in the details points to the specific variable data (or product) that should be printed, and indicates how many units of each
			*  - articles.json         Information of all articles referenced in this order
			*  - artifacts.json        Information of article artifacts
			*  - labels.json           Information of all labels that will be required to print this order. Notice the package does not include the label files, just their information, label files will be synced separatelly for now (to reduce print package size).
			*  - variabledata.json     The variable data of the order (extracted from the dynamic catalogs)
		*/
        private void AddOrderDataFiles(PrintDB ctx, IDBX conn, ZipArchive archive, IOrder o)
        {
            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Adding Order Data Files");

            // Add the info of the order
            var order = conn.SelectOneToJson(@"
				    select top 1 o.*, p.ClientReference from CompanyOrders o    
                    left join CompanyProviders p on p.ID = o.ProviderRecordID                        
					where o.ID = @OrderID",
                o.ID);
            AddFileToArchive(archive, "Data/order.json", order.ToString());

            // Add the info of the company that issued this order (issuer, billto and sendto might be the same company)
            var company = conn.SelectOneToJson(@"
		            select * from Companies
					where ID = @CompanyID",
            o.CompanyID);
            AddFileToArchive(archive, "Data/issuer.json", company.ToString());

            // Add the info of the company that will be billed for this order (issuer, billto and sendto might be the same company)
            var billto = conn.SelectOneToJson(@"
				    select * from Companies
					where ID = @CompanyID",
            o.BillToCompanyID);
            AddFileToArchive(archive, "Data/billto.json", billto.ToString());

            // Add the info of the company that will receive the finished order (issuer, billto and sendto might be the same company)
            var sendto = conn.SelectOneToJson(@"
				    select * from Companies
					where ID = @CompanyID",
            o.SendToCompanyID);
            AddFileToArchive(archive, "Data/sendto.json", sendto.ToString());

            // Add the info of the company that will receive the finished order (issuer, billto and sendto might be the same company)
            var companyProvider = conn.SelectOneToJson(@"
				    SELECT *
                    FROM CompanyProviders
                    where CompanyID=  @CompanyID and ProviderCompanyID = @ProviderCompanyID",   
            o.CompanyID, o.SendToCompanyID);

            AddFileToArchive(archive, "Data/companyProvider.json", companyProvider.ToString());

            // Get the delivery address
            var sendtoaddress = conn.SelectOneToJson(@"
				    select * from Addresses
					where ID = @addressid",
            o.SendToAddressID);
            AddFileToArchive(archive, "Data/address.json", sendtoaddress.ToString());

            // Add the info of the brand
            var brand = conn.SelectOneToJson(@"
				    select b.* from Brands b
                    inner join Projects p on b.ID = p.BrandID                       
					where p.ID = @projectID",
            o.ProjectID);
            AddFileToArchive(archive, "Data/brand.json", brand.ToString());

            // Add the info of the location
            var location = conn.SelectOneToJson(@"
				    select top 1 l.ID LocationID, l.CompanyID, l.[Name], l.MaxNotEncodingQuantity, l.FscCode from Locations l
					left join CompanyOrders o on o.LocationID = l.ID
					where o.ID = @orderID",
            o.ID);
            if(location.Count > 0)
                AddFileToArchive(archive, "Data/location.json", location.ToString());

            // Add the info of the project
            var project = conn.SelectOneToJson(@"
				    select p.* from Projects p                        
					where p.ID = @projectID",
            o.ProjectID);
            AddFileToArchive(archive, "Data/project.json", project.ToString());

            // Add the information of all articles referenced from this order
            var articles = conn.SelectToJson(@"
				    select a.* from Articles a 
                        join PrinterJobs j on j.ArticleID = a.ID
				    where j.CompanyOrderID = @orderid", o.ID);
            AddFileToArchive(archive, "Data/articles.json", articles.ToString());

            // Get the details of each artifact referenced from this order (this contains instructions for each artifact)
            var artifacts = conn.SelectToJson(@"
				    select art.* from Artifacts art
                        join PrinterJobs j on j.ArticleID = art.ArticleID
				    where j.CompanyOrderID = @orderid", o.ID);
            AddFileToArchive(archive, "Data/artifacts.json", artifacts.ToString());

            // Add labels directly referenced from the order + labels referenced as artifacts
            var labels = conn.SelectToJson(@"
				    select distinct l.* from Labels l 
                        join Articles a on a.LabelID = l.ID
                        join PrinterJobs j on j.ArticleID = a.ID
				    where j.CompanyOrderID = @orderid1
                    UNION
                    select distinct l.* from Labels l 
                        join Artifacts art on art.LabelID = l.ID
                        join PrinterJobs j on j.ArticleID = art.ArticleID
				    where j.CompanyOrderID = @orderid2", o.ID, o.ID);

            AddFileToArchive(archive, "Data/labels.json", labels.ToString());

            // Add the variable data for the order, extracted from the dynamic catalogs
            var exportedData = catDataRepo.ExportData(o.ProjectID, "Orders", true, new NameValuePair("ID", o.OrderDataID));
            exportedData.RemoveAll(p => String.IsNullOrWhiteSpace(p.Records) || p.Records.Length < 3 || p.Name.StartsWith("REL_"));
            AddFileToArchive(archive, "Data/variabledata.json", JsonConvert.SerializeObject(exportedData));

            // Add the RFID configuration
            var rfid = rfidRepo.SearchRFIDConfig(ctx, o.ProjectID);
            if(rfid == null || rfid.SerializedConfig == null)
                throw new InvalidOperationException($"Cannot process this order because no valid RFID configuration has been found. CompanyID: {o.CompanyID}, ProjectID: {o.ProjectID}.");
            AddFileToArchive(archive, "Data/rfidconfig.json", rfid.SerializedConfig);

            var rfidLabel = labels.Where(p => (p as JObject).GetValue<bool>("EncodeRFID")).FirstOrDefault();
            if(rfidLabel != null)
            {
                var rfidConfigContext = factory.GetInstance<IConfigurationContext>();
                var rfidEncoding = rfidConfigContext.GetInstance<RFIDConfigurationInfo>(rfid.SerializedConfig).Process;
                if(rfidEncoding is IPrintPackageDataProcessor dataProcessor)
                {
                    dataProcessor.AddPrintPackageData(new PrintPackage(archive), o.ID, o.OrderNumber, exportedData);
                }
            }

            // Add each job associated to this order
            var jobs = conn.SelectToJson(@"
                    select * from PrinterJobs
                    where CompanyOrderID = @orderid", o.ID);
            AddFileToArchive(archive, "Data/jobs.json", jobs.ToString());

            // Add the details of each job
            var jobdetails = conn.SelectToJson(@"
                    select d.* from PrinterJobDetails d 
                        join PrinterJobs j on d.PrinterJobID = j.ID
                        where j.CompanyOrderID = @orderid", o.ID);
            AddFileToArchive(archive, "Data/details.json", jobdetails.ToString());

            var info = orderRepo.GetProjectInfo(o.ID);
            var inlays = string.Empty;
            var configInlay = string.Empty;

            //2021_inlay_isaac_I01 begin
            //Se manda a llamar los inlays desde el repositorio, donde se valida por cada valor, y se regresa por la siguiente jerarquia
            //Si encuentra datos en proyectos ya no toma en cuenta los demás, si no encuentra datos, se pasa al siguiente que es
            //brand, si encuentra datos regresa la configuración por brand, de no se así se buscará por compañías
            // si no se encuentra nada se regresa null.
            var _inlayResult = inLayRepository.GetInlays(ctx, info.ProjectID, info.BrandID, info.CompanyID);
            inlays = JsonConvert.SerializeObject(_inlayResult.ToList());

            var _inlayConfigresult = inLayRepository.GetInLayConfig(ctx, info.ProjectID, info.BrandID, info.CompanyID);
            configInlay = JsonConvert.SerializeObject(_inlayConfigresult.ToList());

            AddFileToArchive(archive, "Data/inlays.json", string.IsNullOrEmpty(inlays) ? "[{}]" : inlays);
            AddFileToArchive(archive, "Data/inlaysconfig.json", string.IsNullOrEmpty(configInlay) ? "[{}]" : configInlay);
            //2021_inlay_isaac_I01 end

            log.LogMessage($"CreatePrintPackage(Order:{o.ID}) - Done adding Order Data Files");
        }


        private List<UPD> GetUnitPreviewData(IDBX conn, IOrder o)
        {
            var label = conn.SelectOne<LabelData>(@"
					select * from PrinterJobs j
						join Articles a on j.ArticleID = a.ID
						join Labels l on a.LabelID = l.ID
					where j.CompanyOrderID = @oid", o.ID);

            if(label == null)
                return new List<UPD>(); // Article is not a label, return an empty list of "unit preview data"

            var catalog1 = conn.SelectOne<Catalog>(@"
				select * from Catalogs
					where 
						ProjectID = @projectid and
						Name = @catalogName",
                o.ProjectID, "OrderDetails");

            var catalog2 = conn.SelectOne<Catalog>(@"
				select * from Catalogs
					where 
						ProjectID = @projectid and
						Name = @catalogName",
                o.ProjectID, "VariableData");

            var groupingField = GetGroupingColumn(label.GroupingFields);


            // Get information required to generate label previews
            var previewDetails = conn.Select<UPD>($@"
					select min(l.ID) as LabelID, min(j.CompanyOrderID) as OrderID, min(jd.ProductDataID) as DetailID, vpd.{groupingField}
					from Labels l 
						join Articles a on a.LabelID = l.ID
						join PrinterJobs j on j.ArticleID = a.ID
						join PrinterJobDetails jd on jd.PrinterJobID = j.ID
						join {connManager.CatalogDB}.dbo.OrderDetails_{catalog1.CatalogID} vod on vod.ID = jd.ProductDataID
						join {connManager.CatalogDB}.dbo.VariableData_{catalog2.CatalogID} vpd on vod.Product = vpd.ID
					where j.CompanyOrderID = @orderid
					group by vpd.{groupingField}
				", o.ID);

            if(previewDetails == null || previewDetails.Count == 0)
                throw new Exception($"Order {o.ID} does not have OrderDetails in Variable Data.");

            var detailIDs = previewDetails.Merge(",", r => r.DetailID.ToString());

            previewDetails.AddRange(conn.Select<UPD>($@"
					select l.ID as LabelID, j.CompanyOrderID as OrderID, jd.ProductDataID as DetailID
					from Artifacts artf 
						join Articles a on artf.ArticleID = a.ID
						join Labels l on artf.LabelID = l.ID
						join PrinterJobs j on j.ArticleID = a.ID
						join PrinterJobDetails jd on jd.PrinterJobID = j.ID
					where j.CompanyOrderID = @orderid and jd.ProductDataID in ({detailIDs})
				", o.ID));

            return previewDetails;
        }

        class GroupingColumnInfo
        {
            public string GroupingFields;
            public string DisplayFields;
        }

        private string GetGroupingColumn(string groupingFields)
        {
            // NOTE: Any error at this point is due to missconfiguration, to fix the problem fix the system configuration, then reprocess the print package. Until that is done, this bit of code will keep throwing and prevent the order from moving to the Pending state...
            if(String.IsNullOrWhiteSpace(groupingFields))
                return "Barcode";
            var grouping = JsonConvert.DeserializeObject<GroupingColumnInfo>(groupingFields);
            if(String.IsNullOrWhiteSpace(grouping.GroupingFields))
                return "Barcode";
            string[] tokens = grouping.GroupingFields.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return tokens[0];
        }

        class UPD
        {
            public int LabelID;
            public int OrderID;
            public int DetailID;
        }

        class FileProps
        {
            public string FileName;
            public DateTime UpdateDate;
        }

        class cOrderJobs
        {
            public string ArticleCode;
            public Guid PrintCountSequence;
            public int PrintCountSequenceType;
            public string PrintCountSelectorField;
            public SelectorType PrintCountSelectorType;
            public int ProductDataID;
            public int Quantity;
        }

        class MapPreviewUPD
        {
            public Task Task;
            public UPD Product;
        }

        class PrintPackage : IPrintPackage
        {
            private readonly ZipArchive archive;

            public PrintPackage(ZipArchive archive)
            {
                this.archive = archive;
            }

            public string GetFile(string fileName)
            {
                return archive.GetFileContent(fileName);
            }

            public void AddFile(string fileName, string content)
            {
                archive.AddFile(fileName, content);
            }
        }
    }
}
