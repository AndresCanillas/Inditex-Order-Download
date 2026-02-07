using LinqKit;
using Service.Contracts;
using Service.Contracts.Documents;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services.Mango;

namespace WebLink.Services
{
    public class MangoOrderTrackingReportGeneratorService : IMangoOrderTrackingDailyReportService
    {
        private readonly IDBConnectionManager conn;
        private readonly IProjectRepository projectRepo;
        private readonly ICatalogRepository catalogRepo;
        private readonly IFileStoreManager storeManager;
        private readonly IDataImportService documentService;
        private readonly ILogService log;
        private readonly ITempFileService tempFileservice;
        private readonly IOrderEmailService emailService;
        private MangoDailyReportConfig config;

        private static string PRODUCED_BY = "INDET";

        public MangoOrderTrackingReportGeneratorService(
            IDBConnectionManager conn,
            IProjectRepository projectRepo,
            ICatalogRepository catalogRepo,
            IFileStoreManager storeManager,
            IDataImportService documentService,
            ILogService log,
            ITempFileService tempFileservice,
            IOrderEmailService emailService,
            IAppConfig config
            )
        {
            this.conn = conn;
            this.projectRepo = projectRepo;
            this.catalogRepo = catalogRepo;
            this.storeManager = storeManager;
            this.documentService = documentService;
            this.log = log;
            this.tempFileservice = tempFileservice;
            this.emailService = emailService;
            this.config = config.Bind<MangoDailyReportConfig>("CustomSettings.Mango.DailyReport");
        }

        public void SendReport(int companyID, DateTime startDate, DateTime endDate)
        {
            var data = GetReportData(companyID, startDate, endDate);

            var csvPath = tempFileservice.GetTempFileName($"MangoOrderDailyReport_{startDate.ToString("yyyyMMdd")}.csv", true);

            var excelFileID = CreateFile(data, csvPath);


            var filePath = tempFileservice.GetTempFileName($"MangoOrderDailyReport_{startDate.ToString("yyyyMMdd")}.xlsx", true);

            var excelFile = storeManager.GetFile(excelFileID);

            File.WriteAllBytes(filePath, excelFile.GetContentAsBytes());

            PutFileViaEmail(filePath, startDate);

        }

        private void PutFileViaEmail(string filePath, DateTime startDate)
        {
            emailService.SendMessage(
                config.EmailAddress,
                $"MANGO - INDET Order daily report  {startDate.ToString("yyyy-MM-dd")}",
                "Please look at the attached file",
                new List<string> { filePath }).Wait();
        }

        private Guid CreateFile(List<List<string>> data, string filePath)
        {
            // create CSV contet

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


            var tempStore = storeManager.OpenStore("TempStore");

            var dataFile = tempStore.CreateFile($"MANGODAILYORDERREPORT_{DateTime.Now.ToString("yyyyMMddHHmmssff")}.csv");

            dataFile.SetContent(filePath);

            var task = documentService.CreateExcelFromCSV(new Service.Contracts.Documents.ExcelConfigurationRequest() { FromCSVFileID = (dataFile as IFSFile).FileGUID });

            task.Wait();

            if(!task.Result.Success)
            {
                throw new Exception("Mango Report File cannot be created");
            }

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<DocumentImportCreateExcelResponse>(task.Result.Data.ToString());

            //var excelFileId = (task.Result.Data as DocumentImportCreateExcelResponse).ExcelFileID;

            return response.ExcelFileID;
        }

