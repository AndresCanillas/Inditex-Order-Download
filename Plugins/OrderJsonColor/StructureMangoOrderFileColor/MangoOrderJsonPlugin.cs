using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using StructureMangoOrderFile;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Mango.OrderPlugin
{
    [FriendlyName("Mango - Json Plugin"), Description("Selects the correct article code according to the type of label and the content of the composition.")]
    public class MangoOrderJsonPlugin : IDocumentImportPlugin
    {
        private IConnectionManager connMng;
        private IFileStoreManager storeManager;
        private ITempFileService tmpFileService;
        private IAppLog log;
        private IRemoteFileStore tempStore;
        private IFSFile tempFile;
        private IConnectionManager db;
        private Encoding encoding;
        private string FileName = string.Empty;
        private const string PROVIDER_CLEANER_REGEX = @"[^0-9a-zA-Z\-]+";
        private const string catalogDefaultCompo = "Details.Product.HasComposition";
        private const string catalogDefaultIntructions = "Details.Product.HasComposition.CareInstructions";
        public Font font;
        public Bitmap bmp;
        public Graphics g;



        public MangoOrderJsonPlugin(IConnectionManager connMng, IFileStoreManager storeManager, ITempFileService tmpFileService, IAppLog log)
        {
            this.connMng = connMng;
            this.storeManager = storeManager;
            this.tmpFileService = tmpFileService;
            this.log = this.log = log.GetSection("MangoOrderJsonPlugin");
            this.db = db;
            tempStore = storeManager.OpenStore("TempStore");

        }

        public MangoOrderJsonPlugin()
        {

        }
        private Encoding GetEncoding(DocumentImportConfiguration configuration)
        {
            if (configuration.Input.Encoding.ToLower() == "default")
            {
                return Encoding.Default;
            }
            else
            {
                try { return Encoding.GetEncoding(configuration.Input.Encoding); }
                catch { return Encoding.Default; }
            }
        }
        public void PrepareFile(DocumentImportConfiguration configuration, ImportedData data)
        {
            try
            {
                log.LogMessage($"PluginMango.OnPrepareFile, Start OnPrepareFile.");
                encoding = GetEncoding(configuration);
                var file = storeManager.GetFile(configuration.FileGUID);

                var fileContent = file.GetContentAsStream().ReadAllText(GetEncoding(configuration));

                var fname = Path.GetFileNameWithoutExtension(configuration.FileName);
                // required put bcjson extension in FileMask for mapping configuration
                tempFile = tempStore.CreateFile($"{fname}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.mngjson");


                log.LogMessage($"PluginMango.OnPrepareFile, loaded file with {fileContent.Length} characters.");
                if (ProcessingFile(file.FileName, configuration, fileContent))
                {
                    configuration.FileGUID = tempFile.FileGUID;
                    configuration.FileName = tempFile.FileName;// change filename to avoid use .json extension
                }
                log.LogMessage($"PluginMango.OnPrepareFile, Finish OnPrepareFile.");
            }
            catch (Exception ex)
            {
                log.LogMessage($"PluginMango.OnPrepareFile, Error: {ex.Message}.\r\n Tracer: {ex.StackTrace}. \r\n Inner:{ex.InnerException.Message} ");
            }
        }

        public string CreateHeader(PropertyInfo[] properties)
        {
            string header = "";
            if (!(properties is null))
            {


                for (int i = 0; i < properties.Length; i++)
                {
                    var Layer1 = string.Concat(properties[i].Name,

                    CreateHeader(properties[i].GetType().GetProperties()), ";");


                }
            }
            return header;

        }

        private bool ProcessingFile(string fileName, DocumentImportConfiguration configuration, string fileContent)
        {
            try
            {
                bool resp = false;
                encoding = GetEncoding(configuration);
                string strEncoding = configuration.Input.Encoding;
                var orderData = JsonConvert.DeserializeObject<MangoOrderData>(fileContent);

                GetProviderData(configuration.CompanyID, orderData.Supplier, orderData.LabelOrder.Id, configuration.ProjectID.ToString());

                string output = LoadData(orderData);

                var content = encoding.GetBytes(output);
                tempFile.SetContent(content);
                resp = true;

                return resp;
            }
            catch (Exception ex)
            {
                log.LogMessage($"PluginMango.OnPrepareFile, Error: {ex.Message}.");
                return default;
                throw;
            }


        }
        public string LoadData(MangoOrderData orderData)
        {
            bool hasHead = false;
            const char delimeter = ';';
            const int careinstructionsLimitQty = 15;
            const int mesurementsLimitQty = 5;
            const int tilesLimitQty = 9;
            const int fabricLimitQty = 8;
            //const int sizeRangesLimitQty = 1;
            const string totalCompositionFields = "TitleCode1;TitleName1;FabricName_Fabric1_Title1;FabriCode_Fabric1_Title1;FabricTypeSymbol_Fabric1_Title1;FabricPercent_Fabric1_Title1;FabricName_Fabric2_Title1;FabriCode_Fabric2_Title1;FabricTypeSymbol_Fabric2_Title1;FabricPercent_Fabric2_Title1;FabricName_Fabric3_Title1;FabriCode_Fabric3_Title1;FabricTypeSymbol_Fabric3_Title1;FabricPercent_Fabric3_Title1;FabricName_Fabric4_Title1;FabriCode_Fabric4_Title1;FabricTypeSymbol_Fabric4_Title1;FabricPercent_Fabric4_Title1;FabricName_Fabric5_Title1;FabriCode_Fabric5_Title1;FabricTypeSymbol_Fabric5_Title1;FabricPercent_Fabric5_Title1;FabricName_Fabric6_Title1;FabriCode_Fabric6_Title1;FabricTypeSymbol_Fabric6_Title1;FabricPercent_Fabric6_Title1;FabricName_Fabric7_Title1;FabriCode_Fabric7_Title1;FabricTypeSymbol_Fabric7_Title1;FabricPercent_Fabric7_Title1;FabricName_Fabric8_Title1;FabriCode_Fabric8_Title1;FabricTypeSymbol_Fabric8_Title1;FabricPercent_Fabric8_Title1;TitleCode2;TitleName2;FabricName_Fabric1_Title2;FabriCode_Fabric1_Title2;FabricTypeSymbol_Fabric1_Title2;FabricPercent_Fabric1_Title2;FabricName_Fabric2_Title2;FabriCode_Fabric2_Title2;FabricTypeSymbol_Fabric2_Title2;FabricPercent_Fabric2_Title2;FabricName_Fabric3_Title2;FabriCode_Fabric3_Title2;FabricTypeSymbol_Fabric3_Title2;FabricPercent_Fabric3_Title2;FabricName_Fabric4_Title2;FabriCode_Fabric4_Title2;FabricTypeSymbol_Fabric4_Title2;FabricPercent_Fabric4_Title2;FabricName_Fabric5_Title2;FabriCode_Fabric5_Title2;FabricTypeSymbol_Fabric5_Title2;FabricPercent_Fabric5_Title2;FabricName_Fabric6_Title2;FabriCode_Fabric6_Title2;FabricTypeSymbol_Fabric6_Title2;FabricPercent_Fabric6_Title2;FabricName_Fabric7_Title2;FabriCode_Fabric7_Title2;FabricTypeSymbol_Fabric7_Title2;FabricPercent_Fabric7_Title2;FabricName_Fabric8_Title2;FabriCode_Fabric8_Title2;FabricTypeSymbol_Fabric8_Title2;FabricPercent_Fabric8_Title2;TitleCode3;TitleName3;FabricName_Fabric1_Title3;FabriCode_Fabric1_Title3;FabricTypeSymbol_Fabric1_Title3;FabricPercent_Fabric1_Title3;FabricName_Fabric2_Title3;FabriCode_Fabric2_Title3;FabricTypeSymbol_Fabric2_Title3;FabricPercent_Fabric2_Title3;FabricName_Fabric3_Title3;FabriCode_Fabric3_Title3;FabricTypeSymbol_Fabric3_Title3;FabricPercent_Fabric3_Title3;FabricName_Fabric4_Title3;FabriCode_Fabric4_Title3;FabricTypeSymbol_Fabric4_Title3;FabricPercent_Fabric4_Title3;FabricName_Fabric5_Title3;FabriCode_Fabric5_Title3;FabricTypeSymbol_Fabric5_Title3;FabricPercent_Fabric5_Title3;FabricName_Fabric6_Title3;FabriCode_Fabric6_Title3;FabricTypeSymbol_Fabric6_Title3;FabricPercent_Fabric6_Title3;FabricName_Fabric7_Title3;FabriCode_Fabric7_Title3;FabricTypeSymbol_Fabric7_Title3;FabricPercent_Fabric7_Title3;FabricName_Fabric8_Title3;FabriCode_Fabric8_Title3;FabricTypeSymbol_Fabric8_Title3;FabricPercent_Fabric8_Title3;TitleCode4;TitleName4;FabricName_Fabric1_Title4;FabriCode_Fabric1_Title4;FabricTypeSymbol_Fabric1_Title4;FabricPercent_Fabric1_Title4;FabricName_Fabric2_Title4;FabriCode_Fabric2_Title4;FabricTypeSymbol_Fabric2_Title4;FabricPercent_Fabric2_Title4;FabricName_Fabric3_Title4;FabriCode_Fabric3_Title4;FabricTypeSymbol_Fabric3_Title4;FabricPercent_Fabric3_Title4;FabricName_Fabric4_Title4;FabriCode_Fabric4_Title4;FabricTypeSymbol_Fabric4_Title4;FabricPercent_Fabric4_Title4;FabricName_Fabric5_Title4;FabriCode_Fabric5_Title4;FabricTypeSymbol_Fabric5_Title4;FabricPercent_Fabric5_Title4;FabricName_Fabric6_Title4;FabriCode_Fabric6_Title4;FabricTypeSymbol_Fabric6_Title4;FabricPercent_Fabric6_Title4;FabricName_Fabric7_Title4;FabriCode_Fabric7_Title4;FabricTypeSymbol_Fabric7_Title4;FabricPercent_Fabric7_Title4;FabricName_Fabric8_Title4;FabriCode_Fabric8_Title4;FabricTypeSymbol_Fabric8_Title4;FabricPercent_Fabric8_Title4;TitleCode5;TitleName5;FabricName_Fabric1_Title5;FabriCode_Fabric1_Title5;FabricTypeSymbol_Fabric1_Title5;FabricPercent_Fabric1_Title5;FabricName_Fabric2_Title5;FabriCode_Fabric2_Title5;FabricTypeSymbol_Fabric2_Title5;FabricPercent_Fabric2_Title5;FabricName_Fabric3_Title5;FabriCode_Fabric3_Title5;FabricTypeSymbol_Fabric3_Title5;FabricPercent_Fabric3_Title5;FabricName_Fabric4_Title5;FabriCode_Fabric4_Title5;FabricTypeSymbol_Fabric4_Title5;FabricPercent_Fabric4_Title5;FabricName_Fabric5_Title5;FabriCode_Fabric5_Title5;FabricTypeSymbol_Fabric5_Title5;FabricPercent_Fabric5_Title5;FabricName_Fabric6_Title5;FabriCode_Fabric6_Title5;FabricTypeSymbol_Fabric6_Title5;FabricPercent_Fabric6_Title5;FabricName_Fabric7_Title5;FabriCode_Fabric7_Title5;FabricTypeSymbol_Fabric7_Title5;FabricPercent_Fabric7_Title5;FabricName_Fabric8_Title5;FabriCode_Fabric8_Title5;FabricTypeSymbol_Fabric8_Title5;FabricPercent_Fabric8_Title5;TitleCode6;TitleName6;FabricName_Fabric1_Title6;FabriCode_Fabric1_Title6;FabricTypeSymbol_Fabric1_Title6;FabricPercent_Fabric1_Title6;FabricName_Fabric2_Title6;FabriCode_Fabric2_Title6;FabricTypeSymbol_Fabric2_Title6;FabricPercent_Fabric2_Title6;FabricName_Fabric3_Title6;FabriCode_Fabric3_Title6;FabricTypeSymbol_Fabric3_Title6;FabricPercent_Fabric3_Title6;FabricName_Fabric4_Title6;FabriCode_Fabric4_Title6;FabricTypeSymbol_Fabric4_Title6;FabricPercent_Fabric4_Title6;FabricName_Fabric5_Title6;FabriCode_Fabric5_Title6;FabricTypeSymbol_Fabric5_Title6;FabricPercent_Fabric5_Title6;FabricName_Fabric6_Title6;FabriCode_Fabric6_Title6;FabricTypeSymbol_Fabric6_Title6;FabricPercent_Fabric6_Title6;FabricName_Fabric7_Title6;FabriCode_Fabric7_Title6;FabricTypeSymbol_Fabric7_Title6;FabricPercent_Fabric7_Title6;FabricName_Fabric8_Title6;FabriCode_Fabric8_Title6;FabricTypeSymbol_Fabric8_Title6;FabricPercent_Fabric8_Title6;TitleCode7;TitleName7;FabricName_Fabric1_Title7;FabriCode_Fabric1_Title7;FabricTypeSymbol_Fabric1_Title7;FabricPercent_Fabric1_Title7;FabricName_Fabric2_Title7;FabriCode_Fabric2_Title7;FabricTypeSymbol_Fabric2_Title7;FabricPercent_Fabric2_Title7;FabricName_Fabric3_Title7;FabriCode_Fabric3_Title7;FabricTypeSymbol_Fabric3_Title7;FabricPercent_Fabric3_Title7;FabricName_Fabric4_Title7;FabriCode_Fabric4_Title7;FabricTypeSymbol_Fabric4_Title7;FabricPercent_Fabric4_Title7;FabricName_Fabric5_Title7;FabriCode_Fabric5_Title7;FabricTypeSymbol_Fabric5_Title7;FabricPercent_Fabric5_Title7;FabricName_Fabric6_Title7;FabriCode_Fabric6_Title7;FabricTypeSymbol_Fabric6_Title7;FabricPercent_Fabric6_Title7;FabricName_Fabric7_Title7;FabriCode_Fabric7_Title7;FabricTypeSymbol_Fabric7_Title7;FabricPercent_Fabric7_Title7;FabricName_Fabric8_Title7;FabriCode_Fabric8_Title7;FabricTypeSymbol_Fabric8_Title7;FabricPercent_Fabric8_Title7;TitleCode8;TitleName8;FabricName_Fabric1_Title8;FabriCode_Fabric1_Title8;FabricTypeSymbol_Fabric1_Title8;FabricPercent_Fabric1_Title8;FabricName_Fabric2_Title8;FabriCode_Fabric2_Title8;FabricTypeSymbol_Fabric2_Title8;FabricPercent_Fabric2_Title8;FabricName_Fabric3_Title8;FabriCode_Fabric3_Title8;FabricTypeSymbol_Fabric3_Title8;FabricPercent_Fabric3_Title8;FabricName_Fabric4_Title8;FabriCode_Fabric4_Title8;FabricTypeSymbol_Fabric4_Title8;FabricPercent_Fabric4_Title8;FabricName_Fabric5_Title8;FabriCode_Fabric5_Title8;FabricTypeSymbol_Fabric5_Title8;FabricPercent_Fabric5_Title8;FabricName_Fabric6_Title8;FabriCode_Fabric6_Title8;FabricTypeSymbol_Fabric6_Title8;FabricPercent_Fabric6_Title8;FabricName_Fabric7_Title8;FabriCode_Fabric7_Title8;FabricTypeSymbol_Fabric7_Title8;FabricPercent_Fabric7_Title8;FabricName_Fabric8_Title8;FabriCode_Fabric8_Title8;FabricTypeSymbol_Fabric8_Title8;FabricPercent_Fabric8_Title8;TitleCode9;TitleName9;FabricName_Fabric1_Title9;FabriCode_Fabric1_Title9;FabricTypeSymbol_Fabric1_Title9;FabricPercent_Fabric1_Title9;FabricName_Fabric2_Title9;FabriCode_Fabric2_Title9;FabricTypeSymbol_Fabric2_Title9;FabricPercent_Fabric2_Title9;FabricName_Fabric3_Title9;FabriCode_Fabric3_Title9;FabricTypeSymbol_Fabric3_Title9;FabricPercent_Fabric3_Title9;FabricName_Fabric4_Title9;FabriCode_Fabric4_Title9;FabricTypeSymbol_Fabric4_Title9;FabricPercent_Fabric4_Title9;FabricName_Fabric5_Title9;FabriCode_Fabric5_Title9;FabricTypeSymbol_Fabric5_Title9;FabricPercent_Fabric5_Title9;FabricName_Fabric6_Title9;FabriCode_Fabric6_Title9;FabricTypeSymbol_Fabric6_Title9;FabricPercent_Fabric6_Title9;FabricName_Fabric7_Title9;FabriCode_Fabric7_Title9;FabricTypeSymbol_Fabric7_Title9;FabricPercent_Fabric7_Title9;FabricName_Fabric8_Title9;FabriCode_Fabric8_Title9;FabricTypeSymbol_Fabric8_Title9;FabricPercent_Fabric8_Title9;";
            const string compositionFields = "TitleCode;TitleName";
            const string fabricFields = "FabricName_Fabric_Title;FabriCode_Fabric_Title;FabricTypeSymbol_Fabric_Title;FabricPercent_Fabric_Title";
            const string careinstructionFields = "CareCode;CareSAPCode;CareGroup;";
            const string mesurementsFields = "Dim;Description;Measurement;";


            string dataLabel = "";
            string line = "";
            string head = "";
            var prepackData = new Prepackdata()
            {
                MangoPrePackCode = "",
                PrepackBarCode = "",
                PrepackColor = "",
                PrepackQty = "",
                PrepackTotalQty = ""

            };


            if (orderData.LabelData.Count() == 0 || orderData.LabelData == null)
                throw new InvalidOperationException($"Error: and the order ({orderData.LabelOrder.Id}) ");

            foreach (var label in orderData.LabelData)
            {

                if (orderData.ItemData.Count() == 0 || orderData.ItemData == null)
                    throw new InvalidOperationException($"Error: and the order ({orderData.LabelOrder.Id}) ");

                foreach (var item in orderData.ItemData)
                {

                    CreateLabelData(ref hasHead,
                        delimeter,
                        careinstructionsLimitQty,
                        tilesLimitQty,
                        fabricLimitQty,
                        totalCompositionFields,
                        compositionFields,
                        fabricFields,
                        careinstructionFields,
                        ref dataLabel,
                        out line,
                        out head,
                        orderData,
                        label,
                        item,
                        prepackData,
                        mesurementsFields,
                        mesurementsLimitQty);
                }
                CreateSetCompos(ref hasHead, delimeter, careinstructionsLimitQty, tilesLimitQty, fabricLimitQty, totalCompositionFields, compositionFields, fabricFields, careinstructionFields, ref dataLabel, ref line, ref head, orderData, label, prepackData, mesurementsFields, mesurementsLimitQty);
            }

            // CreateAdheDistLabel(ref hasHead, delimeter, careinstructionsLimitQty, tilesLimitQty, fabricLimitQty, totalCompositionFields, compositionFields, fabricFields, careinstructionFields, ref dataLabel, ref line, ref head, orderData, prepackData);


            return dataLabel;


        }

        private void GetProviderData(int companyID, Supplier supplierData, string orderNumber, string projectID)
        {

            // database names defined in DocumentService
            using (var db = connMng.OpenDB("PrintDB"))
            {

                // looking the ref inner print central
                if (!CheckProviderExist(db, Regex.Replace(supplierData.SupplierCode, PROVIDER_CLEANER_REGEX, ""), companyID))
                {
                    log.LogMessage($"PluginMango.OnPrepareFile, no se ha encontrado el proveedor {Regex.Replace(supplierData.SupplierCode, PROVIDER_CLEANER_REGEX, "")}");
                    // notify Customer
                    var companyInfo = GetCompanyInfo(db, companyID);

                    var title = $"The client reference ({supplierData.SupplierCode}) not found for Order Number {orderNumber}, CompanyID= {companyID} with ProjectID= {projectID}.";
                    var message = $"Error while procesing file {FileName}.\r\nThe order refers to a supplier code that is not registered in the system: {companyInfo.CompanyCode}";
                    var nkey = message.GetHashCode().ToString();
                    SendNotification(db, title, message, 1, 0, nkey, Newtonsoft.Json.JsonConvert.SerializeObject(supplierData));

                    // TODO: pending to send Email
                }


            }
        }
        private bool CheckProviderExist(IDBX db, string reference, int companyID)
        {
            var exist = true;

            var sql = @"
                SELECT ID, CompanyID, ProviderCompanyID, DefaultProductionLocation, ClientReference
                FROM CompanyProviders pv 
                WHERE CompanyID = @companyID
                AND ClientReference = @clientReference";

            var providerInfo = db.SelectOne<ProviderInfo>(sql, companyID, reference);

            if (providerInfo == null)
                exist = false;

            return exist;
        }

        private CompanyInfo GetCompanyInfo(IDBX db, int companyID)
        {
            var sql = @"
                SELECT ID, Name, CompanyCode
                FROM Companies c 
                WHERE ID = @companyID";

            var companyInfo = db.SelectOne<CompanyInfo>(sql, companyID);

            return companyInfo;
        }

        // TODO: require check if notification key already registered
        private void SendNotification(IDBX db, string title, string message, int locationId, int projectID, string nkey, string jsonData = null)
        {
            var NotificationTypeFTPFileWhatcher = 3;
            var data = jsonData != null ? jsonData : "{}";
            var msg = message;

            var sql = $@"INSERT INTO [dbo].[Notifications]
           ([CompanyID]
           ,[Type]
           ,[IntendedRole]
           ,[IntendedUser]
           ,[NKey]
           ,[Source]
           ,[Title]
           ,[Message]
           ,[Data]
           ,[AutoDismiss]
           ,[Count]
           ,[Action]
           ,[LocationID]
           ,[ProjectID]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[UpdatedBy]
           ,[UpdatedDate])
     VALUES
           (1
           ,{NotificationTypeFTPFileWhatcher}
           ,'{Service.Contracts.Authentication.Roles.IDTCostumerService}'
           ,''
           ,'MangoJson.Plugin.DocumentImport/{nkey}'
           ,'MangoJson.Plugin.DocumentImport'
           ,@title
           ,@msg
           ,@data
           ,0
           ,1
           ,null
           ,{locationId}
           ,{projectID}
           ,'SysAdmin'
           ,GETDATE()
           ,'System'
           ,GETDATE())";

            db.ExecuteNonQuery(sql, title, msg, data);


        }


        public class ProviderInfo
        {
            public int ID { get; set; }
            public int CompanyID { get; set; }
            public int ProviderCompanyID { get; set; }
            public int DefaultProductionLocation { get; set; }
            public string ClientReference { get; set; }
        }

        public class CompanyInfo
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string CompanyCode { get; set; }
        }

        private void CreateAdheDistLabel(ref bool hasHead, char delimeter, int careinstructionsLimitQty, int tilesLimitQty, int fabricLimitQty, string totalCompositionFields, string compositionFields, string fabricFields, string careinstructionFields, ref string dataLabel, ref string line, ref string head, MangoOrderData orderData, Prepackdata prepackdata, string mesurementsFields, int mesurementsLimitQty)
        {
            foreach (var item in orderData.ItemData)
            {

                CreateLabelData(ref hasHead,
                    delimeter,
                    careinstructionsLimitQty,
                    tilesLimitQty,
                    fabricLimitQty,
                    totalCompositionFields,
                    compositionFields,
                    fabricFields,
                    careinstructionFields,
                    ref dataLabel,
                    out line,
                    out head,
                    orderData,
                    new Labeldata
                    {
                        LabelID = GetPackLabel(item.SizePack.SizePackQty, orderData.Style.Line),
                        Variable = "",
                        Vendor = ""
                    },
                    item,
                    prepackdata,
                    mesurementsFields,
                    mesurementsLimitQty);
            }
            foreach (var prepack in orderData.PrepackData)
            {
                var item = crearItemDataEmpty();
                CreateLabelData(ref hasHead,
                   delimeter,
                   careinstructionsLimitQty,
                   tilesLimitQty,
                   fabricLimitQty,
                   totalCompositionFields,
                   compositionFields,
                   fabricFields,
                   careinstructionFields,
                   ref dataLabel,
                   out line,
                   out head,
                   orderData,
                   new Labeldata
                   {
                       LabelID = "B000MSZ",
                       Variable = "",
                       Vendor = ""
                   },
                   item,
                   prepack,
                   mesurementsFields,
                   mesurementsLimitQty);

            }




        }

        private Itemdata crearItemDataEmpty()
        {
            return
             new Itemdata()
             {
                 COLOR = "",
                 Currency = "",
                 EAN13 = "",
                 stocksegment = "",
                 itemQty = "",
                 MangoColorCode = "",
                 MangoSAPSizeCode = "",
                 MangoSizeCode = "",
                 Material = "",
                 PVP = "",
                 SizeName = "",
                 SizePack = new Sizepack()
                 {
                     SizeBarCode = "",
                     SizePackQty = "",
                     SizePackType = "",
                     TotalSizePackQty = "",
                 },
                 GarmentMeasurements = new List<Garmentmeasurement>()
                 {
                     new Garmentmeasurement(){
                     Description = "",
                     Dim = "",
                     Measurement = "" }
                 },
             };
        }

        private void CreateSetCompos(ref bool hasHead, char delimeter, int careinstructionsLimitQty, int tilesLimitQty, int fabricLimitQty, string totalCompositionFields, string compositionFields, string fabricFields, string careinstructionFields, ref string dataLabel, ref string line, ref string head, MangoOrderData orderData, Labeldata label, Prepackdata prepackdata, string mesurementsFields, int mesurementsLimitQty)
        {
            foreach (var item in orderData.ItemData)
            {
                var labelIds = GetSetComposLabel(label.LabelID, orderData.Style.Line);

                if (labelIds != null)

                    foreach (var labelId in labelIds.Split(',').ToList())
                    {
                        CreateLabelData(ref hasHead,
                        delimeter,
                        careinstructionsLimitQty,
                        tilesLimitQty,
                        fabricLimitQty,
                        totalCompositionFields,
                        compositionFields,
                        fabricFields,
                        careinstructionFields,
                        ref dataLabel,
                        out line,
                        out head,
                        orderData,
                        new Labeldata
                        {
                            LabelID = labelId,
                            Variable = label.Variable,
                            Vendor = label.Vendor
                        },
                        item,
                        prepackdata,
                        mesurementsFields,
                        mesurementsLimitQty);
                    }

            }
        }


        private string GetSetComposLabelWoman(string labelId)
        {
            string labelIdRetun = null;
            switch (labelId)
            {
                case "GI000PRO":
                    labelIdRetun = "CB000R82";
                    break;
                case "GI002NPO":
                    labelIdRetun = "GI001N82";
                    break;
                case "GI003NTU":
                    labelIdRetun = "GI001IN2";
                    break;
                case "GI003TCO":
                    labelIdRetun = "CB001T82";
                    break;

            }
            return labelIdRetun;
        }
        private string GetSetComposLabelMan(string labelId)
        {
            string labelIdRetun = null;
            switch (labelId)
            {
                case "GI000PRO":
                    labelIdRetun = "CB000R82";
                    break;
                case "GI003NTU":
                    labelIdRetun = "GI001IN2,GI004CI2";
                    break;
                case "GI000DPO":
                    labelIdRetun = "DB000D82,GI000CD3";
                    break;
                case "GI000DIM":
                    labelIdRetun = "GI000ID2,GI000CD3";
                    break;

            }
            return labelIdRetun;
        }
        private string GetSetComposLabelKids(string labelId)
        {

            string labelIdRetun = null;
            switch (labelId)
            {
                case "GI000PRO":
                    labelIdRetun = "CB000C82";
                    break;
                case "GI000DPO":
                    labelIdRetun = "CB000B82,GI000KD3";
                    break;


            }
            return labelIdRetun;
        }
        private string GetSetComposLabelBelts(string labelId)
        {
            string labelIdRetun = null;
            switch (labelId)
            {
                case "GI003NTU":
                    labelIdRetun = "GI001IN2,GI004CI2";
                    break;
            }
            return labelIdRetun;

        }

        private string GetSetComposLabelBaby(string labelId)
        {
            string labelIdRetun = null;
            switch (labelId)
            {
                case "GI000RIM":
                    labelIdRetun = "GI000IR2, GI000RI3";
                    break;
                case "GI000DIM":
                    labelIdRetun = "GI000ID2,GI000KD3";
                    break;


            }
            return labelIdRetun;

        }

        private string GetSetComposLabelHome(string labelId)
        {
            string labelIdRetun = null;
            switch (labelId)
            {
                case "GI000PRO":
                    labelIdRetun = "CB000C82";
                    break;
                case "GI000DPO":
                    labelIdRetun = "CB000B82";
                    break;


            }
            return labelIdRetun;

        }

        private string GetSetComposLabel(string labelId, string line)
        {
            string labelIdRetun = null;
            switch (line)
            {
                case "WOMAN":
                    GetSetComposLabelWoman(labelId);
                    break;
                case "MAN":
                    GetSetComposLabelMan(labelId);
                    break;
                case "KIDS":
                    GetSetComposLabelKids(labelId);
                    break;
                case "HOME":
                    GetSetComposLabelHome(labelId);
                    break;
                case "BABY":
                    GetSetComposLabelBaby(labelId);
                    break;
                case "BELTS":
                    GetSetComposLabelBelts(labelId);
                    break;
            };
            return labelIdRetun;
        }



        private string GetPackLabel(string sizePackQty, string line)
        {
            int packQty = int.Parse(sizePackQty);

            if (packQty < 1)
                throw new InvalidOperationException($"Error sizePackQty must be 1 or above 1 and it has a value of {sizePackQty}");


            string labelIdRetun = packQty > 1 ? "CB000KS1" : "CB000K41";
            switch (line)
            {
                case "WOMAN":
                    labelIdRetun = packQty > 1 ? "CB000151" : "CB000041";
                    break;
                case "MAN":
                    labelIdRetun = packQty > 1 ? "CB000151" : "CB000041";
                    break;


            }
            return labelIdRetun;

        }



        private void CreateLabelData(ref bool hasHead, char delimeter, int careinstructionsLimitQty, int tilesLimitQty, int fabricLimitQty, string totalCompositionFields, string compositionFields, string fabricFields, string careinstructionFields, ref string dataLabel, out string line, out string head, MangoOrderData orderData, Labeldata label, Itemdata item, Prepackdata prepackdata, string measurementsFields, int measurementsLimitQty)
        {
            line = "";
            head = "";


            LoadFields(ref line, ref head, label, delimeter, hasHead);
            LoadFields(ref line, ref head, orderData.LabelOrder, delimeter, hasHead);
            LoadFields(ref line, ref head, orderData.Supplier, delimeter, hasHead);
            LoadFields(ref line, ref head, orderData.Style, delimeter, hasHead);
            LoadFields(ref line, ref head, orderData.Destination, delimeter, hasHead);
            LoadFields(ref line, ref head, orderData.Origin, delimeter, hasHead);
            LoadFields(ref line, ref head, item, delimeter, hasHead);
            LoadFields(ref line, ref head, item.SizePack, delimeter, hasHead);
            LoadGarmentMeasurements(ref line, ref head, measurementsFields, item.GarmentMeasurements, delimeter,
                hasHead, measurementsLimitQty, orderData.LabelOrder.Id);
            //LoadFields(ref line, ref head, item.GarmentMeasurements, delimeter, hasHead);
            LoadComposition(ref line, ref head, orderData, totalCompositionFields, compositionFields, tilesLimitQty,
                fabricFields, fabricLimitQty, delimeter, hasHead);
            LoadCareInstructions(ref line, ref head, careinstructionFields, orderData, delimeter,
                hasHead, careinstructionsLimitQty);
            LoadFields(ref line, ref head, orderData.SizeRange, delimeter, hasHead);
            //LoadSizeRange(ref line, ref head, orderData, delimeter, hasHead);
            LoadPrepackData(ref line, ref head, prepackdata, delimeter, hasHead);
            if (!hasHead)
            {
                dataLabel = head + "\r\n";
                hasHead = true;
            }
            dataLabel += line + "\r\n";
        }


        private void LoadPrepackData(ref string line, ref string head, Prepackdata prepackdata, char delimeter, bool hasHead)
        {
            LoadFields(ref line, ref head, prepackdata, delimeter, hasHead);
        }

        private void LoadCareInstructions(
            ref string line,
            ref string head,
            string careinstructionFields,
            MangoOrderData orderData,
            char delimeter,
            bool hasHead,
            int careinstructionsLimitQty)
        {
            int delimeterQty = careinstructionFields.Count(x => delimeter == x);
            int careInstructionsQty = orderData.CareInstructions == null ? 0 : orderData.CareInstructions.Count();
            if (careInstructionsQty != 0 && orderData.CareInstructions != null)
            {
                if (careInstructionsQty > careinstructionsLimitQty)
                    throw new InvalidOperationException($"Error: CareInstructions can't be above 15 and the order ({orderData.LabelOrder.Id}) has {careInstructionsQty}");

                var indexCareInstruction = 1;
                foreach (var careInstruction in orderData.CareInstructions)
                {
                    LoadFields(ref line, ref head, careInstruction, delimeter, hasHead, indexCareInstruction++.ToString());

                }
                if (careInstructionsQty < careinstructionsLimitQty)
                {
                    line += CreateDelimeter(delimeterQty, delimeter, careinstructionsLimitQty - careInstructionsQty);
                    if (!hasHead) head += CreateFieldHead(careinstructionFields, careInstructionsQty + 1, careinstructionsLimitQty, delimeter);
                }

            }
            else
            {
                line += CreateDelimeter(delimeterQty, delimeter, careinstructionsLimitQty);
                if (!hasHead) head += CreateFieldHead(careinstructionFields, 1, careinstructionsLimitQty, delimeter);
            }
        }


        private void LoadGarmentMeasurements(
           ref string line,
           ref string head,
           string garmentMeasurementsFields,
           List<Garmentmeasurement> measurements,
           char delimeter,
           bool hasHead,
           int garmentMeasurementsLimitQty,
           string orderID)
        {
            int delimeterQty = garmentMeasurementsFields.Count(x => delimeter == x);
            int measurementsQty = measurements == null ? 0 : measurements.Count();
            if (measurementsQty != 0 && measurements != null)
            {
                if (measurementsQty > garmentMeasurementsLimitQty)
                    throw new InvalidOperationException($"Error: GarmentMeasurements can't be above 5 and the order ({orderID}) has {measurementsQty}");

                var indexmeasurements = 1;
                foreach (var careInstruction in measurements)
                {
                    LoadFields(ref line, ref head, careInstruction, delimeter, hasHead, indexmeasurements++.ToString());

                }
                if (measurementsQty < garmentMeasurementsLimitQty)
                {
                    line += CreateDelimeter(delimeterQty, delimeter, garmentMeasurementsLimitQty - measurementsQty);
                    if (!hasHead) head += CreateFieldHead(garmentMeasurementsFields, measurementsQty + 1, garmentMeasurementsLimitQty, delimeter);
                }

            }
            else
            {
                line += CreateDelimeter(delimeterQty, delimeter, garmentMeasurementsLimitQty);
                if (!hasHead) head += CreateFieldHead(garmentMeasurementsFields, 1, garmentMeasurementsLimitQty, delimeter);
            }
        }
        private string CreateFieldHead(string fields, int interactionStart, int interactionFinish, char delimeter)
        {
            string respose = "";
            for (int interactionCounter = interactionStart; interactionFinish >= interactionCounter; interactionCounter++)
            {
                respose += fields.Replace(delimeter.ToString(), interactionCounter.ToString() + delimeter);
            }
            return respose;
        }
        private string CreateFieldComposition(string titles, string fabrics, int interactionTitleStart, int interactionTotleFinish, int interactionFabricStart, int interactionFabricFinish, char delimeter)
        {
            var titleFields = titles.Split(delimeter).ToList();
            string respose = "";
            for (int titleCounter = interactionTitleStart; interactionTotleFinish >= titleCounter; titleCounter++)
            {
                titleFields.ForEach(field => respose += field + titleCounter.ToString() + delimeter);
                respose += CreateFieldFabric(fabrics, titleCounter.ToString(), interactionFabricStart, interactionFabricFinish, delimeter);
            }
            return respose;
        }

        private string CreateFieldFabric(string fabrics, string titleCounter, int interactionFabricStart, int interactionFabricFinish, char delimeter)
        {

            var fabricFields = fabrics.Split(delimeter).ToList();
            string respose = "";

            for (int fabricCounter = interactionFabricStart; interactionFabricFinish >= fabricCounter; fabricCounter++)
            {
                fabricFields.ForEach(field => respose += field.Replace("Fabric_", "Fabric" + fabricCounter.ToString() + "_") + titleCounter + delimeter);

            }


            return respose;
        }

        private string CreateDelimeter(int delimiterQty, char delimeter, int interactionQty)
        {
            string respose = "";
            for (int interactionCounter = 1; interactionQty >= interactionCounter; interactionCounter++)
            {
                respose += CreateDelimeter(delimiterQty, delimeter);
            }
            return respose;
        }
        private string CreateDelimeter(int delimiterQty, char delimeter)
        {
            string respose = "";
            for (int delimeterCounter = 1; delimiterQty >= delimeterCounter; delimeterCounter++)
            {
                respose += delimeter;
            }
            return respose;
        }




        private void LoadComposition(
            ref string line,
            ref string head,
            MangoOrderData orderData,
            string totalcompositionFileds,
            string compositionFields,
            int titleLimitQty,
            string fabricFields,
            int fabricLimitQty,
            char delimeter,
            bool hasHead
            )
        {

            int delimeterCompositionQty = compositionFields.Count(x => x == delimeter);
            int compositionsQty = orderData.Composition == null ? 0 : orderData.Composition.Count();
            if (compositionsQty != 0 && orderData.Composition != null)
            {
                if (compositionsQty > titleLimitQty)
                    throw new InvalidOperationException($"Error: Comoposition can't be above {titleLimitQty} " +
                        $"and the order ({orderData.LabelOrder.Id}) has {compositionsQty}");


                var indexComposition = 1;
                foreach (var composition in orderData.Composition)
                {
                    var suffixTitle = indexComposition++.ToString();
                    LoadFields(ref line, ref head, composition, delimeter, hasHead, suffixTitle);


                    int delimeterFabricQty = fabricFields.Count(x => delimeter == x);
                    int fabricQty = composition.Fabric.Count();

                    if (fabricQty > fabricLimitQty)
                        throw new InvalidOperationException($"Error: fabric can't be above {fabricLimitQty} " +
                            $"and the order ({orderData.LabelOrder.Id}) has {fabricQty}");

                    var indexFabric = 1;
                    foreach (var fabric in composition.Fabric)
                    {
                        var suffixFabric = string.Concat("_Fabric", indexFabric++, "_Title", suffixTitle);
                        LoadFields(ref line, ref head, fabric, delimeter, hasHead, suffixFabric);
                    }
                    if (fabricQty < fabricLimitQty)
                    {
                        line += CreateDelimeter(delimeterFabricQty + 1, delimeter, fabricLimitQty - fabricQty);
                        if (!hasHead) head += CreateFieldFabric(fabricFields, suffixTitle, fabricQty + 1, fabricLimitQty, delimeter);
                    }

                }
                if (compositionsQty < titleLimitQty)
                {

                    line += CreateDelimeter(delimeterCompositionQty, delimeter, (titleLimitQty * ((4 * fabricLimitQty) + 2)) - (compositionsQty * ((4 * fabricLimitQty) + 2)));
                    if (!hasHead) head += CreateFieldComposition(compositionFields, fabricFields, compositionsQty + 1, titleLimitQty, 1, fabricLimitQty, delimeter);
                }

            }
            else
            {
                line += CreateDelimeter(delimeterCompositionQty, delimeter, titleLimitQty * ((4 * fabricLimitQty) + 2));
                if (!hasHead) head += CreateFieldComposition(compositionFields, fabricFields, 1, titleLimitQty, 1, fabricLimitQty, delimeter);
            }


        }

        private void LoadFields(
            ref string line,
            ref string head,
            object orderData,
            char delimeter,
            bool hasHead,
            string suffix = null)
        {
            if (orderData == null)
                throw new InvalidOperationException($"Error: The objtec {orderData.GetType().Name} can't be empty");

            string[] notIncludeFields = { "GarmentMeasurements", "SizePack", "Fabric" };
            var properties = orderData.GetType().GetProperties().ToList();


            foreach (var property in properties)
            {
                if (property != null)
                {
                    var fieldName = property.Name == "Id" ? "LabelOrderId" : property.Name;
                    if (!notIncludeFields.Contains(fieldName))
                    {
                        if (!hasHead) head += string.Concat(fieldName, (suffix == null ? "" : suffix), delimeter);
                        line += string.Concat((property.GetValue(orderData) ?? "").ToString(), delimeter);
                    }

                }
            }

        }





        public void Dispose()
        {
            if (bmp != null)
                bmp.Dispose();

            if (g != null)
                g.Dispose();
        }
        public void Execute(DocumentImportConfiguration configuration, ImportedData data)
        {
            //ConcatCompo(configuration, data);

        }

        private void ConcatCompo(DocumentImportConfiguration configuration, ImportedData data)
        {
            log.LogMessage("Executing Mango Json Plugin.");
            try
            {
                int rowIndex = 0;
                font = new Font("Arial", 4f);
                bmp = new Bitmap(100, 100);
                g = Graphics.FromImage(bmp);


                var file = storeManager.GetFile(configuration.FileGUID);
                while (rowIndex < data.Rows.Count)
                {
                    var label = data.GetValue("Details.ArticleCode").ToString();
                    // var extension = label.Substring(label.Length - 3, 3).ToUpper();
                    data.CurrentRow = rowIndex;
                    log.LogMessage($"Processing {label} label");

                    switch (label)
                    {
                        case "CPO":
                        case "GI000PRO":
                        case "PROS":
                        case "PVADHPP0":
                        case "GI000DPO":
                        case "SCO":
                            {
                                var materialsFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 4.1338f, 1.1417f),
                                new RibbonFace(font, 4.1338f, 1.1417f)
                            };

                                var ciFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 3.5826f, 1.1417f),
                                new RibbonFace(font, 4.2126f, 1.1417f)
                            };

                                ProcessRow(data, materialsFaces, ciFaces, label);
                            }
                            break;
                        case "GI002NPO": // update text container sizes only for this label
                            {
                                var materialsFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 4.0452f, 1.05630f),
                                new RibbonFace(font, 4.0452f, 1.05630f),
                            };

                                var ciFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 3.54291f, 1.05630f),
                                new RibbonFace(font, 4.0452f, 1.05630f)
                            };

                                ProcessRow(data, materialsFaces, ciFaces, label);
                            }
                            break;


                        case "GI003NTU":
                        case "TIM":
                        case "GI000RIM":
                        case "GI000DIM":
                        case "GI003TCO":
                            {
                                var materialsFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 3.0291f, 0.9322f),
                                new RibbonFace(font, 3.0074f, 0.9590f)
                            };

                                var ciFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 2.4976f, 0.9279f),
                                new RibbonFace(font, 3.0775f, 0.8826f)
                            };

                                ProcessRow(data, materialsFaces, ciFaces, label);
                            }
                            break;

                        case "GI000HPO":
                        case "GI000HRO":
                            {
                                var materialsFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 2.7290f, 0.9727f),
                                new RibbonFace(font, 2.7290f, 0.9727f)
                            };

                                var ciFaces = new List<RibbonFace>() {
                                new RibbonFace(font, 2.1565f, 0.9727f),
                                new RibbonFace(font, 2.7290f, 0.9727f)
                            };

                                ProcessRow(data, materialsFaces, ciFaces, label);
                            }
                            break;
                    }
                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                throw;
            }
        }

        private void ProcessRow(ImportedData data, List<RibbonFace> materialsFaces, List<RibbonFace> ciFaces, string baseArticleCode)
        {
            //var materialsAll = data.GetValue("Details.Product.IsCPOTIMNTU.materials_all").ToString();//ConcatMaterialsAll(data);

            var materialsAll = ConcatMaterialsAll(data);
            var materialsFront = data.GetValue(catalogDefaultCompo + ".Split1").ToString();
            var materialsBack = data.GetValue(catalogDefaultCompo + ".Split2").ToString();
            var lineCount = materialsAll.CharCount('\r');
            log.LogMessage($"Row {data.CurrentRow}: {materialsAll.Length} characters, split in {lineCount} lines.");
            if (ContentFits(materialsFaces[0], materialsAll))
            {
                log.LogMessage($"ArticleCode for this row will be set to {baseArticleCode}1.");
                data.SetValue("Details.ArticleCode", $"{baseArticleCode}1");
                data.SetValue(catalogDefaultCompo + ".Error", "0");
                data.SetValue(catalogDefaultIntructions + ".Error", "0");
            }
            else if (ContentFits(materialsFaces[0], materialsFront) && ContentFits(materialsFaces[0], materialsBack))
            {
                log.LogMessage($"ArticleCode for this row will be set to {baseArticleCode}2.");
                data.SetValue("Details.ArticleCode", $"{baseArticleCode}2");
                data.SetValue(catalogDefaultCompo + ".Split1", materialsFront.Replace("¬", ""));
                data.SetValue(catalogDefaultCompo + ".Split2", materialsBack.Replace("¬", ""));
                data.SetValue(catalogDefaultCompo + ".Error", "0");
                SplitCareInstructions(data, ciFaces);
            }
            else
            {
                log.LogMessage($"ArticleCode for this row will be set to {baseArticleCode}3.");
                data.SetValue("Details.ArticleCode", $"{baseArticleCode}3");
                var fitFlag1 = MaterialsFitContent(materialsFaces, materialsFront);
                data.SetValue(catalogDefaultCompo + ".Split1", materialsFaces[0].FittingText);
                data.SetValue(catalogDefaultCompo + ".Split3", materialsFaces[1].FittingText);
                ClearFaces(materialsFaces);
                var fitFlag2 = MaterialsFitContent(materialsFaces, materialsBack);
                data.SetValue(catalogDefaultCompo + ".Split2", materialsFaces[0].FittingText);
                data.SetValue(catalogDefaultCompo + ".Split4", materialsFaces[1].FittingText);
                data.SetValue(catalogDefaultCompo + ".Error", fitFlag1 && fitFlag2 ? "0" : "1");
                SplitCareInstructions(data, ciFaces);
            }
        }


        private void SplitCareInstructions(ImportedData data, List<RibbonFace> ciFaces)
        {
            var ci = data.GetValue(catalogDefaultCompo + ".Split1").ToString();
            if (ContentFits(ciFaces[0], ci))
            {
                data.SetValue(catalogDefaultIntructions + ".Error", "0");
            }
            else
            {
                bool fitFlag = CareInstructionsFitContent(ciFaces, ci);
                data.SetValue(catalogDefaultIntructions + ".Split1", ciFaces[0].FittingText);
                data.SetValue(catalogDefaultIntructions + ".Split2", ciFaces[1].FittingText);
                data.SetValue(catalogDefaultIntructions + ".Error", fitFlag ? "0" : "1");
            }
        }


        private string ConcatMaterialsAll(ImportedData data)
        {
            StringBuilder sb = new StringBuilder(5000);
            foreach (var col in data.Cols)
            {
                if (String.IsNullOrWhiteSpace(col.InputColumn)) continue;
                //if ((col.InputColumn.Contains("TitleCode") && !data.GetValue(col.InputColumn).ToString().Contains("COMPOSITION")) || col.InputColumn.StartsWith("FabricPercent") || col.InputColumn.StartsWith("FabricCode"))
                if ((col.InputColumn.Contains("TitleCode") || col.InputColumn.StartsWith("FabricPercent") || col.InputColumn.StartsWith("FabricCode")))
                    sb.Append(data.GetValue(col.InputColumn));
            }
            return sb.ToString();
        }


        public bool MaterialsFitContent(List<RibbonFace> faces, string textToFit)
        {
            var faceIdx = 0;
            var contentIdx = 0;
            var elementsToFit = textToFit.Split('¬');
            while (faceIdx < faces.Count)
            {
                var face = faces[faceIdx];
                var sb = new StringBuilder(3000);
                while (contentIdx < elementsToFit.Length)
                {
                    sb.Append(elementsToFit[contentIdx]);
                    var testContent = sb.ToString();
                    if (ContentFits(face, testContent))
                    {
                        face.FittingText = testContent;
                        contentIdx++;
                    }
                    else
                    {
                        break;
                    }
                }
                faceIdx++;
            }
            return contentIdx == elementsToFit.Length;  // Returns true if the text did fit in all available faces, false otherwise.
                                                        // Note: Even if false is returned, faces will be initialized with the text that fits in each available face.
        }


        // Splits content into the different faces passed as argument, main difference with respect to MaterialsFitContent is the way content is split.
        // In this case content is split by words
        private bool CareInstructionsFitContent(List<RibbonFace> faces, string text)
        {
            var blankChars = new char[] { ' ', '\r', '\n', '\t' };
            var faceIdx = 0;
            var contentIdx = 0;
            while (faceIdx < faces.Count)
            {
                var face = faces[faceIdx];
                var sb = new StringBuilder(3000);
                while (contentIdx < text.Length)
                {
                    var blankIdx = text.IndexOfAny(blankChars, contentIdx);
                    if (blankIdx < 0)
                        sb.Append(text.Substring(contentIdx));
                    else
                        sb.Append(text.Substring(contentIdx, blankIdx - contentIdx + 1));
                    var testContent = sb.ToString();
                    if (ContentFits(face, testContent))
                    {
                        face.FittingText = testContent;
                        contentIdx = blankIdx + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                faceIdx++;
            }
            return contentIdx == text.Length;  // Returns true if the text did fit in all available faces, false otherwise.
                                               // Note: Even if false is returned, faces will be initialized with the text that fits in each available face.
        }


        public bool ContentFits(RibbonFace face, string text)
        {
            var size = g.MeasureString(text, face.Font, (int)face.WidthInPixels, new StringFormat(StringFormatFlags.MeasureTrailingSpaces));
            var result = size.Height < face.HeightInPixels;
            return result;
        }

        public void ClearFaces(List<RibbonFace> ribbons)
        {
            ribbons.ForEach(r => r.FittingText = string.Empty);
        }


        public class RibbonFace
        {
            public Font Font;
            public float WidthInInches;
            public float HeightInInches;
            public string FittingText;

            public float WidthInPixels { get => WidthInInches * 96; }
            public float HeightInPixels { get => HeightInInches * 96; }

            public RibbonFace(Font font, float widthInInches, float heightInInches)
            {
                Font = font;
                WidthInInches = widthInInches;
                HeightInInches = heightInInches;
            }
        }
    }
}
