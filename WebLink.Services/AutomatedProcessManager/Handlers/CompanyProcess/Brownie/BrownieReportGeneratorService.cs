using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;

namespace WebLink.Services
{
    public class BrownieReportGeneratorService : IBrownieReportGeneratorService
    {
        private readonly IProjectRepository _projectRepo;
        private readonly ITempFileService _tempFileService;
        private readonly IAppConfig _config;
        private readonly IFactory _factory;
        private readonly ICatalogRepository _catalogRepo;
        private readonly IDBConnectionManager _connManager;
        private readonly IEncryptionService _encryp;
        private readonly ILogService _log;
        private readonly IOrderUtilService _orderUtilService;

        public BrownieReportGeneratorService(
            IProjectRepository projectRepo,
            ITempFileService tempFileService,
            IAppConfig config, IFactory factory,
            ICatalogRepository catalogRepo,
            IDBConnectionManager connManager,
            IEncryptionService encryp,
            ILogService log,
            IOrderUtilService orderUtilService)
        {
            _projectRepo = projectRepo;
            _tempFileService = tempFileService;
            _config = config;
            _factory = factory;
            _catalogRepo = catalogRepo;
            _connManager = connManager;
            _encryp = encryp;
            _log = log;
            _orderUtilService = orderUtilService;
        }

        public void SendReport(int companyID, DateTime from, DateTime to)
        {
            var projects = _projectRepo.GetByCompanyID(companyID, false).ToList();
            var fileExtension = _config.GetValue<string>("CustomSettings.Brownie.FileExtension", "csv");
            var filePattern = $"BrownieCompositionReportTo-{to.ToString("yyyyMMdd")}.{fileExtension}";
            var filePath = _tempFileService.GetTempFileName(filePattern, true);

            GetReportData(filePath, companyID);

        }

        private void GetReportData(string filePath, int companyID)
        {

            var reportCompositions = GetReport(filePath);

            if(reportCompositions.Count() == 0)
            {
                _log.LogMessage("There are no compositions to report.");
                return;
            }

            var csvContent = GenerateCSV(reportCompositions, filePath);
            var projects = _projectRepo.GetByCompanyID(companyID, false).ToList();
            var ftpConfigured = projects.Last(f => f.FTPClients != null && f.EnableFTPFolder == true && JsonConvert.DeserializeObject<List<FtpClientConfig>>(_encryp.DecryptString(f.FTPClients)).Any());

            PutFile(ftpConfigured, filePath);
        }

        private void PutFile(IProject project, string filePath)
        {
            PutFileViaFTP(project, filePath);
        }

        private void PutFileViaFTP(IProject project, string filePath)
        {

            var decrypted = _encryp.DecryptString(project.FTPClients);
            var configuredClients = JsonConvert.DeserializeObject<List<FtpClientConfig>>(decrypted);
            var targetFileName = Path.GetFileName(filePath);

            foreach(var cli in configuredClients)
            {
                if(cli.WorkDirectory == "/Entradas")
                {
                    SendViaFTP(cli, filePath, targetFileName);
                    _tempFileService.DeleteTempFile(filePath); //Delete Temporal File
                }
            }
        }

        private void SendViaFTP(FtpClientConfig cli, string filePath, string targetFilename)
        {
            var ftpClient = new RebexFtpLib.Client.FtpClient();

            byte[] fileContent = _tempFileService.ReadTempFile(filePath);

            ftpClient.Initialize(0,
                "ftpConnectionName-Brownie",
                cli.Server, cli.Port, cli.User, cli.Password,
                (RebexFtpLib.Client.FTPMode)((int)cli.Mode), cli.AllowInvalidCert, null, null,
                RebexFtpLib.Client.SFTPKeyAlgorithm.None);

            ftpClient.Connect();

            ftpClient.ChangeDirectory(cli.WorkDirectory);

            ftpClient.SendFile(fileContent, targetFilename);

            _log.LogMessage($"Uploaded Report Compositions Brownie File: {targetFilename} To: {cli.WorkDirectory}");
        }



