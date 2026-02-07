using Inidtex.ZaraExterlLables;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using Services.Core;
using StructureInditexOrderFile;
using System;
using System.IO;
using System.Text;

namespace Inditex.OrderPlugin
{
    [FriendlyName("Inditex - Zara.Hangtag.Kids.Plugin"), Description("InditexZaraHangtagKidsPlugin.Json.DocumentService")]
    public class InditexZaraHangtagKidsPlugin : IDocumentImportPlugin
    {
        private readonly IConnectionManager connMng;
        private readonly IFileStoreManager storeManager;
        private readonly ILogService log;
        private readonly IProviderVerifier providerVerifier;
        private IRemoteFileStore tempStore;
        private IFSFile tempFile;
        private Encoding encoding;

        public InditexZaraHangtagKidsPlugin(
            IConnectionManager connMng,
            IFileStoreManager storeManager,
            ILogService log)
            : this(connMng, storeManager, log, new ProviderVerifier(new ProviderRepository(), new NotificationWriter()))
        {
        }

        public InditexZaraHangtagKidsPlugin(
            IConnectionManager connMng,
            IFileStoreManager storeManager,
            ILogService log,
            IProviderVerifier providerVerifier)
        {
            this.connMng = connMng;
            this.storeManager = storeManager;
            this.log = log;
            this.providerVerifier = providerVerifier;
            tempStore = storeManager.OpenStore("TempStore");
        }

        public InditexZaraHangtagKidsPlugin()
        {

        }

        public void PrepareFile(DocumentImportConfiguration configuration, ImportedData data)
        {
            try
            {
                log.LogMessage("Inditex.ZaraHangtag.Kids.Plugin.OnPrepareFile, Start OnPrepareFile.");
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


                var pluginType = fname.Split('_')[1];

                tempFile = tempStore.CreateFile($"{fname}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.{pluginType}");


                log.LogMessage($"Inditex.ZaraHangtag.Kids.Plugin.OnPrepareFile, loaded file with {fileContent.Length} characters.");
                if(ProcessingFile(encoding, configuration, fileContent, pluginType))
                {
                    configuration.FileGUID = tempFile.FileGUID;
                    configuration.FileName = tempFile.FileName;// change filename to avoid use .json extension
                }
                log.LogMessage("Inditex.ZaraHangtag.Kids.Plugin.OnPrepareFile, Finish OnPrepareFile.");
            }
            catch(Exception ex)
            {
                log.LogMessage($"Inditex.ZaraHangtag.Kids.Plugin.OnPrepareFile, Error: {ex.Message}.\r\n Tracer: {ex.StackTrace}. " +
                    $"\r\n Inner:{ex.InnerException?.Message} ");
            }
        }



        private bool ProcessingFile(Encoding encoding, DocumentImportConfiguration configuration, string fileContent, string pluginType)
        {
            try
            {
                bool resp = false;
                var orderData = JsonConvert.DeserializeObject<InditexOrderData>(fileContent);
                using(var db = connMng.OpenDB("PrintDB"))
                {
                    providerVerifier.ValidateProviderData(
                        configuration.CompanyID,
                        orderData.supplier,
                        orderData.POInformation.productionOrderNumber,
                        configuration.ProjectID.ToString(),
                        db,
                        log,
                        configuration.FileName);
                }
                string output = JsonToTextConverter.LoadData(orderData, log, connMng, configuration.ProjectID, pluginType);
                var content = encoding.GetBytes(output);
                tempFile.SetContent(content);
                resp = true;

                return resp;
            }
            catch(Exception ex)
            {
                log.LogException($"Inditex.ZaraHangtag.Kids.Plugin.OnPrepareFile, Error: {ex.Message}.", ex);
                return false;
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
