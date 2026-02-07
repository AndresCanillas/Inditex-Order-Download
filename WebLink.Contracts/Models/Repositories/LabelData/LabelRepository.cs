using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.LabelService;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class LabelRepository : GenericRepository<ILabelData, LabelData>, ILabelRepository
	{
		private IRemoteFileStore store;
		private IFileStoreManager storeManager;
		private IBLabelServiceClient labelService;
		private IProjectRepository projectRepo;
		private IRFIDConfigRepository rfidRepo;
		private IAppConfig config;
		private ILogService log;

		public LabelRepository(
			IFactory factory,
			IProjectRepository projectRepo,
			IRFIDConfigRepository rfidRepo,
			IAppConfig config,
			IFileStoreManager storeManager,
			IBLabelServiceClient labelService,
			ILogService log
			)
			:base(factory, (ctx)=>ctx.Labels)
        {
            this.projectRepo = projectRepo;
            this.rfidRepo = rfidRepo;
            this.config = config;
            this.labelService = labelService;
            this.storeManager = storeManager;
            store = storeManager.OpenStore("ProjectStore");
            this.labelService.Url = config["WebLink:LabelService"];
            this.log = log;
        }


        protected override string TableName { get => "Labels"; }


        protected override void AfterInsert(PrintDB ctx, IUserData userData, LabelData actual)
        {
            var modified = false;
			if (string.IsNullOrEmpty(actual.GroupingFields))
            {
                var groupingData = JsonConvert.SerializeObject(new LabelGroupingData());
                actual.GroupingFields = groupingData;
                modified = true;
            }
			if (modified)
                ctx.SaveChanges();
        }


		protected override void UpdateEntity(PrintDB ctx, IUserData userData, LabelData actual, ILabelData data)
		{
			actual.Name = data.Name;
			actual.Comments = data.Comments;
			actual.PreviewData = data.PreviewData;
			if(userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign))
			{
				actual.ProjectID = data.ProjectID;
				actual.DoubleSide = data.DoubleSide;
				actual.EncodeRFID = data.EncodeRFID;
				actual.IsSerialized = data.IsSerialized;
				actual.Mappings = data.Mappings;
				actual.Type = data.Type;
				actual.MaterialID = data.MaterialID;
				actual.LabelsAcross = data.LabelsAcross;
				actual.Rows = data.Rows;
				actual.Cols = data.Cols;
				actual.RequiresDataEntry = data.RequiresDataEntry;
				actual.FileName = data.FileName;

                actual.IncludeComposition = data.IncludeComposition;
                actual.ShoeComposition = data.ShoeComposition;
                actual.IncludeCareInstructions = data.IncludeCareInstructions;
                actual.IsValid = data.IsValid;
                if(data.IsValid)
                {
                    actual.IsValidBy = userData.UserName;
                    actual.IsValidDate = DateTime.Now;
                }
			}
		}


        public List<ILabelData> GetByProjectID(int projectid)
        {
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                return GetByProjectID(ctx, projectid);
            }
        }


        public List<ILabelData> GetByProjectID(PrintDB ctx, int projectid)
        {
            var project = projectRepo.GetByID(ctx, projectid);
            return new List<ILabelData>(
                All(ctx).Where(p => p.ProjectID == null || p.ProjectID == projectid)
            );
        }


        public List<string> GetGroupingFields(int id)
        {
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                return GetGroupingFields(ctx, id);
            }
        }


        public List<string> GetGroupingFields(PrintDB ctx, int id)
        {
            List<string> fieldsList = null;
            var labelFields = ctx.Labels.Where(c => c.ID == id).Single().GroupingFields;

			if (!string.IsNullOrEmpty(labelFields))
            {
                var labelData = JsonConvert.DeserializeObject<LabelGroupingData>(labelFields);

                fieldsList = labelData.DisplayFields.Split(',').Select(x => x).ToList();
                fieldsList.Insert(0, labelData.GroupingFields);
            }

            return fieldsList;
        }

        public string GetComparerField(int id)
        {
            using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                return ctx.Labels.Where(c => c.ID == id).Single().ComparerField;
            }
        }


        public void UpdateGroupingFields(int id, string data)
        {
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                UpdateGroupingFields(ctx, id, data);
            }
        }


        public void UpdateGroupingFields(PrintDB ctx, int id, string data)
        {
            var label = ctx.Labels.Where(c => c.ID == id).Single();
            label.GroupingFields = data;
            ctx.SaveChanges();
        }


        public void UpdateComparerField(int id, string data)
        {
            using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                UpdateComparerField(ctx, id, data);
            }
        }


        public void UpdateComparerField(PrintDB ctx, int id, string data)
        {
            var label = ctx.Labels.Where(c => c.ID == id).Single();
            label.ComparerField = data;
            ctx.SaveChanges();
        }


        public Guid GetLabelFileReference(int labelid)
        {
            var label = GetByID(labelid);     // Ensures label exists and user has permissions to access it

            var projectid = 1;
			if (label.ProjectID != null)
                projectid = label.ProjectID.Value;

            var container = store.GetOrCreateFile(projectid, Project.FILE_CONTAINER_NAME);
            var labelCategory = container.GetAttachmentCategory("Labels");
			if (!labelCategory.TryGetAttachment(label.FileName, out var file))
                throw new Exception($"File for label {labelid} could not be found.");

            return file.FileGUID;
        }


        public NiceLabelInfo UploadFile(int labelid, string fileName, Stream content)
        {
            var label = GetByID(labelid);    // Ensures label exists and user has permissions to access it
            var userData = base.factory.GetInstance<IUserData>();
            var projectid = 1;
			if (label.ProjectID != null)
                projectid = label.ProjectID.Value;

            // Upload label file to file repository as an attachment
            var container = store.GetOrCreateFile(projectid, Project.FILE_CONTAINER_NAME);
            var labelCategory = container.GetAttachmentCategory("Labels");
			if (!labelCategory.TryGetAttachment(fileName, out var file))
                file = labelCategory.CreateAttachment(fileName);
            file.SetContent(content);

            // If the label file name was changed, then delete the old attachment and update the label filename field
            if(String.Compare(label.FileName, fileName, true) != 0)
            {
				if (!String.IsNullOrWhiteSpace(label.FileName) && labelCategory.TryGetAttachment(label.FileName, out var oldFile))
                    oldFile.Delete();

				using (var ctx = base.factory.GetInstance<PrintDB>())
                {
                    label.FileName = fileName;
                    label.UpdatedFileBy = userData.UserName;
                    label.UpdatedFileDate = DateTime.Now;
                    ctx.Update(label);
                    ctx.SaveChanges();
                }
            }


            // Update the label properties (IsDataBound, Width & Height)

            var info = new NiceLabelInfo();
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                info = GetLabelInfo(label.ID).Result;
                SetAutoFields(DBOperation.Update, userData, (LabelData)label);
                label.IsDataBound = info.IsDataBound;
                label.Width = info.Width;
                label.Height = info.Height;
                label.Rows = info.Rows;
                label.Cols = info.Columns;
                label.UpdatedFileBy = userData.UserName;
                label.UpdatedFileDate = DateTime.Now;
                ctx.Update(label);
                ctx.SaveChanges();
            }

            return info;
        }


        public Stream DownloadFile(int labelid, out string fileName)
        {
            var label = GetByID(labelid);    // Ensures label exists and user has permissions to access it

            var projectid = 1;
			if (label.ProjectID != null)
                projectid = label.ProjectID.Value;

			if (!store.TryGetFile(projectid, out var container))
                throw new InvalidOperationException($"File for label {labelid} could not be found.");

            var labelsCategory = container.GetAttachmentCategory("Labels");
			if (!labelsCategory.TryGetAttachment(label.FileName, out var file))
                throw new InvalidOperationException($"File for label {labelid} could not be found.");

            fileName = file.FileName;
            return file.GetContentAsStream();
        }


        public Stream GetLabelPreview(int labelid)
        {
            var label = GetByID(labelid);   // Ensures label exists and user has permissions to access it
            IFileData container;

            var projectid = 1;
			if (label.ProjectID != null)
                projectid = label.ProjectID.Value;

			if (!store.TryGetFile(projectid, out container))
                return null;

            var previewsCategory = container.GetAttachmentCategory("Previews");
            var fnwe = Path.GetFileNameWithoutExtension(label.FileName);
			if (!previewsCategory.TryGetAttachment($"{fnwe}-preview.png", out var previewFile))
                return null;

            return previewFile.GetContentAsStream();
        }


        public Guid GetLabelPreviewReference(int labelid)
        {
            var label = GetByID(labelid);   // Ensures label exists and user has permissions to access it
            IFileData container;

            var projectid = 1;
			if (label.ProjectID != null)
                projectid = label.ProjectID.Value;

			if (!store.TryGetFile(projectid, out container))
                return Guid.Empty;

            var previewsCategory = container.GetAttachmentCategory("Previews");
            var fnwe = Path.GetFileNameWithoutExtension(label.FileName);
			if (!previewsCategory.TryGetAttachment($"{fnwe}-preview.png", out var previewFile))
                return Guid.Empty;

            return previewFile.FileGUID;
        }


        // NOTE: Currently not usable
        public async Task SetLabelPreviewWithVariablesAsync(int labelid, string previewData)
        {
            var userData = base.factory.GetInstance<IUserData>();
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                var label = await base.GetByIDAsync(ctx, labelid);
                SetAutoFields(DBOperation.Update, userData, (LabelData)label);
                await base.UpdateAsync(ctx, label);

                List<LabelMapping> mappings = null;
				if (label.Mappings != null && label.Mappings.Length > 0)
                    mappings = JsonConvert.DeserializeObject<List<LabelMapping>>(label.Mappings);

                IVariableData data = VariableData.FromJson(previewData);
                LoadPredefinedFields(label, mappings, data);

                var labelFile = await store.GetFileAttachmentAsync(label.ProjectID ?? 1, "Labels", label.FileName);

                var cfg = new ArticlePreviewRequest2()
                {
                    ProjectID = label.ProjectID ?? 1,
                    LabelID = label.ID,
                    FileName = label.FileName,
                    LabelFileGUID = labelFile.FileGUID,
                    FileUpdateDate = label.UpdatedDate.ToString("yyyy/MM/dd HH:mm:ss"),
                    PreviewSide = label.DoubleSide ? LabelSide.Both : LabelSide.Front,
                    UseDefaultSize = false,
                    IncludeWaterMark = false,
                    VariableData = data.Data,
                    AttachAsLabelPreview = true,
                    IncludeResultInResponse = false
                };

                var response = await labelService.GetArticlePreview2Async(cfg);
				if (!response.Success)
                    throw new LabelRepositoryLabelPreviewException($"Could not create label preview LabelID: {labelid} using direct variable values. LabelService response was: {response.ErrorMessage}, see the LabelService log file for additional details.");
            }
        }


        private void LoadPredefinedFields(ILabelData label, List<LabelMapping> mappings, IVariableData data)
        {
            // ISSUE: Is not possible to populate RFID Encoding without the exported data. So for labels that use fields from the RFIDEncoding predefined table this will never work (we wont be able to correctly initialize fields like TrackingCode for instance)
            int projectid = label.ProjectID ?? 1;
			if (label.EncodeRFID)
            {
                //AddRFIDRecord(rfidEncoding, data, true);
            }
        }


        public async Task SetLabelPreviewAsync(int labelid, int orderid, int variableDataDetailID)
        {
            using(var ctx = base.factory.GetInstance<PrintDB>())
            {
                await SetLabelPreviewAsync(ctx, labelid, orderid, variableDataDetailID);
            }
        }


        public async Task SetLabelPreviewAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID)
        {
            var request = await CreateArticlePreviewRequest(ctx, labelid, orderid, variableDataDetailID, true);
            var response = await labelService.GetArticlePreviewAsync(request);
			if (!response.Success)
                throw new LabelRepositoryLabelPreviewException($"Could not create label preview LabelID: {labelid} and OrderID: {orderid}. LabelService response was: {response.ErrorMessage}, see the LabelService log file for additional details.");
        }


        public async Task<Stream> GetArticlePreviewAsync(int labelid, int orderid, int variableDataDetailID)
        {
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                return await GetArticlePreviewAsync(ctx, labelid, orderid, variableDataDetailID);
            }
        }


        public async Task<Stream> GetArticlePreviewAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID)
        {
            var request = await CreateArticlePreviewRequest(ctx, labelid, orderid, variableDataDetailID, false);
            var response = await labelService.GetArticlePreviewAsync(request);
			if (!response.Success)
                throw new LabelRepositoryLabelPreviewException($"Could not create label preview LabelID: {labelid} and OrderID: {orderid}. LabelService response was: {response.ErrorMessage}, see the LabelService log file for additional details.", labelid);
            return storeManager.GetFile(response.FileGUID).GetContentAsStream();
        }


        public async Task<Guid> GetArticlePreviewReferenceAsync(int labelid, int orderid, int variableDataDetailID)
        {
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                return await GetArticlePreviewReferenceAsync(ctx, labelid, orderid, variableDataDetailID);
            }
        }


        public async Task<Guid> GetArticlePreviewReferenceAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID)
        {
            var request = await CreateArticlePreviewRequest(ctx, labelid, orderid, variableDataDetailID, false);
            var response = await labelService.GetArticlePreviewAsync(request);
			if (!response.Success)
                throw new LabelRepositoryLabelPreviewException($"Could not create label preview for LabelID: {labelid} and OrderID: {orderid}. LabelService response was: {response.ErrorMessage}, see the LabelService log file for additional details.");
            return response.FileGUID;
        }


        public async Task<string> PrintArticleAsync(int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample)
        {
			using (var ctx = base.factory.GetInstance<PrintDB>())
            {
                return await PrintArticleAsync(ctx, labelid, orderid, variableDataDetailID, driverName, settings, isSample);
            }
        }

        public async Task<string> PrintArticleByQuantityAsync(int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample)
        {
            using(var ctx = base.factory.GetInstance<PrintDB>())
            {
                return await PrintArticleByQuantityAsync(ctx, labelid, orderid, variableDataDetailID, driverName, settings, isSample);
            }
        }

        public async Task<string> PrintArticleByQuantityAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample)
        {
            var request = await CreatePrintToFileRequest(ctx, labelid, orderid, variableDataDetailID, driverName, settings, isSample);
            var response = await labelService.PrintArticleByQuantityAsync(request);
            if(!response.Success)
                throw new Exception($"Could not create label preview LabelID: {labelid} and OrderID: {orderid}. LabelService response was: {response.ErrorMessage}, see the LabelService log file for additional details.");

            var fileContent = storeManager.GetFile(response.FileGUID).GetContentAsBytes();
            log.LogMessage($"PrintArticleAsync completed, returned {fileContent.Length} bytes.");
            return Encoding.UTF8.GetString(fileContent);
        }

        public async Task<string> PrintArticleAsync(PrintDB ctx, int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample)
        {
            var request = await CreatePrintToFileRequest(ctx, labelid, orderid, variableDataDetailID, driverName, settings, isSample);
            var response = await labelService.PrintArticleAsync(request);
			if (!response.Success)
                throw new Exception($"Could not create label preview LabelID: {labelid} and OrderID: {orderid}. LabelService response was: {response.ErrorMessage}, see the LabelService log file for additional details.");

            var fileContent = storeManager.GetFile(response.FileGUID).GetContentAsBytes();
            log.LogMessage($"PrintArticleAsync completed, returned {fileContent.Length} bytes.");
            return Encoding.UTF8.GetString(fileContent);
        }


        private async Task EnsureCanReadAsync(string fileName)
        {
            var retryCount = 0;
            var waitTime = 50;
			while (true)
            {
                try
                {
					using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) { }
                    return;
                }
				catch (Exception)
                {
                    retryCount++;
                    await Task.Delay(waitTime);
                    waitTime += 50;
					if (retryCount >= 5)
                        throw;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="labelid"></param>
        /// <param name="orderid"></param>
        /// <param name="orderDetailID"> PrinterJobDetail.ProductDataID === OrderDetails.ID </param>
        /// <param name="attachToLabel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<ArticlePreviewRequest> CreateArticlePreviewRequest(PrintDB ctx, int labelid, int orderid, int orderDetailID, bool attachToLabel)
        {
            var label = await ctx.Labels.Where(p => p.ID == labelid).FirstOrDefaultAsync();
			if (label == null)
                throw new Exception($"Label [{labelid}] could not be found.");

            int projectid = label.ProjectID ?? 1;// if project ID is a shared label

            var order = await ctx.CompanyOrders.Where(o => o.ID == orderid).FirstOrDefaultAsync();
			if (order == null)
                throw new Exception($"Order [{orderid}] could not be found.");

            var factoryLocation = await ctx.Locations.Where(w => w.ID == order.LocationID).FirstOrDefaultAsync();
            var ordersCatalog = await ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "Orders").FirstOrDefaultAsync();
            var detailsCatalog = await ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "OrderDetails").FirstOrDefaultAsync();
            var printJobDetailInfo = await ctx.PrinterJobDetails.Join(ctx.PrinterJobs, jd => jd.PrinterJobID, job => job.ID, (d, j) => new { PrinterJobDetail = d, OrderID = j.CompanyOrderID })
                .Where(w => w.OrderID == order.ID && w.PrinterJobDetail.ProductDataID == orderDetailID)
                .Select(s => s.PrinterJobDetail).SingleAsync();

            if (ordersCatalog == null)
                throw new Exception($"Orders Catalog for project [{order.ProjectID}] could not be found.");

			if (detailsCatalog == null)
                throw new Exception($"OrderDetails Catalog for project [{order.ProjectID}] could not be found.");

            if(factoryLocation == null)
                throw new Exception($"Location Row ID [{order.LocationID}] could not be found.");

            List<LabelMapping> mappings = null;
			if (label.Mappings != null && label.Mappings.Length > 0)
                mappings = JsonConvert.DeserializeObject<List<LabelMapping>>(label.Mappings);

            List<TableData> data;
			using (var db = factory.GetInstance<DynamicDB>())
            {
                db.Open(config.GetValue<string>("Databases.CatalogDB.ConnStr"));
                data = db.ExportData(ordersCatalog.CatalogID, false, new NameValuePair("ID", order.OrderDataID));
                data.AddRange(db.ExportData(detailsCatalog.CatalogID, true, new NameValuePair("ID", orderDetailID)));
            }

			if (label.EncodeRFID)
            {
                var rfidEncoding = rfidRepo.GetEncodingProcess(ctx, projectid);
				if (rfidEncoding == null)
                    throw new InvalidOperationException($"Could not find any valid RFID configuration for project {projectid}");

                AddRFIDRecord(orderid, order.OrderNumber, orderDetailID, rfidEncoding, data, true);
            }

            var serial = 87564;

            // Add SerialNumbers table
            {
                var variableData = data.First(t => t.Name == "VariableData");
                var fields = typeof(SerialNumbersInfo).GetCatalogDefinition().Fields;
                var productField = fields.Where(f => f.Name == "Product").First();
                productField.Type = ColumnType.Reference;
                productField.CatalogID = variableData.CatalogID;
                var productArray = JArray.Parse(variableData.Records);
                var product = productArray[0] as JObject;
                var productid = product.GetValue<int>("ID");
                var barcode = product.GetValue<string>("Barcode");
                data.Add(new TableData()
                {
                    Name = "SerialNumbers",
                    Fields = JsonConvert.SerializeObject(fields),
                    Records = JsonConvert.SerializeObject(new SerialNumbersInfo[] { new SerialNumbersInfo() { Product = productid, Barcode = barcode, Serial = serial } })
                });
            }


            // Add GlobalSettings table
            {
                var fields = typeof(GlobalSettingsInfo).GetCatalogDefinition().Fields;
                var resourcePath = GetResourceDirectory(projectid);
                var projectName = (await projectRepo.GetByIDAsync(ctx, projectid)).Name;
                data.Add(new TableData()
                {
                    Name = "GlobalSettings",
                    Fields = JsonConvert.SerializeObject(fields),
                    Records = JsonConvert.SerializeObject(new GlobalSettingsInfo[] { new GlobalSettingsInfo() { ID = 1, FactoryID = factoryLocation.ID, PrintCount = serial, ResourcePath = resourcePath, FscCode = factoryLocation.FscCode, ProjectName = projectName } })
                });
            }

            var labelFile = await store.GetFileAttachmentAsync(projectid, "Labels", label.FileName);

            var cfg = new ArticlePreviewRequest()
            {
                ProjectID = projectid,
                LabelID = labelid,
                FileName = label.FileName,
                LabelFileGUID = labelFile.FileGUID,
                FileUpdateDate = label.UpdatedDate.ToString("yyyy/MM/dd HH:mm:ss"),
                DetailID = printJobDetailInfo.ID,
                PreviewSide = label.DoubleSide ? LabelSide.Both : LabelSide.Front,
                UseDefaultSize = false,
                IncludeWaterMark = false,
                ExportedData = data,
                Mappings = mappings,
                AttachAsLabelPreview = attachToLabel
            };

            return cfg;
        }

        public string GetResourceDirectory(int projectid)
        {
			if (!store.TryGetFile(projectid, out var container))
                throw new Exception($"Could not locate project file container for project {projectid}.");

            var dir = Path.Combine(container.PhysicalPath, "Images");
			if (!dir.EndsWith("\\"))
                dir += "\\";
            return dir;
        }

        private async Task<PrintToFileRequest> CreatePrintToFileRequest(PrintDB ctx, int labelid, int orderid, int variableDataDetailID, string driverName, IPrinterSettings settings, bool isSample)
        {
            var label = await ctx.Labels.Where(p => p.ID == labelid).FirstOrDefaultAsync();
			if (label == null)
                throw new Exception($"Label {labelid} could not be found.");

            int projectid = label.ProjectID ?? 1;

            var order = await ctx.CompanyOrders.Where(o => o.ID == orderid).FirstOrDefaultAsync();
			if (order == null)
                throw new Exception($"Order {orderid} could not be found.");

            var ordersCatalog = await ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "Orders").FirstOrDefaultAsync();
			if (ordersCatalog == null)
                throw new Exception($"Orders Catalog for project {order.ProjectID} could not be found.");

            var detailsCatalog = await ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "OrderDetails").FirstOrDefaultAsync();
			if (detailsCatalog == null)
                throw new Exception($"OrderDetails Catalog for project {order.ProjectID} could not be found.");

            List<LabelMapping> mappings = null;
			if (label.Mappings != null && label.Mappings.Length > 0)
                mappings = JsonConvert.DeserializeObject<List<LabelMapping>>(label.Mappings);

            List<TableData> data;
			using (var db = factory.GetInstance<DynamicDB>())
            {
                db.Open(config.GetValue<string>("Databases.CatalogDB.ConnStr"));
                data = db.ExportData(ordersCatalog.CatalogID, false, new NameValuePair("ID", order.OrderDataID));
                data.AddRange(db.ExportData(detailsCatalog.CatalogID, true, new NameValuePair("ID", variableDataDetailID)));
            }

			if (label.EncodeRFID)
            {
                var rfidEncoding = rfidRepo.GetEncodingProcess(ctx, projectid);
				if (rfidEncoding == null)
                    throw new InvalidOperationException($"Could not find any valid RFID configuration for project {projectid}");
                AddRFIDRecord(orderid, order.OrderNumber, variableDataDetailID, rfidEncoding, data, isSample);
            }

            var fields = typeof(GlobalSettingsInfo).GetCatalogDefinition().Fields;
            var resourcePath = GetResourceDirectory(projectid);
            var projectName = (await projectRepo.GetByIDAsync(ctx, projectid)).Name;    
            data.Add(new TableData()
            {
                Name = "GlobalSettings",
                Fields = JsonConvert.SerializeObject(fields),
                Records = JsonConvert.SerializeObject(new GlobalSettingsInfo[] { new GlobalSettingsInfo() { ID = 1, ResourcePath = resourcePath, FactoryID = 1, ProjectName = projectName } })
            });

            var labelFile = await store.GetFileAttachmentAsync(projectid, "Labels", label.FileName);

            var cfg = new PrintToFileRequest()
            {
                ProjectID = projectid,
                LabelID = labelid,
                FileName = label.FileName,
                LabelFileGUID = labelFile.FileGUID,
                FileUpdateDate = label.UpdatedDate.ToString("yyyy/MM/dd HH:mm:ss"),
                DetailID = variableDataDetailID,
                DriverName = driverName,
                XOffset = settings.XOffset.ToString("0.000"),
                YOffset = settings.YOffset.ToString("0.000"),
                Darkness = settings.Darkness,
                Speed = settings.Speed,
                ChangeOrientation = settings.ChangeOrientation,
                Rotated = settings.Rotated,
                Mappings = mappings,
                ExportedData = data
            };

            return cfg;
        }


        private void AddRFIDRecord(int orderID, string orderNumber, int detailID, ITagEncodingProcess encodingAlgorithm, List<TableData> data, bool isSampleOrPreview)
        {
            int productID = 0;
            JObject productData = null;
            TableData variableData = null;
            TableData detailsData = null;
            JArray detailRows = null;

            try
            {
                variableData = data.First(t => t.Name == "VariableData");
				if (String.IsNullOrWhiteSpace(variableData.Records))
                    throw new InvalidOperationException("VariableData table is empty");
            }
			catch (Exception ex)
            {
                throw new InvalidOperationException($"Was unable to find table VariableData in given exportedData", ex);
            }

            try
            {
                detailsData = data.First(t => t.Name == "OrderDetails");
				if (String.IsNullOrWhiteSpace(detailsData.Records))
                    throw new InvalidOperationException("OrderDetails table is empty");
            }
			catch (Exception ex)
            {
                throw new InvalidOperationException($"Was unable to find table OrderDetails in given exportedData", ex);
            }

            try
            {
                detailRows = JArray.Parse(detailsData.Records);
				if (detailRows == null || detailRows.Count == 0)
                    throw new InvalidOperationException("OrderDetails.Records is an empty array");
            }
			catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not parse OrderDetails.Records as an array or it is empty", ex);
            }

            try
            {
                productID = (detailRows[0] as JObject).GetValue<int>("Product");
                productData = data.FlattenObject("VariableData", productID);
            }
			catch (Exception ex)
            {
                throw new InvalidOperationException($"Was unable to read product id or product data from given exportedData. ProductID: {productID}, ProductData: {productData?.ToString()}", ex);
            }

            TagEncodingInfo tag;
			if (isSampleOrPreview)
                tag = encodingAlgorithm.EncodeSample(productData);
            else
                tag = encodingAlgorithm.Encode(new EncodeRequest(orderID, orderNumber, detailID, productData, 1, 1))[0];

            var fields = typeof(TagEncodingInfoUI).GetCatalogDefinition().Fields;
            //fields.Add(new FieldDefinition() { FieldID = fields.Count, Type = ColumnType.Int, Name = "Product" });
            var rfidEncoding = new JArray();
            var tagRow = JObject.Parse(JsonConvert.SerializeObject(tag));
            tagRow.Add("Product", JToken.FromObject(productID));
            //tagRow["Product"] = productID;

            rfidEncoding.Add(tagRow);
            data.Add(new TableData()
            {
                Name = "RFIDEncoding",
                Fields = JsonConvert.SerializeObject(fields),
                Records = rfidEncoding.ToString()
            });
        }


        public async Task<NiceLabelInfo> GetLabelInfo(int id)
        {
            var label = await GetByIDAsync(id);     //Ensures label exists and user has permissions to access it

            var projectid = 1;
			if (label.ProjectID != null)
                projectid = label.ProjectID.Value;

			if (store.TryGetFile(projectid, out var container))
            {
				if (!container.GetAttachmentCategory("Labels").TryGetAttachment(label.FileName, out var labelFile))
                    throw new Exception($"Could not locate label file for label {id}");
				var cfg = new LabelInfoRequest() {  LabelFile = labelFile.FileGUID };
                var response = await labelService.GetLabelInfoAsync(cfg);
				if (response.Success)
                {
                    return new NiceLabelInfo()
                    {
                        FileName = response.FileName,
                        Width = response.Width,
                        Height = response.Height,
                        Variables = response.Variables,
                        IsDataBound = response.IsDataBound,
                        Columns = response.Cols,
                        Rows = response.Rows,
                    };
                }
                else throw new Exception($"Could not get label variables for LabelFileName {label.FileName}, ProjectID {projectid}. LabelService response was: {response.ErrorMessage}, see the LabelService log file for additional details.");
            }
            else throw new Exception($"Label file for Label {id} was not found.");
        }


        public class LabelGroupingData
        {
            public string GroupingFields { get; set; } = "Barcode";
            public string DisplayFields { get; set; } = "TXT1,TXT2,Size";
        }
    }

    public class GlobalSettingsInfo
    {
        [PK]
        public int ID { get; set; }
        public int FactoryID { get; set; }
        public long PrintCount { get; set; }
        public string ResourcePath { get; set; }
        public string FscCode { get; set; }
        public string ProjectName { get; set; }  
    }

    public class SerialNumbersInfo
    {
        public int Product { get; set; }
        public string Barcode { get; set; }
        public long Serial { get; set; }
    }
}