        private List<ReportRow> GetReport(string filePath)
        {
            var currentProject = -1;
            var tableNameRel = string.Empty;
            ICatalog orderCatalog = null;
            ICatalog detailCatalog = null;
            ICatalog variableDataCatalog = null;
            ICatalog compositionLabelCatalog = null;
            ICatalog baseDataCatalog = null;
            ICatalog careInstructionsCatalog = null;
            ICatalog sectionsCatalog = null;
            ICatalog fiberCatalog = null;
            ICollection<string> percentFibers = null;
            ICollection<string> codesFibers = null;
            ICatalog madeInCatalog = null;
            var brandID = _config.GetValue<int>("CustomSettings.Brownie.BrandID", 0);
            var projects = _projectRepo.GetByBrandID(brandID, false);
            var baseDatafieldNames = GetFielsNamesRequiredReport();

            var reportCompositions = new List<ReportRow>();

            foreach(var project in projects)
            {
                var closedOrders = GetOrderDataIDs(project.ID);

                closedOrders.ToList().ForEach(order =>
                {
                    if(currentProject != order.ProjectID)
                    {
                        currentProject = order.ProjectID;
                        GetCatalogs(project, out orderCatalog, out detailCatalog, out variableDataCatalog, out compositionLabelCatalog, out baseDataCatalog, out tableNameRel, out careInstructionsCatalog, out sectionsCatalog, out madeInCatalog, out fiberCatalog);
                    }

                    var compositions = _orderUtilService.GetComposition(order.OrderGroupID);

                    var compositionPerArticleCode = compositions
                                                    .GroupBy(x => x.ArticleCode)
                                                    .Select(g => g.First())
                                                    .ToList();

                    if(compositionPerArticleCode.Count > 0)
                    {
                        foreach(var compositionItem in compositionPerArticleCode)
                        {
                            var dataReport = GetDataReport(compositionItem, baseDatafieldNames, orderCatalog, detailCatalog, variableDataCatalog, baseDataCatalog, compositionLabelCatalog, tableNameRel);
                            _log.LogMessage($"Brownie Order OrderGroupID: {order.OrderGroupID}, CompanyOrderID {order.CompanyOrderID}");
                            var reportRow = new ReportRow();
                            reportRow = AddFieldsReportRow(reportRow, dataReport, order);
                            var codesSections = GetCodesSections(compositionItem, sectionsCatalog);
                            reportRow = AddSectionsReportRow(reportRow, codesSections);
                            reportRow = GetCodesFibers(compositionItem, reportRow, fiberCatalog);
                            if(reportRow == null)
                                continue;
                            var codesCareInstructions = GetCodesCareIntructions(compositionItem, careInstructionsCatalog);
                            reportRow = AddCareInstructionsToReportRow(codesCareInstructions, reportRow);
                            reportRow = GetCodeMadeIn(dataReport.FullMadeIn, madeInCatalog, reportRow);
                            reportCompositions.Add(reportRow);
                        }

                    }

                });
            }

            return reportCompositions;

        }

        private ReportRow AddCareInstructionsToReportRow(ICollection<string> codesCareInstructions, ReportRow reportRow)
        {
            var tipo = typeof(ReportRow);
            int index = 1;

            foreach(var code in codesCareInstructions)
            {
                string propName = string.Empty;

                if(index <= 6)
                    propName = $"CUIDADO_{index}";
                else if(index <= 11)
                    propName = $"ADICIONALES_{index - 6}";
                else
                    break; // Max 11 instructions (6 + 5)

                var prop = tipo.GetProperty(propName);
                if(prop != null && prop.CanWrite)
                    prop.SetValue(reportRow, code);

                index++;
            }

            return reportRow;
        }

        private ReportRow AddSectionsReportRow(ReportRow reportRow, ICollection<string> codesSections)
        {
            int index = 1;

            foreach(var code in codesSections)
            {
                string propertyName = $"PARTE_{index:D2}"; // Generate PARTE_01, PARTE_02, ...

                var property = typeof(ReportRow).GetProperty(propertyName);
                if(property != null && property.CanWrite)
                {
                    property.SetValue(reportRow, code);
                }

                index++;
                if(index > 4) break; //MAX PARTE_04
            }

            return reportRow;
        }

        private ReportRow AddFieldsReportRow(ReportRow reportRow, DataReport dataReport, Order order)
        {
            reportRow.ORDER = dataReport?.Order;
            reportRow.ARTICULO = dataReport?.Articulo;
            reportRow.COLOR = dataReport?.Color;
            reportRow.DESC_ARTICULO = dataReport?.Desc_Articulo;
            reportRow.DESC_COLOR = dataReport?.Des_Color;
            reportRow.ARTICULO_INDET = dataReport?.ArticleIndet;
            reportRow.FECHA_VALIDADO = order?.ValidationDate?.ToCSVDateFormat();

            return reportRow;
        }