        private List<List<string>> GetReportData(int companyID, DateTime startDate, DateTime endDate)
        {

            var apiData = new List<List<string>>();
            
            apiData = GetReportDataFromAPIBrand(companyID, startDate, endDate);
            // remove info from old brand
            var textData = GetReportDataFromTextBrand(companyID, startDate, endDate);

            textData.RemoveAt(0); // remove header

            apiData.AddRange(textData);

            return apiData;

        }
        #region API BRAND
        private List<List<string>> GetReportDataFromAPIBrand(int companyID, DateTime startDate, DateTime endDate)
        {
            // ONLY use API Brand
            // TODO: how to identify projects that must be reported
            // 133 -> Brand API -> 2025/03/31
            var projects = projectRepo.GetByBrandID(133, false);

            List<List<string>> reportData = new List<List<string>>();

            // Add Headers
            reportData.Add(new List<string>
            {
                "COMPANY",
                "COUNTRY",
                "SAP_ORDER",
                "VENDOR_CODE",
                "NUMBER_ORDER",
                "REFERENCE_ITEM",
                "QUANTITY",
                "RECEIVED_DATE",
                "VENDOR_CONFIRM",
                "REQUIREMENT_DATE",
                "FIRST_PRODUCTION_DATE",
                "LAST_PRODUCTION_DATE",
                "SHIPMENT_DATE"

            });


            using(var db = conn.OpenWebLinkDB())
            using(var dynDb = conn.OpenCatalogDB())
            {
                foreach(var project in projects)
                {

                    var printData = new List<OrderFieldsRequired>();
                    List<OrderFieldsRequired> printInfo = OrderWebInfo(startDate, endDate, db, project);

                    var orderDataID = printInfo.Select(s => s.ORDER_DATA_ID).ToList();
                    if(orderDataID.Count > 0)
                    {
                        int blocksize = 1000;
                        int t = 0;
                        while(t < orderDataID.Count)
                        {
                            var BlockOrderDataIds = orderDataID.Skip(t).Take(blocksize).ToList();
                            var sqlStr = CreateDataQueryForAPIBrand(project, BlockOrderDataIds);
                            printData.AddRange(dynDb.Select<OrderFieldsRequired>(sqlStr));
                            t += blocksize;
                        }
                    }

                    var projectData = printInfo
                        .Join(printData, i => i.ORDER_DATA_ID, d => d.ORDER_DATA_ID, (i, p) => new { Info = i, Data = p })
                        .Select(s => new List<string>
                        {
                            PRODUCED_BY,
                            string.IsNullOrEmpty(s.Data.COUNTRY) ? string.Empty: Regex.Replace( s.Data.COUNTRY, "(Made In|Hecho en)", string.Empty, RegexOptions.IgnoreCase) ,
                            s.Data.NUMBER_ORDER,
                            s.Data.VENDOR_CODE,
                            s.Data.NUMBER_ORDER,
                            s.Data.REFERENCE_ITEM,
                            s.Info.QUANTITY,
                            s.Info.RECEIVED_DATE,
                            s.Info.VENDOR_CONFIRM,
                            s.Info.REQUIREMENT_DATE,
                            s.Info.FIRST_PRODUCTION_DATE,
                            s.Info.LAST_PRODUCTION_DATE,
                            s.Info.SHIPMENT_DATE
                        });

                    reportData.AddRange(projectData);

                }
            }


            return reportData;
        }


        private string CreateDataQueryForAPIBrand(IProject project, IList<int> orderDataId)
        {
            var allCatalogs = catalogRepo.GetByProjectID(project.ID);
            var orderCt = allCatalogs.Single(s => s.Name == Catalog.ORDER_CATALOG);
            var detailCt = allCatalogs.Single(s => s.Name == Catalog.ORDERDETAILS_CATALOG);
            var varDataCt = allCatalogs.Single(s => s.Name == Catalog.VARIABLEDATA_CATALOG);
            var baseDataCt = allCatalogs.Single(s => s.Name == Catalog.BASEDATA_CATALOG);

            var relField = orderCt.Fields.Single(s => s.Name == "Details");

            var sql = $@"
            SELECT DISTINCT
            bd.countryorigin AS COUNTRY
            , bd.SupplierCode AS VENDOR_CODE
            , bd.LabelOrderId as SAP_ORDER
            , bd.LabelOrderId as NUMBER_ORDER
            , bd.LabelID as [REFERENCE_ITEM]   
            , do.ID as ORDER_DATA_ID
            FROM {orderCt.TableName} do
            INNER JOIN REL_{orderCt.CatalogID}_{detailCt.CatalogID}_{relField.FieldID} r on do.ID = r.SourceID
            INNER JOIN {detailCt.TableName} d on d.ID = r.TargetID
            INNER JOIN {varDataCt.TableName} v on v.ID = d.Product
            INNER JOIN {baseDataCt.TableName} bd on bd.ID = v.IsBaseData
            WHERE do.ID in ({string.Join(',', orderDataId)})";

            return sql;

        }

