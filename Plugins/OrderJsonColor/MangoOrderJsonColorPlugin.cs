using JsonColor;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using Services.Core;
using StructureMangoOrderFileColor;
using System;
using System.IO;
using System.Text;

namespace Mango.OrderPlugin
{
    [FriendlyName("Mango - Json Plugin Color"), Description("Mango.Json.DocumentService")]
    public class MangoOrderJsonPluginColor : IDocumentImportPlugin
    {
        private IConnectionManager connMng;
        private IFileStoreManager storeManager;
        private ILogService log;
        private IRemoteFileStore tempStore;
        private IFSFile tempFile;
        private Encoding encoding;

        public MangoOrderJsonPluginColor(
            IConnectionManager connMng,
            IFileStoreManager storeManager,
            ILogService log)
        {
            this.connMng = connMng;
            this.storeManager = storeManager;
            this.log = log;
            tempStore = storeManager.OpenStore("TempStore");
        }

        public MangoOrderJsonPluginColor()
        {

        }

        public void PrepareFile(DocumentImportConfiguration configuration, ImportedData data)
        {
            try
            {
                log.LogMessage($"PluginMango.OnPrepareFile, Start OnPrepareFile.");
                try
                {
                    if(configuration.Input.Encoding.ToLower() == "default") encoding = Encoding.Default;
                    encoding = Encoding.GetEncoding(configuration.Input.Encoding);
                }
                catch
                {
                    encoding = Encoding.Default;
                }
                var file = storeManager.GetFile(configuration.FileGUID);

                var fileContent = file.GetContentAsStream().ReadAllText(encoding);

                var fname = Path.GetFileNameWithoutExtension(configuration.FileName);

                tempFile = tempStore.CreateFile($"{fname}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.mngjsoncolor");


                log.LogMessage($"PluginMangoJsonColor.OnPrepareFile, loaded file with {fileContent.Length} characters.");
                if(ProcessingFile(encoding, configuration, fileContent))
                {
                    configuration.FileGUID = tempFile.FileGUID;
                    configuration.FileName = tempFile.FileName;// change filename to avoid use .json extension
                }
                log.LogMessage($"PluginMangoJsonColor.OnPrepareFile, Finish OnPrepareFile.");
            }
            catch(Exception ex)
            {
                log.LogMessage($"PluginMangoJsonColor.OnPrepareFile, Error: {ex.Message}.\r\n Tracer: {ex.StackTrace}. " +
                    $"\r\n Inner:{ex.InnerException.Message} ");
            }
        }



        private bool ProcessingFile(Encoding encoding, DocumentImportConfiguration configuration, string fileContent)
        {
            try
            {
                bool resp = false;
                string strEncoding = configuration.Input.Encoding;
                var orderData = JsonConvert.DeserializeObject<MangoOrderData>(fileContent);
                using(var db = connMng.OpenDB("PrintDB"))
                {
                    ProviderVerifier.ValidateProviderData(
                        configuration.CompanyID, orderData.Supplier,
                        orderData.LabelOrder.LabelOrderId, configuration.ProjectID.ToString(),
                        db, log, configuration.FileName);
                }
                ClientDefinitions.isOnlyColor = LoadJsonConfig.GetIsOnlyColor();
                string output = JsonToTextConverter.LoadData(orderData, log, connMng, configuration.ProjectID);

                var content = encoding.GetBytes(output);
                tempFile.SetContent(content);
                resp = true;

                return resp;
            }
            catch(Exception ex)
            {
                log.LogException($"PluginMango.OnPrepareFile, Error: {ex.Message}.", ex);
                return false;
                throw;
            }
        }

        public void Dispose()
        {

        }

        public void Execute(DocumentImportConfiguration configuration, ImportedData data)
        {
            //throw new NotImplementedException();
        }
    }
}