        private ReportRow GetCodeMadeIn(string fullMadeIn, ICatalog madeInCatalog, ReportRow reportRow)
        {
            var madeInList = fullMadeIn.Split('/');

            if(madeInList.Length > 0)
            {
                //English [0]
                var madeInEnglish = madeInList[0];
                var codeMadeIn = SearchCodeInMadeInCatalog(madeInEnglish, madeInCatalog);
                reportRow.MADE_IN = codeMadeIn;
            }

            return reportRow;
        }

        private string SearchCodeInMadeInCatalog(string madeInEnglish, ICatalog madeInCatalog)
        {
            using(var dynDb = _connManager.OpenCatalogDB())
            {
                var madeInCode = dynDb.SelectOne<MadeIn>($@"SELECT Country FROM {madeInCatalog.TableName} m WHERE m.English = '{madeInEnglish?.Trim()}'");

                if(madeInCode == null)
                    throw new Exception($"There is no sections: English: {madeInEnglish} in {madeInCatalog.TableName}");
                return madeInCode.Country;
            }
        }

        private ICollection<string> GetCodesSections(CompositionDefinition composition, ICatalog sectionsCatalog)
        {
            var sections = new List<string>();

            foreach(var section in composition.Sections)
                sections.Add(GetCodeSection(section.SectionID, sectionsCatalog));
            return sections;
        }

        private ReportRow GetCodesFibers(CompositionDefinition composition, ReportRow reportRow, ICatalog fiberCatalog)
        {
            var fiberDict = new List<KeyValuePair<string, string>>();

            foreach(var compositionSection in composition.Sections)
            {
                foreach(var fiber in compositionSection.Fibers)
                {
                    string percent, code;
                    var fiberFound = SearchFiberCode(fiber.FiberID, fiberCatalog);
                    if(fiberFound.IsActive == false)
                        return null;
                    fiberDict.Add(new KeyValuePair<string, string>(fiber?.Percentage, fiberFound.Code));
                }
            }

            return AddFibersToReportRow(fiberDict, reportRow);
        }

        private FiberDTO SearchFiberCode(int fiberID, ICatalog fiberCatalog)
        {
            using(var dynDb = _connManager.OpenCatalogDB())
            {
                var fiberCode = dynDb.SelectOne<FiberDTO>($@"SELECT Code,IsActive FROM {fiberCatalog.TableName} f WHERE f.ID = {fiberID}");

                if(fiberCode == null)
                    throw new Exception($"There isn't FiberID: {fiberID} in {fiberCatalog.TableName}");
                return fiberCode;
            }
        }

        private ReportRow AddFibersToReportRow(List<KeyValuePair<string, string>> fiberDict, ReportRow reportRow)
        {
            int parteIndex = 1;
            int fiberIndex = 1;
            int totalPercent = 0;

            foreach(var kvp in fiberDict)
            {
                int percent = int.Parse(kvp.Key);
                string code = kvp.Value;

                if(totalPercent + percent > 100)
                {
                    // Skip to the next part if you go over 100%
                    parteIndex++;
                    fiberIndex = 1;
                    totalPercent = 0;

                    if(parteIndex > 4) break; // Only until PARTE_04
                }

                string porcentajeProp = $"PORCENTAJE_{parteIndex:D2}_{fiberIndex:D2}";
                string compoProp = $"COMPO_{parteIndex:D2}_{fiberIndex:D2}";

                var tipo = typeof(ReportRow);

                var porcentajePropInfo = tipo.GetProperty(porcentajeProp);
                var compoPropInfo = tipo.GetProperty(compoProp);

                porcentajePropInfo?.SetValue(reportRow, percent.ToString());
                compoPropInfo?.SetValue(reportRow, code);

                totalPercent += percent;
                fiberIndex++;
            }

            return reportRow;
        }

        private string GetCodeSection(int sectionID, ICatalog sectionsCatalog)
        {

            using(var dynDb = _connManager.OpenCatalogDB())
            {
                var sectionCode = dynDb.SelectOne<Section>($@"SELECT Code FROM {sectionsCatalog.TableName} s WHERE s.ID = {sectionID}");

                if(sectionCode == null)
                    throw new Exception($"There isn't sectionsID: English: {sectionID} in {sectionsCatalog.TableName}");
                return sectionCode.Code;
            }
        }