        #endregion

        #region TEXT Brand 

        private List<List<string>> GetReportDataFromTextBrand(int companyID, DateTime startDate, DateTime endDate)
        {
            // ONLY use  Brand Mango
            // TODO: how to identify projects that must be reported
            // 40 -> Brand Mango -> 2025/03/31
            var projects = projectRepo.GetByBrandID(40, false);

            List<List<string>> reportData = new List<List<string>>();

            // Add Headers
            reportData.Add(new List<string>
            {
                "COMPANY",
                "COUNTRY",
                "SAP_ORDER",
                "VENDOR_CODE",
                "NUMBER_ORDER",
                "REFERENCE_ITEM",
                "QUANTITY",
                "RECEIVED_DATE",
                "VENDOR_CONFIRM",
                "REQUIREMENT_DATE",
                "FIRST_PRODUCTION_DATE",
                "LAST_PRODUCTION_DATE",
                "SHIPMENT_DATE"
                

            });


            var validProject = new List<int> { 217, 249, 301 };

            using(var db = conn.OpenWebLinkDB())
            using(var dynDb = conn.OpenCatalogDB())
            {
                foreach(var project in projects.Where(w => validProject.Any(a => a == w.ID) ))
                {
                    var printData = new List<OrderFieldsRequired>();

                    List<OrderFieldsRequired> printInfo = OrderWebInfo(startDate, endDate, db, project);

                    var orderDataID = printInfo.Select(s => s.ORDER_DATA_ID).ToList();
                    
                    if(orderDataID.Count > 0)
                    {
                        int blocksize = 1000;
                        int t = 0;
                        while(t < orderDataID.Count)
                        {
                            var BlockOrderDataIds = orderDataID.Skip(t).Take(blocksize).ToList();
                            var sqlStr = CreateDataQueryForTextBrand(project, BlockOrderDataIds);
                            printData.AddRange(dynDb.Select<OrderFieldsRequired>(sqlStr));
                            t+=blocksize;
                        }
                    }

                    var projectData = printInfo
                        .Join(printData, i => i.ORDER_DATA_ID, d => d.ORDER_DATA_ID, (i, p) => new { Info = i, Data = p })
                        .Select(s => new List<string>
                        {
                            PRODUCED_BY,
                            string.IsNullOrEmpty(s.Data.COUNTRY) ? string.Empty: Regex.Replace( s.Data.COUNTRY, "(Made In|Hecho en)", string.Empty, RegexOptions.IgnoreCase) ,
                            s.Data.SAP_ORDER,
                            s.Data.VENDOR_CODE,
                            s.Data.NUMBER_ORDER,
                            s.Data.REFERENCE_ITEM,
                            s.Info.QUANTITY,
                            s.Info.RECEIVED_DATE,
                            s.Info.VENDOR_CONFIRM,
                            s.Info.REQUIREMENT_DATE,
                            s.Info.FIRST_PRODUCTION_DATE,
                            s.Info.LAST_PRODUCTION_DATE,
                            s.Info.SHIPMENT_DATE
                        });

                    reportData.AddRange(projectData);
                }
            }

            return reportData;
        }

