using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class BandFRFIDReportGeneratorService : IBandFRFIDReportGeneratorService
    {
        private readonly BandFRifConfig config;
        private readonly ILogSection log;
        private readonly IEncodedLabelRepository encodeLabelRepo;
        private readonly IOrderRepository orderRepo;
        private readonly IProjectRepository projectRepo;
        private readonly IDBConnectionManager connManager;
        private readonly ICatalogRepository catalogRepo;
        private readonly ITempFileService tempFileService;
        private readonly IOrderEmailService emailService;
        private readonly IEncryptionService encryp;
        private readonly ILocationRepository locationRepository;
        private IEnumerable<int> closedOrdersIds;

        public BandFRFIDReportGeneratorService(IAppConfig config,
            ILogService log,
            IEncodedLabelRepository encodeLabelRepo,
            IOrderRepository orderRepo,
            IProjectRepository projectRepo,
            IDBConnectionManager connManager,
            ICatalogRepository catalogRepo,
            ITempFileService tempFileService,
            IOrderEmailService emailService,
            IEncryptionService encryp,
            ILocationRepository locationRepository
            )
        {
            this.config = config.Bind<BandFRifConfig>("CustomSettings.BandF");
            this.log = log.GetSection("ReverseFlow");
            this.encodeLabelRepo = encodeLabelRepo;
            this.orderRepo = orderRepo;
            this.projectRepo = projectRepo;
            this.connManager = connManager;
            this.catalogRepo = catalogRepo;
            this.tempFileService = tempFileService;
            this.emailService = emailService;
            this.encryp = encryp;
            this.locationRepository = locationRepository;
        }

        public void SendReport(int companyID, DateTime from, DateTime to)
        {
            closedOrdersIds = new List<int>();

            var projects = projectRepo.GetByCompanyID(companyID, false).ToList();

            var filePattern = $"REPORT.{to.ToString("yyyyMMdd")}.csv";

            var filePath = tempFileService.GetTempFileName(filePattern, true);

            var data = GetReportData(companyID, from, to);

            Log($"B&F - data found '{data.Count()}'");

            if(data.Count() < 1)
            {
                return;
            }
            
            var ftpConfigured = projects.Last(f => f.FTPClients != null && f.EnableFTPFolder == true && JsonConvert.DeserializeObject<List<FtpClientConfig>>(encryp.DecryptString(f.FTPClients)).Any() );

            filePath = CreateFile(data, filePattern);

            PutFile(ftpConfigured, filePath);

            Log($"B&F file Created {filePath}");

            closedOrdersIds.ToList().ForEach(orderID => encodeLabelRepo.MarkAsProcessedInReport(orderID));

            tempFileService.RegisterForDelete(filePath, DateTime.Now.AddDays(1));

        }

        public void SendHistory()
        {

        }

        private IEnumerable<IEnumerable<string>> GetReportData(int companyID, DateTime from, DateTime to)
        {
            var orderStatus = new List<WebLink.Contracts.Models.OrderStatus> {
                WebLink.Contracts.Models.OrderStatus.Completed,
                WebLink.Contracts.Models.OrderStatus.Printing
            };

            List<List<string>> lines = new List<List<string>>();

            // B&F without header
            //lines.Add(new List<string> {
            //        "OrderNum",
            //        "Name",
            //        "Brand",
            //        "Barcode",
            //        "EPC",
            //        "TID",
            //        "RSSI",
            //        "Date"
            //         });// Header


            var currentProject = -1;
            List<ICatalog> catalogs = null;
            var tableAlias = "bd";

            List<string> baseDatafieldNames = GetCustomerFieldsNamesRequired(tableAlias);

            //var closedOrders = orderRepo.GetEncodedByProjectInStatusBetween(project.ID, orderStatus, to.AddMonths(-1), to).ToList();
            closedOrdersIds = encodeLabelRepo.GetOrderIDEncodeBetweenDates(companyID, from, to).ToList();

            using(DynamicDB dynamicDb = connManager.CreateDynamicDB())
            {
                closedOrdersIds.ToList().ForEach(orderID =>
                {
                    var co = orderRepo.GetByID(orderID, true);

                    

                    ILocation location = new Location() { Name = "Undefined" };

                    if(co.LocationID.HasValue)
                        location = locationRepository.GetByID(co.LocationID.Value);

                    if(currentProject != co.ProjectID)
                    {
                        currentProject = co.ProjectID;

                        var project = projectRepo.GetByID(co.ProjectID, true);

                        catalogs = catalogRepo.GetByProjectID(project.ID, true);
                        
                    }

                    var orderCatalog = catalogs.First(x => x.TableName.Contains(Catalog.ORDER_CATALOG));
                    var orderDetailsCatalog = catalogs.First(x => x.TableName.Contains(Catalog.ORDERDETAILS_CATALOG));
                    var variableDataCatalog = catalogs.First(x => x.TableName.Contains(Catalog.VARIABLEDATA_CATALOG));
                    var baseDataCatalog = catalogs.First(x => x.TableName.Contains(Catalog.BASEDATA_CATALOG));
                    var tableNameRel = GetTableNameRel(orderCatalog, orderDetailsCatalog, "Details");

                    // with one detail is enough
                    var orderBaseData = dynamicDb.SelectOne(orderCatalog.CatalogID,
                        $@"SELECT  {string.Join(',', baseDatafieldNames.ToArray())}  FROM #TABLE o 
                    INNER JOIN {tableNameRel} r ON o.ID = r.SourceID 
                    INNER JOIN {orderDetailsCatalog.TableName} d ON d.ID = r.TargetID 
                    INNER JOIN {variableDataCatalog.TableName} v ON v.ID = d.Product 
                    INNER JOIN {baseDataCatalog.TableName} {tableAlias} ON {tableAlias}.ID = v.IsBaseData 
                    WHERE o.ID = @orderDataID", co.OrderDataID);

                    IEnumerable<IEncodedLabel> found = new List<IEncodedLabel>();
                    long lastEpcID = 0;
                    do
                    {

                        found = encodeLabelRepo.GetForPendingReverseFlowSortedByID(co.ID, 1000, lastEpcID);
                        if(found.Any())
                            lastEpcID = found.Last().ID;

                        Log($"Current Order {co.ID} found : '{found.Count()}'");

                        found.ToList()
                        .ForEach(ean =>
                        {
                            var line = new List<string>();
                            line.Add(co.OrderNumber);
                            line.Add(location.Name);
                            line.Add(orderBaseData.GetValue("Brand", "-"));
                            line.Add(ean.Barcode);
                            line.Add(String.IsNullOrEmpty(ean.EPC) ? String.Empty : ean.EPC);
                            line.Add(String.IsNullOrEmpty(ean.TID) ? String.Empty : ean.TID);
                            line.Add(ean.RSSI.ToString());
                            line.Add(ean.Date.ToString("yyyy-MM-dd"));
                            lines.Add(line);
                        });
                    } while(found.Any());

                });

            }

            return lines;
        }

        private List<string> GetCustomerFieldsNamesRequired(string tableAlias)
        {
            //baseDatafieldNames.ForEach(f => f = $"{tableAlias}.{f}");// add tableName prefix
            return new List<string>() { $"{tableAlias}.Brand", $"{tableAlias}.Barcode" };
        }

        private string CreateFile(IEnumerable<IEnumerable<string>> data, string FileNamePattern)
        {
            var filePath = tempFileService.GetTempFileName(FileNamePattern, false);

            using(StreamWriter csvFile = new StreamWriter(filePath, true))
            {
                data.ToList().ForEach(row =>
                {

                    var editedRow = row.ToList();

                    // quote content
                    if(!string.IsNullOrEmpty(config.QuoteCellContent.ToString()))
                        for(int pos = 0; pos < editedRow.Count; pos++)
                            editedRow[pos] = Rfc4180Writer.QuoteValue(editedRow[pos]);

                    csvFile.WriteLine(string.Join(config.Separator, editedRow));
                });
            }

            return filePath;
        }

        private void PutFile(IProject project, string filePath)
        {
            // TODO: for email o FTP configuration flag
            //PutFileViaEmail(filePath);

            if(!string.IsNullOrEmpty(config.EmailAddress))
                PutFileViaEmail(filePath);

            PutFileViaFTP(project, filePath);

            

        }

        private void PutFileViaEmail(string filePath)
        {
            emailService.SendMessage(
                config.EmailAddress,
                $"B&F - INDET RFID daily report  {DateTime.Now.ToString("yyyy-MM-dd")}",
                "Please look at the attached files...",
                new List<string> { filePath }).Wait();
        }


        private void PutFileViaFTP(IProject project, string filePath)
        {

            var decrypted = encryp.DecryptString(project.FTPClients);
            var configuredClients = JsonConvert.DeserializeObject<List<FtpClientConfig>>(decrypted);
            var targetFileName = Path.GetFileName(filePath);
            var localServers = new List<string> { "127.0.0.1", "localhost", "smartdots-np-01" };

            foreach(var cli in configuredClients)
            {

                if(config.OnlyLocalFTPServer == true && localServers.Any(a => a.Contains(cli.Server.ToLower())) == false)
                {
                    Log($"B&F OnlyLocalFTPServer Enabled {cli.Server}");
                    continue; // skip if not is a local server
                }

                Log($"B&F Enviando Archivo al Cliente {cli.Server}");
                SendViaFTP(cli, filePath, targetFileName);

                Log($"B&F - Enviado a {cli.Server}");

            }

           

        }

        private void SendViaFTP(FtpClientConfig cli, string filePath, string targetFilename)
        {
            var targetDirectory = config.FTPTargetDirectory;

            var ftpClient = new RebexFtpLib.Client.FtpClient();

            byte[] fileContent = tempFileService.ReadTempFile(filePath);

            ftpClient.Initialize(0,
                "ftpConnectionName-bandf",
                cli.Server, cli.Port, cli.User, cli.Password,
                (RebexFtpLib.Client.FTPMode)((int)cli.Mode), cli.AllowInvalidCert, null, null,
                RebexFtpLib.Client.SFTPKeyAlgorithm.None);

            ftpClient.Connect();

            ftpClient.ChangeDirectory(targetDirectory); // hardcode directory

            ftpClient.SendFile(fileContent, targetFilename);

        }


        private string GetTableNameRel(ICatalog orderCatalog, ICatalog orderDetailsCatalog, string setName)
        {
            var numberOrdersCatalog = orderCatalog.CatalogID;
            var numberOrdersDetailsCatalog = orderDetailsCatalog.CatalogID;
            var definition = orderCatalog.Fields;
            var setField = definition.FirstOrDefault(f => f.Name == setName);

            return $"REL_{numberOrdersCatalog}_{numberOrdersDetailsCatalog}_{setField.FieldID.ToString()}";

        }

        private void MarkEansAsCompleted(OrderInfoDTO order)
        {
            Log("Start Update Bulk EncodeLabel rows in DB");
            encodeLabelRepo.MarkAsProcessedInReport(order.OrderID);
            Log("END Update Bulk EncodeLabel rows in DB");
        }

        private void Log(string msg)
        {

            if(config.Debug == false) return;
            log.LogMessage(msg);

        }

        private void Log(string msg, Exception ex)
        {
            if(config.Debug == false) return;
            log.LogException(msg, ex);
        }
    }

    internal class BandFRifConfig
    {
        public char QuoteCellContent;
        public char Separator;
        public string FTPTargetDirectory;
        public bool SendHistory;
        public DateTime HistoryStartDate;
        public string FileExtension;
        public string EmailAddress;
        public bool Debug;
        public bool OnlyLocalFTPServer;
    }
}