        private ICollection<string> GetCodesCareIntructions(CompositionDefinition composition, ICatalog careInstructionsCatalog)
        {
            var codesCareInstructions = new List<string>();
            if(composition.CareInstructions == null)
                throw new Exception("Care Instructions is Empty! (Report Brownie)");

            foreach(var careInstruction in composition.CareInstructions)
            {
                if(careInstruction?.Instruction != null)
                    codesCareInstructions.Add(GetCodeCareInstructions(careInstruction.Instruction, careInstructionsCatalog));
            }

            return codesCareInstructions;

        }

        private void GetCatalogs(
            IProject project,
            out ICatalog orderCatalog,
            out ICatalog orderDetailsCatalog,
            out ICatalog variableDataCatalog,
            out ICatalog compositionLabelCatalog,
            out ICatalog baseDataCatalog,
            out string tableNameRel,
            out ICatalog careInstructionsCatalog,
            out ICatalog sectionsCatalog,
            out ICatalog madeInCatalog,
            out ICatalog fiberCatalog)
        {
            var catalogs = _catalogRepo.GetByProjectID(project.ID, true);
            orderCatalog = catalogs.First(x => x.TableName.Contains(Catalog.ORDER_CATALOG));
            orderDetailsCatalog = catalogs.First(x => x.TableName.Contains(Catalog.ORDERDETAILS_CATALOG));
            tableNameRel = GetTableNameRel(orderCatalog, orderDetailsCatalog, "Details");
            variableDataCatalog = catalogs.First(x => x.TableName.Contains(Catalog.VARIABLEDATA_CATALOG));
            baseDataCatalog = catalogs.First(x => x.TableName.Contains(Catalog.BASEDATA_CATALOG));
            compositionLabelCatalog = catalogs.First(x => x.TableName.Contains(Catalog.COMPOSITIONLABEL_CATALOG));
            careInstructionsCatalog = catalogs.First(x => x.TableName.Contains(Catalog.BRAND_CAREINSTRUCTIONS_CATALOG));
            sectionsCatalog = catalogs.First(x => x.TableName.Contains(Catalog.BRAND_SECTIONS_CATALOG));
            fiberCatalog = catalogs.First(x => x.TableName.Contains(Catalog.BRAND_FIBERS_CATALOG));
            madeInCatalog = catalogs.First(x => x.TableName.Contains("MadeIn"));

        }

        private string GenerateCSV(ICollection<ReportRow> reportRows, string filePath)
        {
            var quoteCellContent = _config.GetValue<string>("CustomSettings.Brownie.QuoteCellContent", ";");
            var properties = typeof(ReportRow)
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .OrderBy(p => p.MetadataToken)
                            .ToList();

            var headers = GetHeaderLine();

            using(var file = File.OpenWrite(filePath))
            using(var sw = new StreamWriter(file, Encoding.Unicode))
            {
                sw.WriteLine(headers);
                foreach(var row in reportRows)
                {
                    var values = properties.Select(p =>
                    {
                        var value = p.GetValue(row);
                        return value?.ToString()?.Replace(";", ",") ?? ""; // evitar romper el CSV si hay ;
                    });

                    sw.WriteLine(string.Join(quoteCellContent, values));
                }
                return sw.ToString();
            }

        }

        private string GetHeaderLine()
        {
            var quoteCellContent = _config.GetValue<string>("CustomSettings.Brownie.QuoteCellContent", ";");
            var headers = typeof(ReportRow)
                           .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .OrderBy(p => p.MetadataToken)
                           .Select(p => p.Name);

            return string.Join(quoteCellContent, headers);
        }

        private string GetCodeCareInstructions(int careInstructionID, ICatalog careInstructionsCatalog)
        {
            using(var dynDb = _connManager.OpenCatalogDB())
            {
                var careInstructionCode = dynDb.SelectOne<CareInstruction>($@"SELECT Code FROM {careInstructionsCatalog.TableName} ci WHERE ci.ID = {careInstructionID}");

                if(careInstructionCode == null)
                    throw new Exception($"There isn't care instruction ID: {careInstructionID} in {careInstructionsCatalog.TableName}");
                return careInstructionCode.Code;
            }
        }