        private string CreateDataQueryForTextBrand(IProject project, IList<int> orderDataId)
        {
            var catalogsWihtDAta = GetCatalogsWithLabelData();

            var allCatalogs = catalogRepo.GetByProjectID(project.ID);
            var orderCt = allCatalogs.Single(s => s.Name == Catalog.ORDER_CATALOG);
            var detailCt = allCatalogs.Single(s => s.Name == Catalog.ORDERDETAILS_CATALOG);
            var varDataCt = allCatalogs.Single(s => s.Name == Catalog.VARIABLEDATA_CATALOG);
            var madeinCt = allCatalogs.Single(s => s.Name == "MadeIn");

            var relField = orderCt.Fields.Single(s => s.Name == "Details");

            var stdTables = $@"
FROM 
{orderCt.TableName} do
INNER JOIN REL_{orderCt.CatalogID}_{detailCt.CatalogID}_{relField.FieldID} r on do.ID = r.SourceID
INNER JOIN {detailCt.TableName} d on d.ID = r.TargetID
INNER JOIN {varDataCt.TableName} v on v.ID = d.Product
";


            var sql =
                $@"
SELECT DISTINCT * FROM

(select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.country_origin_txt AS COUNTRY
{stdTables}
inner join $PVPV5801_KDL_KDO$ bd on bd.ID = v.IsPVPV5801  

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, mi.espanol AS COUNTRY
{stdTables}
inner join $PVP46XX$ bd on bd.ID = v.IsPVPV46XX 
left join $MadeIn$ mi on mi.codigo = bd.country_of_origin_code ) 

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.txt_country_of_origin AS COUNTRY
{stdTables}
inner join $ADHV3004$ bd on bd.ID = v.IsADHV3004 ) 

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, mi.espanol AS COUNTRY
{stdTables}
inner join $CB00_R88_1088_D88_1S88$ bd on bd.ID = v.IsFABRIC87 
left join $MadeIn$ mi on mi.codigo = bd.country_origin_code_zone_a
) 

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.country_origin_txt AS COUNTRY
{stdTables}
inner join $ADHV2006$ bd on bd.ID = v.IsADHV2006 ) 

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER,mi.espanol AS COUNTRY
{stdTables}
inner join $CPO_TIM_NTU$ bd on bd.ID = v.IsCPOTIMNTU 
left join $MadeIn$ mi on mi.codigo = bd.country_of_origin_code
) 

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.txt_country_of_origin AS COUNTRY
{stdTables}
inner join $ADHV1006$ bd on bd.ID = v.IsADHV1006 
)

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.txt_country_of_origin AS COUNTRY
{stdTables}
inner join $ADHV4002$ bd on bd.ID = v.IsADHV4002 
)

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, '' AS COUNTRY
{stdTables}
inner join $IX8$ bd on bd.ID = v.IsIX7

) UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, '' AS COUNTRY
{stdTables}
inner join $PVPV0102$ bd on bd.ID = v.IsPVPV0102 
)

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.country_origin_txt AS COUNTRY
{stdTables}
inner join $PVPV2104$ bd on bd.ID = v.IsPVPV2104 
)

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, '' AS COUNTRY
{stdTables}
inner join $PVPV_62_91$ bd on bd.ID = v.IsPVPV_62_91
) 

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, mi.espanol AS COUNTRY
{stdTables}
inner join $GI00_CN8_CC8$ bd on bd.ID = v.IsGI00_CN7_CC7
left join $MadeIn$ mi on mi.codigo = bd.country_of_origin
)

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, mi.espanol AS COUNTRY
{stdTables}
inner join $GI00_CI3_CD3_CI2$ bd on bd.ID = v.IsGI00_CI3_CD3_CI2 
left join $MadeIn$ mi on mi.codigo = bd.country_of_origin_code
) 

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, mi.espanol AS COUNTRY
{stdTables}
inner join $GI00_KI3_KD3_KI2$ bd on bd.ID = v.isGI00_KI3_KD3_KI2 
left join $MadeIn$ mi on mi.codigo = bd.country_of_origin_code
)

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.country_origin_txt AS COUNTRY
{stdTables}
inner join $BoxV3000$ bd on bd.ID = v.IsBoxV3000 
)

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.CountryOfOrigin AS COUNTRY
{stdTables}inner join $BoxV1000$ bd on bd.ID = v.BoxV1000 
)

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, mi.espanol AS COUNTRY
{stdTables}
inner join $ADHV90XX$ bd on bd.ID = v.IsADHV90XX 
left join $MadeIn$ mi on mi.codigo = bd.country_of_origin_code
) 

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, '' AS COUNTRY
{stdTables}
inner join $GI005CIN$ bd on bd.ID = v.IsGI005CIN 
)

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, bd.country_of_origin AS COUNTRY
{stdTables}
inner join $GIUTFCHI$ bd on bd.ID = v.IsGIUTFCHI 
)

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, '' AS COUNTRY
{stdTables}
inner join $CAX$ bd on bd.ID = v.IsCAX 
)

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, '' AS COUNTRY
{stdTables}
inner join $CRS$ bd on bd.ID = v.IsCRS 
) 

UNION ALL (

select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER,  '' AS COUNTRY
{stdTables}
inner join $GI000_BAW_BAC$ bd on bd.ID = v.IsGI000_BAW_BAC 
)

UNION ALL (
select do.ID AS ORDER_DATA_ID, do.OrderNumber AS NUMBER_ORDER, d.ArticleCode AS REFERENCE_ITEM, v.SapCode as SAP_ORDER, mi.espanol AS COUNTRY
{stdTables}
inner join $PVPV85$ bd on bd.ID = v.IsPVPV85
left join $MadeIn$ mi on mi.codigo = bd.country_of_origin) 
) AX

where AX.ORDER_DATA_ID in({string.Join(',', orderDataId)})
";


            catalogsWihtDAta.ForEach(c =>
            {
                sql = sql.Replace($"${c}$", allCatalogs.First(ct => ct.Name == c).TableName);
            });

            sql = sql.Replace($"${madeinCt.Name}$", madeinCt.TableName);

            return sql;
        }

        private IList<string> GetCatalogsWithLabelData()
        {
            var CatalogsToWork = new List<string>
            {
"PVPV5801_KDL_KDO",
"PVP46XX",
"ADHV3004",
"CB00_R88_1088_D88_1S88",
"ADHV2006",
"CPO_TIM_NTU",
"ADHV1006",
"ADHV4002",
"IX8",
"PVPV0102",
"PVPV2104",
"PVPV_62_91",
"GI00_CN8_CC8",
"GI00_CI3_CD3_CI2",
"GI00_KI3_KD3_KI2",
"BoxV3000",
"BoxV1000",
"ADHV90XX",
"GI005CIN",
"GIUTFCHI",
"CAX",
"CRS",
"GI000_BAW_BAC",
"PVPV85"
            };

            return CatalogsToWork;
        }

        #endregion

        private List<OrderFieldsRequired> OrderWebInfo(DateTime startDate, DateTime endDate, Service.Contracts.Database.IDBX db, IProject project)
        {
            int chunkSize = 7;  // days
            var result= new List<OrderFieldsRequired>();
            var chunkInitDate = startDate; 

            while (chunkInitDate < endDate)
            {
                chunkSize =  endDate-chunkInitDate < TimeSpan.FromDays(chunkSize) ? (endDate - chunkInitDate).Days : chunkSize; 
                
                var chunkEndDate = chunkInitDate.AddDays(chunkSize);    
                
                var partialWebInfo = PartialOrderWebInfo(chunkInitDate, chunkEndDate, db, project);
                
                result.AddRange(partialWebInfo);
                
                chunkInitDate = chunkInitDate.AddDays(chunkSize);   
            }

            return result;
        }

        private List<OrderFieldsRequired> PartialOrderWebInfo(DateTime startDate, DateTime endDate, Service.Contracts.Database.IDBX db, IProject project)
        {
            return db.Select<OrderFieldsRequired>($@"

DROP TABLE IF EXISTS #mango_daily_end
DROP TABLE IF EXISTS #mango_daily_start
DROP TABLE IF EXISTS #mango_daily
DROP TABLE IF EXISTS #mango_delivery_info


SELECT 
  FORMAT (d.ShippingDate, 'yyyy-MM-dd') as ShippingDate
, co.ID as OrderID
, ISNULL(CAST(pd.quantity as VARCHAR(8)), '') as QUANTITY
INTO #mango_delivery_info
FROM DeliveryNotes d
JOIN Packages p ON p.DeliveryNoteID = d.ID
JOIN PackageDetails pd ON pd.PackageID = p.ID
LEFT join PrinterJobDetails pjd ON pjd.ID = pd.PrinterJobDetailID
JOIN PrinterJobs pj ON pj.id = isnull (pd.PrinterJobID,pjd.PrinterJobID)
JOIN CompanyOrders co ON co.ID = pj.CompanyOrderID
--WHERE co.OrderGroupID = 309309
WHERE co.ProjectID = @projectID  --249
AND co.UpdatedDate >= @startDate --'2025-02-16 00:00:00'
AND co.UpdatedDate  < @endDate --'2025-02-17 00:00:00'
AND co.OrderStatus not in ( 7,1,2) -- cancelled
AND co.SendToCompanyID <> @TEST_COMPANY_ID



SELECT 
  ISNULL(CAST(dn.QUANTITY as VARCHAR(8)), '') AS QUANTITY
, CAST(FORMAT(co.OrderDate, 'yyyy-MM-dd') AS VARCHAR(10)) AS RECEIVED_DATE
, CAST(FORMAT(co.ValidationDate, 'yyyy-MM-dd') AS VARCHAR(10)) AS VENDOR_CONFIRM
, CAST(FORMAT(DATEADD(DAY, pv.SLADays, co.ValidationDate), 'yyyy-MM-dd') as VARCHAR(10)) AS REQUIREMENT_DATE
, CAST('' AS VARCHAR(10)) AS FIRST_PRODUCTION_DATE
, CAST('' AS VARCHAR(10)) AS LAST_PRODUCTION_DATE
, dn.ShippingDate AS SHIPMENT_DATE 
, co.ID AS ORDER_ID
, co.OrderDataID AS ORDER_DATA_ID
INTO #mango_daily
FROM CompanyOrders co
INNER JOIN CompanyProviders pv ON pv.ID = co.ProviderRecordID
LEFT JOIN #mango_delivery_info  dn on dn.OrderID = co.ID
--WHERE co.OrderGroupID = 309309
WHERE co.ProjectID = @projectID2  --249
AND co.UpdatedDate >= @startDate2 --'2025-02-16 00:00:00'
AND co.UpdatedDate  < @endDate2 --'2025-02-17 00:00:00'
AND co.OrderStatus not in ( 7,1,2) -- cancelled
AND co.SendToCompanyID <> @TEST_COMPANY_ID2


SELECT l.OrderID, MIN(CreatedDate) AS FIRST_PRODUCTION_DATE
INTO #mango_daily_start
FROM OrderLogs l
WHERE OrderID in (SELECT ORDER_ID from #mango_daily)
AND l.Message like '%OrderStatus changed by PrintLocal. New Status: 3 - Printing%'
GROUP BY l.OrderID 

SELECT l.OrderID, MIN(CreatedDate) AS LAST_PRODUCTION_DATE
INTO #mango_daily_end
FROM OrderLogs l
WHERE OrderID in (SELECT ORDER_ID from #mango_daily)
AND l.Message like '%OrderStatus changed by PrintLocal. New Status: 6 - Completed%'
GROUP BY l.OrderID

UPDATE d set FIRST_PRODUCTION_DATE = FORMAT(s.FIRST_PRODUCTION_DATE, 'yyyy-MM-dd')
FROM #mango_daily d
INNER JOIN #mango_daily_start s ON d.ORDER_ID = s.OrderID

UPDATE d set LAST_PRODUCTION_DATE = FORMAT(s.LAST_PRODUCTION_DATE, 'yyyy-MM-dd')
FROM #mango_daily d
INNER JOIN #mango_daily_end s ON d.ORDER_ID = s.OrderID

-- return result
SELECT * 
FROM #mango_daily

DROP TABLE IF EXISTS #mango_daily_end
DROP TABLE IF EXISTS #mango_daily_start
DROP TABLE IF EXISTS #mango_daily
DROP TABLE IF EXISTS #mango_delivery_info

",
project.ID, startDate.ToString("yyyy-MM-dd") + " 00:00:00", endDate.ToString("yyyy-MM-dd") + " 00:00:00",
Company.TEST_COMPANY_ID,
project.ID, startDate.ToString("yyyy-MM-dd") + " 00:00:00", endDate.ToString("yyyy-MM-dd") + " 00:00:00",
Company.TEST_COMPANY_ID
);
        }

    }


    internal class OrderFieldsRequired
    {
        public string COUNTRY;
        public string SAP_ORDER;
        public string VENDOR_CODE;
        public string NUMBER_ORDER;
        public string REFERENCE_ITEM;
        public string QUANTITY;    
        public string RECEIVED_DATE;
        public string VENDOR_CONFIRM;
        public string REQUIREMENT_DATE;
        public string FIRST_PRODUCTION_DATE;
        public string LAST_PRODUCTION_DATE;
        public string SHIPMENT_DATE;

        /* to cross print with printdata*/
        public int ORDER_ID;
        public int ORDER_DATA_ID;
    }

    internal class MangoDailyReportConfig
    {
        public string EmailAddress;
        public string QuoteCellContent;
        public string Separator;
    }
}