        private DataReport GetDataReport(CompositionDefinition composition, List<string> baseDatafieldNames, ICatalog orderCatalog, ICatalog orderDetailsCatalog, ICatalog variableDataCatalog, ICatalog baseDataCatalog, ICatalog compositionLabel, string tableNameRel)
        {
            using(PrintDB ctx = _factory.GetInstance<PrintDB>())
            using(var dynDb = _connManager.OpenCatalogDB())
            {
                var orderDataID = ctx.CompanyOrders.Where(co => co.ID == composition.OrderID).Select(co => co.OrderDataID).FirstOrDefault();

                return dynDb.SelectOne<DataReport>(
                            $@"SELECT  {string.Join(',', baseDatafieldNames.ToArray())}  FROM {orderCatalog.TableName} o 
                            INNER JOIN {tableNameRel} r ON o.ID = r.SourceID 
                            INNER JOIN {orderDetailsCatalog.TableName} d ON d.ID = r.TargetID 
                            INNER JOIN {variableDataCatalog.TableName} v ON v.ID = d.Product 
                            INNER JOIN {baseDataCatalog.TableName} bd ON bd.ID = v.IsBaseData
                            WHERE o.ID = {orderDataID}");
            }
        }

        private List<string> GetFielsNamesRequiredReport()
        {
            return new List<string>()
            {
                "bd.Pedido AS [Order]",
                "bd.Articulo",
                "bd.Color",
                "bd.Desc_Articulo",
                "bd.Des_Color",
                "d.ArticleCode AS [ArticleIndet]",
                "v.FullMadeIn"
            };
        }

        private string GetTableNameRel(ICatalog orderCatalog, ICatalog orderDetailsCatalog, string setName)
        {
            var numberOrdersCatalog = orderCatalog.CatalogID;
            var numberOrdersDetailsCatalog = orderDetailsCatalog.CatalogID;
            var definition = orderCatalog.Fields;
            var setField = definition.FirstOrDefault(f => f.Name == setName);

            return $"REL_{numberOrdersCatalog}_{numberOrdersDetailsCatalog}_{setField.FieldID.ToString()}";

        }


        private IEnumerable<Order> GetOrderDataIDs(int projectID)
        {
            DateTime start = DateTime.Today.AddDays(-1);
            DateTime end = DateTime.Now;

#if DEBUG
            end = DateTime.Now;
#endif
            if(_config.GetValue<bool>("WebLink.IsQA", false) == true)
                end = DateTime.Now;
            else
                end = DateTime.Today.AddTicks(-1);

            using(var ctx = _factory.GetInstance<PrintDB>())
            {
                var orders = ctx.PrinterJobs
                            .Join(ctx.PrinterJobDetails,
                                  j => j.ID,
                                  pjd => pjd.PrinterJobID,
                                  (j, pjd) => new { j, pjd })
                            .Join(ctx.Articles,
                                  jp => jp.j.ArticleID,
                                  a => a.ID,
                                  (jp, a) => new { jp.j, jp.pjd, a })
                            .Join(ctx.Labels
                                  .Where(l => l.IncludeComposition),
                                  jpa => jpa.a.LabelID,
                                  l => l.ID,
                                  (jpa, label) => new { jpa.j, jpa.pjd, jpa.a, Label = label })
                            .Join(ctx.CompanyOrders
                                    .Where(w => w.ProjectID == projectID)
                                    .Where(w => w.UpdatedDate >= start && w.UpdatedDate <= end)
                                    .Where(w => w.OrderStatus == OrderStatus.Completed),
                                  jpal => jpal.j.CompanyOrderID,
                                  o => o.ID,
                                  (jpal, o) => new Order
                                  {

                                      CompanyOrderID = o.ID,
                                      ProjectID = o.ProjectID,
                                      OrderGroupID = o.OrderGroupID,
                                      ValidationDate = o.ValidationDate
                                  })
                            .Distinct()
                            .ToList();

                return orders;
            }
        }

        internal class Order
        {
            public int CompanyOrderID { get; set; }
            public int ProjectID { get; set; }
            public int OrderGroupID { get; set; }
            public DateTime? ValidationDate { get; set; }

        }

        internal class DataReport
        {
            public string Order { get; set; }
            public string Articulo { get; set; }
            public string Color { get; set; }
            public string Desc_Articulo { get; set; }
            public string Des_Color { get; set; }
            public string FullComposition { get; set; }
            public string FullMadeIn { get; set; }
            public string FullCareInstructions { get; set; }
            public string ArticleIndet { get; set; }

        }
        internal class Section
        {
            public string Code { get; set; }
        }
        internal class CareInstruction
        {
            public string Code { get; set; }
        }

        internal class MadeIn
        {
            public string Country { get; set; }
        }

        internal class FiberDTO
        {
            public string Code { get; set; }
            public bool IsActive { get; set; }
        }
    }
}

