using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;

namespace WebLink.Services.Automated
{
    class IrisSyncProcess : IAutomatedProcess
    {
#if DEBUG
        public static string TABLE_NAME = "soh_dev";
#else
        public static string TABLE_NAME = "soh";
#endif
        private IEnumerable<IrisOrder> LastUpdated { get; set; }
        private IConnectionManager cnm { get; set; }
        //private IOrderRepository orderRepo { get; set; }
        private IFactory factory { get; set; }
        private IAppConfig config { get; set; }
        private ILogSection log { get; set; }
        private ILogSection logData { get; set; }
        private IOrderRegisterInERP erpService { get; set; }

        private IOrderRepository orderRepository { get; set; }
        private ICompanyRepository companyRepository { get; set; }
        private IOrderWithArticleDetailedService orderWithArticleDetailedService;
        private IArtifactRepository artifactRepo;

        private bool IsFirstTime { get; set; }
        private bool EnableFullLog { get; set; }

        private bool Enabled { get { return config.GetValue<bool>("WebLink.Iris.Enabled", false); } }

        public IrisSyncProcess(IFactory factory, IConnectionManager cnm, IAppConfig config, ILogService log, IOrderRegisterInERP erpService, IOrderRepository orderRepository, ICompanyRepository companyRepository, IOrderWithArticleDetailedService orderWithArticleDetailedService, IArtifactRepository artifactRepo)
        {
            this.factory = factory;
            this.cnm = cnm;
            this.config = config;
            this.erpService = erpService;
            this.log = log.GetSection("IrisExceptions");
            this.logData = log.GetSection("IrisSyncProcess");

            EnableFullLog = config.GetValue<bool>("WebLink.Iris.EnabledFullLog", false);
            this.IsFirstTime = true;
            this.orderRepository = orderRepository;
            this.companyRepository = companyRepository;
            this.orderWithArticleDetailedService = orderWithArticleDetailedService;
            this.artifactRepo = artifactRepo;
        }

        public TimeSpan GetIdleTime()
        {
            var delta = config.GetValue<double>("WebLink.Iris.FrequencyInMinutes");

            if(IsFirstTime)
            {
                delta = 0.2;
            }

            return TimeSpan.FromMinutes(delta);
        }

        public void OnExecute()
        {
            if(!Enabled)
                return;

            logData.LogMessage("Iris Sync Started");
            // select order from Print
            LastUpdated = GetOrdersToReport();

            // insert or update orders into Iris
            ReportToIris();

            // TODO: LOG  of reported orders
            logData.LogMessage("Iris Sync Executed");
        }

        public void OnLoad()
        {

        }

        public void OnUnload()
        {

        }

        // always return order modified in the last period
        // las hour, last day, last minute
        private IEnumerable<IrisOrder> GetOrdersToReport()
        {

            var delta = config.GetValue<int>("WebLink.Iris.FrequencyInMinutes");

            if(IsFirstTime)
            {
                IsFirstTime = false;

                var reportFull = config.GetValue<bool>("WebLink.Iris.ReportFullEnabled", false);

                if(reportFull)
                {
                    delta = config.GetValue<int>("WebLink.Iris.ReportFullDelta", 1440);
                }
            }


            using(var db = cnm.OpenDB("MainDB"))
            {

                var ordersUpdated = db.Select<IrisOrder>(QueryTemplate(), -1 * 4 * delta);

                return ordersUpdated;
            }
        }

        private void ReportToIris()
        {

            logData.LogMessage("Reporting to iris {0} orders", LastUpdated.ToList().Count);
            logData.LogMessage(config.GetValue("Databases.Iris.Provider", "--"));
            logData.LogMessage(config.GetValue("Databases.Iris.ConnStr", "--"));

            int pos = 0;
            foreach(var order in LastUpdated)
            {
                
                using(var db = cnm.OpenDB("Iris"))
                {


                    if(pos++ % 100 == 0)
                    {
                        System.Threading.Thread.Sleep(250);
                    }
                    try
                    {



                        ComleteOrderInfo(order);
                        var PAD_ORDERID = order.PRINT_ID.ToString("D6");
                        var PAD_GROUPID = order.ORDER_GROUP_ID.ToString("D6");

                        // how the MDOrderNumber was created - some orders use orderID order use "G"+OrderGroupID
                        // the correct for the future is use order alwsay, this is to get backward compatibility
                        //var registerWithGroup = Regex.IsMatch(order.CUSORDREF, "-G[0-9]{6,}$") ? 1 : 0;
                        //var registerWithOrder = Regex.IsMatch(order.CUSORDREF, "-[0-9]{6,}$") ? 1 : 0;

                        // is group, get the correct Order

                        var registeredOrders = db.Select<IrisOrder>(SelectTemplate(), order.PRINT_ID, order.ITMREF, order.SOHNUM, PAD_ORDERID, PAD_GROUPID);

                        IrisOrder registeredOrder = registeredOrders.LastOrDefault();

                        foreach(var item in registeredOrders)
                        {
                            if(item != null && (item.COD_TALLER != order.COD_TALLER || item.SOHNUM != order.SOHNUM || (item.ESTADO_PROD == "5" && order.ESTADO_PROD != "5")))
                            {
                                //if (EnableFullLog) log.LogWarning("Order  {0} {1}", pos, Newtonsoft.Json.JsonConvert.SerializeObject(order));

                                db.ExecuteNonQuery(DeleteTemplate(),
                                    item.ID_SOH
                                );

                                if(EnableFullLog) logData.LogWarning("Order Removed  {0} {1}", pos, order.CUSORDREF);

                                registeredOrder = null;// set as null to allow insert new record after

                                order.ELIMINADO = null;

                            }
                        }


                        // TODO: cambio de proveedor - cambia el numero de la orden o se modifico la configuracion y ahora esta o deja de estar en SAGE, paso del estado pendiente de validacion a validado
                        //if (registeredOrder != null && (registeredOrder.COD_TALLER != order.COD_TALLER || registeredOrder.SOHNUM != order.SOHNUM || (registeredOrder.ESTADO_PROD == "5" && order.ESTADO_PROD != "5")))
                        //{
                        //    //if (EnableFullLog) log.LogWarning("Order  {0} {1}", pos, Newtonsoft.Json.JsonConvert.SerializeObject(order));

                        //    db.ExecuteNonQuery(DeleteTemplate(),
                        //        registeredOrder.ID_SOH
                        //    );

                        //    if (EnableFullLog) log.LogWarning("Order Removed  {0} {1}", pos, order.CUSORDREF);

                        //    registeredOrder = null;// set as null to allow insert new record after

                        //}

                        //ignore order if has a ERP config and is not syncked with ERP
                        if(!string.IsNullOrEmpty(order.ERP_CONFIG_ID) && order.SYNC_WITH_SAGE == "0" && order.PRINT_STATUS == ((int)OrderStatus.Validated))
                        {
                            if(EnableFullLog) logData.LogWarning("Order skiped, sync with SAGE is pending {0} {1}", pos, Newtonsoft.Json.JsonConvert.SerializeObject(order));
                            continue;
                        }




                        //if (EnableFullLog) log.LogWarning("Order {0} {1}", pos, Newtonsoft.Json.JsonConvert.SerializeObject(order));


                        //var sqlQuery = registeredOrder == null ? InsertTemplate() : UpdateTemplate();

                        // Manage detailed articles  
                        //using(var transaccion = db.BeginTransaction())
                        //{
                        //    try
                        //    {
                        db.ExecuteNonQuery($"UPDATE  `sage`.`{TABLE_NAME}` SET ELIMINADO = 'S' WHERE ID_PRINT=@ID_PRINT AND ESTADO_PROD <> '99'", order.PRINT_ID);
                        var SendToCompany = companyRepository.GetByCompanyCode(order.BPCORD);
                        orderWithArticleDetailedService.Execute(order.PRINT_ID, SendToCompany.ID,
                                            ((idSage, articleDescription, quantity, a) => RegisterOrderDetailed(idSage, articleDescription, quantity, db, order, registeredOrder, a)),
                                            ((quantity, a) => RegisterOrder(db, order, registeredOrder, a)),
                                            (quantity, a, line) => AddArtifacts(a, quantity, line, db, order, registeredOrder));
                        //        transaccion.Commit();

                        //    }
                        //    catch(Exception ex)
                        //    {
                        //        throw ex;
                        //    }
                        //}


                        if(EnableFullLog) logData.LogWarning("Order Reported: {0} {1}", pos, order.CUSORDREF);
                    }
                    catch(Exception _ex)
                    {
                        log.LogException($"Order Not Reported OrderID [{order.CUSORDREF}] can not be register on IRIS", _ex);
                        // ??? : add notification
                    }

                }// usind db

            }// end foreach LastUpdated
            
        }

        private void AddArtifacts(OrderDetailDTO orderDetailInfo, int total, int line, IDBX db, IrisOrder order, IrisOrder registeredOrder)
        {


            //var artifacts = artifactRepo.GetByArticle(ctx, a.ArticleID).ToList();

            //if (artifacts != null || artifacts.Count > 0)
            //{
            //    var startingLine = sageOrder.MaxNumberItemLines(orderInfo.OrderID.ToString());
            //    //foreach (var artifact in artifacts)
            //    for (int artPos = startingLine; artPos < startingLine + artifacts.Count(); artPos++)
            //    {
            //        var artifact = artifacts.ElementAt(artPos - startingLine);

            //        if (!artifact.SyncWithSage)
            //        {
            //            continue;
            //        }

            //        ISageRequestItem artifactItm = new SageRequestItem();
            //        artifactItm.SetReference(artifact.SageRef);
            //        artifactItm.SetCustomerRef(artifact.Name);
            //        artifactItm.SetQuantity(total);
            //        artifactItm.SetWsReference($"{orderInfo.OrderID.ToString()}-{(artPos + 1)}");
            //        sageOrder.AddItem(artifactItm);
            //    }
            //}


            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var artifacts = artifactRepo.GetByArticle(ctx, orderDetailInfo.ArticleID).ToList();
                if(artifacts != null || artifacts.Count > 0)
                {
                    // var startingLine = sageOrder.MaxNumberItemLines(a.OrderID.ToString());
                    var startingLine = line;

                    for(int artPos = startingLine; artPos < startingLine + artifacts.Count(); artPos++)
                    {
                        var artifact = artifacts.ElementAt(artPos - startingLine);

                        if(!artifact.SyncWithSage)
                        {
                            continue;
                        }

                        order.ITMDES = artifact.Name;
                        order.ID_SAGE = $"{orderDetailInfo.OrderID.ToString()}-{(artPos + 1)}";
                        order.QTY = total.ToString();
                        order.ITMREF = artifact.SageRef;
                        RegisterOrder(db, order, registeredOrder, orderDetailInfo);
                    }
                }

            }



        }

        private void RegisterOrderDetailed(string idSageDetailed, string articleDescription, int quantity, IDBX db, IrisOrder order, IrisOrder registeredOrder, OrderDetailDTO a)
        {
            order.ITMDES = articleDescription;
            order.ID_SAGE = idSageDetailed;
            order.QTY = quantity.ToString();
            RegisterOrder(db, order, registeredOrder, a);
        }



        private void RegisterOrder(IDBX db, IrisOrder order, IrisOrder registeredOrder, OrderDetailDTO orderDetailInfo)
        {

            if (registeredOrder == null)
            {                
                InsertTemplateToIRIS(db, order);

                return;
            }


            var query = $@"SELECT ID_SAGE, ID_SOH, SOHNUM FROM `sage`.`{TABLE_NAME}` WHERE ID_SAGE = @ID_SAGE";

            var registeredOrders = db.Select<IrisOrder>(query, order.ID_SAGE);


            if(!registeredOrders.Any() || registeredOrders.Count(ro => ro.SOHNUM == order.SOHNUM) < 1)
            {
                if(string.IsNullOrEmpty(order.ELIMINADO) || order.ELIMINADO != "S")
                    InsertTemplateToIRIS(db, order);
            }
            else
            {
                UpdateTemplateToIRIS(db, order, registeredOrders.First(ro => ro.SOHNUM == order.SOHNUM));
            }
        }

        private void UpdateTemplateToIRIS(IDBX db, IrisOrder order, IrisOrder registeredOrder)
        {
            db.ExecuteNonQuery(UpdateTemplate(),
                //order.SOHNUM,
                order.SALFCY,
                order.STOFCY,
                order.ORDDAT,
                order.BPCORD,
                order.BPCNAM,
                order.CUSORDREF,
                order.PJT,
                order.ITMREF,
                order.ITMDES,
                order.QTY,
                order.DATA_COMANDA,
                order.DATA_ENTREGA,
                order.ELIMINADO,
                order.ESTADO_PROD,
                order.FECHA_FIN_PROD,
                order.REGMOD,
                order.MARCA,
                order.COD_TALLER,
                order.NOM_TALLER,
                order.TEMPORADA,
                order.PRINT_ID,//ID_PRINT
                order.ID_SAGE,
                order.FECHA_ENTRADA,
                order.CODART_CLIENTE,
                order.PROPIETARIO,

                registeredOrder.ID_SOH
                );
        }

        private void InsertTemplateToIRIS(IDBX db, IrisOrder order)
        {
            db.ExecuteNonQuery(InsertTemplate(),
                order.SOHNUM,
                order.SALFCY,
                order.STOFCY,
                order.ORDDAT,
                order.SHIDAT,
                order.BPCORD,
                order.BPCNAM,
                order.CUSORDREF,
                order.PJT,
                order.ITMREF,
                order.ITMDES,
                order.QTY,
                order.DATA_COMANDA,
                order.DATA_ENTREGA,
                order.ELIMINADO,
                order.ESTADO_PROD,
                order.FECHA_FIN_PROD,
                order.REGMOD,
                order.MARCA,
                order.COD_TALLER,
                order.NOM_TALLER,
                order.TEMPORADA,
                order.PRINT_ID,//ID_PRINT
                order.ID_SAGE,
                order.FECHA_ENTRADA,
                order.CODART_CLIENTE,
                order.PROPIETARIO

                );
        }

        private void ComleteOrderInfo(IrisOrder order)
        {
            var orderInfo = new OrderInfoDTO()
            {
                OrderID = order.PRINT_ID,
                OrderNumber = order.ORDER_NUMBER,
                CompanyCode = order.COMPANY_CODE,
                ProjectCode = order.PROJECT_CODE,
                BillToCompanyCode = order.BILL_TO_CODE,
                SendTo = order.SEND_TO_CODE
            };

            if(string.IsNullOrEmpty(order.CUSORDREF) || string.IsNullOrEmpty(order.PJT))
            {
                order.CUSORDREF = erpService.GetMDReference(orderInfo, null);
                order.PJT = erpService.GetProjectCodeShared(orderInfo);
            }
        }

        private string InsertTemplate()
        {

            var q = $@"INSERT INTO `sage`.`{TABLE_NAME}`
            (`SOHNUM`,
            `SALFCY`,
            `STOFCY`,
            `ORDDAT`,
            `SHIDAT`,
            `BPCORD`,
            `BPCNAM`,
            `CUSORDREF`,
            `PJT`,
            `ITMREF`,
            `ITMDES`,
            `QTY`,
            `DATA_COMANDA`,
            `DATA_ENTREGA`,
            `ELIMINADO`,
            `ESTADO_PROD`,
            `FECHA_FIN_PROD`,
            `REGMOD`,
            `MARCA`,
            `COD_TALLER`,
            `NOM_TALLER`,
            `TEMPORADA`,
            `ID_PRINT`,
            `ID_SAGE`,
            `FECHA_ENTRADA`,
            `CODART_CLIENTE`,
            `PROPIETARIO`
            )
            VALUES
            (@SOHNUM,
            @SALFCY,
            @STOFCY,
            @ORDDAT,
            @SHIDAT,
            @BPCORD,
            @BPCNAM,
            @CUSORDREF,
            @PJT,
            @ITMREF,
            @ITMDES,
            @QTY,
            @DATA_COMANDA,
            @DATA_ENTREGA,
            @ELIMINADO,
            @ESTADO_PROD,
            @FECHA_FIN_PROD,
            @REGMOD,
            @MARCA,
            @COD_TALLER,
            @NOM_TALLER,
            @TEMPORADA,
            @ID_PRINT,
            @ID_SAGE,
            @FECHA_ENTRADA,
            @CODART_CLIENTE,
            @PROPIETARIO 
            )
            ";
            return q;
        }

        private string UpdateTemplate()
        {
            var q = $@"
            UPDATE `sage`.`{TABLE_NAME}`
            SET 
            SALFCY = @SALFCY,
            STOFCY = @STOFCY,
            ORDDAT = @ORDDAT,
            BPCORD = @BPCORD,
            BPCNAM = @BPCNAM,
            CUSORDREF = @CUSORDREF,
            PJT = @PJT,
            ITMREF = @ITMREF,
            ITMDES = @ITMDES,
            QTY = @QTY,
            DATA_COMANDA = @DATA_COMANDA,
            DATA_ENTREGA = @DATA_ENTREGA,
            ELIMINADO = @ELIMINADO,
            ESTADO_PROD = @ESTADO_PROD,
            FECHA_FIN_PROD = @FECHA_FIN_PROD,
            REGMOD = @REGMOD,
            MARCA = @MARCA,
            COD_TALLER = @COD_TALLER,
            NOM_TALLER = @NOM_TALLER,
            TEMPORADA = @TEMPORADA,
            ID_PRINT = @ID_PRINT,
            ID_SAGE = @ID_SAGE,
            FECHA_ENTRADA = @FECHA_ENTRADA,
            CODART_CLIENTE = @CODART_CLIENTE,
            PROPIETARIO = @PROPIETARIO
           

            WHERE ID_SOH = @ID_SOH
            ";

            return q;
        }

        private string DeleteTemplate()
        {
            var q = $@"
            UPDATE `sage`.`{TABLE_NAME}`
            SET 
            ELIMINADO = 'S'
            WHERE ID_SOH = @ID_SOH
            ";

            return q;
        }

        private string SelectTemplate()
        {
            ///XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
            /// esperando por el campo clave para buscar, donde se guardara el identificador de cada linea
            var q = $@"
SELECT 
   `ID_SOH`,
   `ID_SDH`,
   `ID_PRINT`,
   `ID_SAGE`,
   `ID_IRIS`,
   `SOHNUM`,
   `SALFCY`,
   `STOFCY`,
   `ORDDAT`,
   `SHIDAT`,
   `BPCORD`,
   `BPCNAM`,
   `CUSORDREF`,
   `PJT`,
   `ITMREF`,
   `ITMDES`,
   `QTY`,
    DATE_FORMAT(`DATA_COMANDA`, '%Y-%m-%d 00:00:00'),
    DATE_FORMAT(`DATA_ENTREGA`, '%Y-%m-%d 00:00:00'),
    `ELIMINADO`,
    `ESTADO_PROD`,
    DATE_FORMAT(`FECHA_FIN_PROD`, '%Y-%m-%d'),
    CASE WHEN `REGMOD` THEN '1' ELSE '0' END AS REGMOD,
    `MARCA`,
    `TEMPORADA`,
    `COD_TALLER`,
    `NOM_TALLER`,
    `PROPIETARIO`
FROM `sage`.`{TABLE_NAME}`
WHERE ID_PRINT = @ID_PRINT";
            return q;
        }

        private string QueryTemplate()
        {

            

            var q = @"
            SELECT
CASE
	WHEN o.ValidationDate < '2022-02-20 00:00:00' OR o.CreatedDate > '2022-03-10 00:00:00' THEN COALESCE(o.SageReference, CONCAT(o.OrderNumber, '-', ISNULL(st.CompanyCode, 'XXX'))) 
	-- contains SAGE Number, use that, else combine, PRINTWEB-OrderGroupID; allowed max len 32
	ELSE COALESCE(
				o.SageReference,
				CONCAT('PRINTWEB-',CAST(o.OrderGroupID as VARCHAR(12)))
		)
	END
                                                      AS SOHNUM,

ISNULL(l.FactoryCode, 'FACTORY_WEB')                  AS SALFCY,
ISNULL(l.FactoryCode, 'FACTORY_WEB')                  AS STOFCY,
FORMAT(ISNULL(o.ValidationDate, o.CreatedDate), 'yyyyMMdd')                  
                                                      AS ORDDAT,
NULL                                                  AS SHIDAT,
st.CompanyCode                                        AS BPCORD,
st.[Name]                                             AS BPCNAM,
ISNULL(o.MDOrderNumber, '')                           AS CUSORDREF,
ISNULL(o.ProjectPrefix, '')                           AS PJT,
ISNULL(a.BillingCode, a.ArticleCode)                  AS ITMREF,
a.[Name]                                              AS ITMDES,
CONVERT(VARCHAR(10),j.Quantity)                       AS QTY,
FORMAT(ISNULL(o.ValidationDate, o.CreatedDate), 'yyyy-MM-dd') 
                                                      AS DATA_COMANDA,
FORMAT(ISNULL(o.DueDate,'1999-12-31'), 'yyyy-MM-dd')  AS DATA_ENTREGA,

				

CASE WHEN o.OrderStatus IN (3) THEN '20'-- imprimiendo
	WHEN o.OrderStatus IN (6) THEN '99' -- producido
	WHEN o.OrderStatus IN (1,2,20) THEN '5' -- recibidas sin validar, no se saben si se van a producir
	ELSE '10' END                                    AS ESTADO_PROD, -- validadas-> se deben producir
CASE WHEN o.OrderStatus IN (3) THEN 'IMPRIMIENDO'-- imprimiendo
	WHEN o.OrderStatus IN (6) THEN 'PRODUCIDO' -- producido
	WHEN o.OrderStatus IN (1,2,20) THEN 'NO VALIDADO' -- espera
	ELSE 'VALIDADO' END                                    AS ESTADO_PROD_TEXT,

CASE WHEN o.OrderStatus = 7 THEN 'S'
ELSE NULL END									     AS ELIMINADO,

CASE WHEN o.OrderStatus = 6 THEN FORMAT(o.UpdatedDate, 'yyyy-MM-dd') 
ELSE NULL END										 AS FECHA_FIN_PROD,

o.OrderStatus										 AS PRINT_STATUS,
CAST( e.ID as VARCHAR(8))							 AS ERP_CONFIG_ID,
o.ID												 AS PRINT_ID,
pj.ProjectCode                                       AS PROJECT_CODE,
st.CompanyCode										 AS SEND_TO_CODE,
o.OrderNumber                                        AS ORDER_NUMBER,
bt.CompanyCode                                       AS BILL_TO_CODE,
rq.CompanyCode                                       AS COMPANY_CODE,
CASE WHEN LEN(o.SageReference) > 0 THEN '1'
     ELSE '0' END                                    AS SYNC_WITH_SAGE,
'1'													 AS REGMOD,
bd.Name      										 AS MARCA,
pv.ClientReference									 AS COD_TALLER,
st.Name												 AS NOM_TALLER,
pj.ProjectCode                                       AS TEMPORADA
--
-- esto no va en print --
--,a.ArticleCode									     AS CLIENT_ARTICLE,
--a.Description									     AS ARTICLE_DESRIPTION,
--CASE
--	WHEN lb.Type = 1 THEN 'STICKER'
--	WHEN lb.Type = 2 THEN 'CARELABEL'
--	WHEN lb.Type = 3 THEN 'HANGTAG'
--	WHEN lb.Type = 4 THEN 'STICKER'
--	ELSE 'ITEM' END								AS ARTICLE_TYPE


,o.OrderGroupID AS ORDER_GROUP_ID
,CAST(o.ID as VARCHAR(12)) as ID_SAGE
,FORMAT(o.CreatedDate, 'yyyy-MM-dd') 				AS FECHA_ENTRADA
,a.ArticleCode                                      AS CODART_CLIENTE
,rq.CompanyCode                                     AS PROPIETARIO
,o.SendToCompanyID                                  AS SEND_TO_COMPANY_ID
FROM CompanyOrders o
INNER JOIN OrderUpdateProperties p ON o.ID = p.OrderID
INNER JOIN Locations l ON o.LocationID = l.ID
INNER JOIN Companies rq ON o.CompanyID = rq.ID
INNER JOIN Companies st ON o.SendToCompanyID = st.ID
INNER JOIN Companies bt ON o.BillToCompanyID = bt.ID
INNER JOIN Projects pj ON o.ProjectID = pj.ID
INNER JOIN Brands bd ON pj.BrandID = bd.ID
INNER JOIN PrinterJobs j ON o.ID = j.CompanyOrderID
LEFT JOIN Articles a ON j.ArticleID = a.ID
LEFT JOIN Labels lb on a.LabelID = lb.ID
LEFT JOIN ERPCompanyLocations e ON o.BillToCompanyID = e.CompanyID AND o.LocationID = e.ProductionLocationID
LEFT JOIN CompanyProviders pv on pv.ID = o.ProviderRecordID
WHERE  
1=1
AND o.UpdatedDate >=  DATEADD(n,@delta, GETDATE() )
AND (p.IsActive = 1 OR (p.IsActive = 0 AND o.OrderStatus = 7) )
AND o.ProductionType = 1
-- remove test providers
AND st.CompanyCode != 'ttqa'
-- reportar a iris a partir de esta fecha 2022-1-27
AND (o.ValidationDate >= '2022-02-25 00:00:00' OR o.CreatedDate >= '2022-02-25 00:00:00')
-- ignore orders by status, sometimes orders stuck in this status for configuration problem
AND o.OrderStatus NOT IN (0,1,2) 
-- AND o.ID = 1191803 -- for testing 
ORDER BY o.UpdatedDate ASC
                        ";

            return q;
        }



        private string QueryTemplate_Test()
        {
            var q = @"
SELECT
CASE
	WHEN o.ValidationDate < '2022-02-20 00:00:00' OR o.CreatedDate > '2022-03-10 00:00:00' THEN COALESCE(o.SageReference, CONCAT(o.OrderNumber, '-', ISNULL(st.CompanyCode, 'XXX'))) 
	-- contains SAGE Number, use that, else combine, PRINTWEB-OrderGroupID; allowed max len 32
	ELSE COALESCE(
				o.SageReference,
				CONCAT('PRINTWEB-',CAST(o.OrderGroupID as VARCHAR(12)))
		)
	END
                                                      AS SOHNUM,

ISNULL(l.FactoryCode, 'FACTORY_WEB')                  AS SALFCY,
ISNULL(l.FactoryCode, 'FACTORY_WEB')                  AS STOFCY,
FORMAT(ISNULL(o.ValidationDate, o.CreatedDate), 'yyyyMMdd')                  
                                                      AS ORDDAT,
NULL                                                  AS SHIDAT,
st.CompanyCode                                        AS BPCORD,
st.[Name]                                             AS BPCNAM,
ISNULL(o.MDOrderNumber, '')                           AS CUSORDREF,
ISNULL(o.ProjectPrefix, '')                           AS PJT,
ISNULL(a.BillingCode, a.ArticleCode)                  AS ITMREF,
a.[Name]                                              AS ITMDES,
CONVERT(VARCHAR(10),j.Quantity)                       AS QTY,
FORMAT(ISNULL(o.ValidationDate, o.CreatedDate), 'yyyy-MM-dd') 
                                                      AS DATA_COMANDA,
FORMAT(ISNULL(o.DueDate,'1999-12-31'), 'yyyy-MM-dd')  AS DATA_ENTREGA,

				

CASE WHEN o.OrderStatus IN (3) THEN '20'-- imprimiendo
	WHEN o.OrderStatus IN (6) THEN '99' -- producido
	WHEN o.OrderStatus IN (1,2,20) THEN '5' -- recibidas sin validar, no se saben si se van a producir
	ELSE '10' END                                    AS ESTADO_PROD, -- validadas-> se deben producir
CASE WHEN o.OrderStatus IN (3) THEN 'IMPRIMIENDO'-- imprimiendo
	WHEN o.OrderStatus IN (6) THEN 'PRODUCIDO' -- producido
	WHEN o.OrderStatus IN (1,2,20) THEN 'NO VALIDADO' -- espera
	ELSE 'VALIDADO' END                                    AS ESTADO_PROD_TEXT,

CASE WHEN o.OrderStatus = 7 THEN 'S'
ELSE NULL END									     AS ELIMINADO,

CASE WHEN o.OrderStatus = 6 THEN FORMAT(o.UpdatedDate, 'yyyy-MM-dd') 
ELSE NULL END										 AS FECHA_FIN_PROD,

o.OrderStatus										 AS PRINT_STATUS,
CAST( e.ID as VARCHAR(8))							 AS ERP_CONFIG_ID,
o.ID												 AS PRINT_ID,
pj.ProjectCode                                       AS PROJECT_CODE,
st.CompanyCode										 AS SEND_TO_CODE,
o.OrderNumber                                        AS ORDER_NUMBER,
bt.CompanyCode                                       AS BILL_TO_CODE,
rq.CompanyCode                                       AS COMPANY_CODE,
CASE WHEN LEN(o.SageReference) > 0 THEN '1'
     ELSE '0' END                                    AS SYNC_WITH_SAGE,
'1'													 AS REGMOD,
bd.Name      										 AS MARCA,
pv.ClientReference									 AS COD_TALLER,
st.Name												 AS NOM_TALLER,
pj.ProjectCode                                       AS TEMPORADA
--
-- esto no va en print --
--,a.ArticleCode									     AS CLIENT_ARTICLE,
--a.Description									     AS ARTICLE_DESRIPTION,
--CASE
--	WHEN lb.Type = 1 THEN 'STICKER'
--	WHEN lb.Type = 2 THEN 'CARELABEL'
--	WHEN lb.Type = 3 THEN 'HANGTAG'
--	WHEN lb.Type = 4 THEN 'STICKER'
--	ELSE 'ITEM' END								AS ARTICLE_TYPE


,o.OrderGroupID AS ORDER_GROUP_ID
,CAST(o.ID as VARCHAR(12)) as ID_SAGE
,FORMAT(o.CreatedDate, 'yyyy-MM-dd') 				AS FECHA_ENTRADA
,a.ArticleCode                                      AS CODART_CLIENTE
,rq.CompanyCode                                     AS PROPIETARIO
FROM CompanyOrders o
INNER JOIN OrderUpdateProperties p ON o.ID = p.OrderID
INNER JOIN Locations l ON o.LocationID = l.ID
INNER JOIN Companies rq ON o.CompanyID = rq.ID
INNER JOIN Companies st ON o.SendToCompanyID = st.ID
INNER JOIN Companies bt ON o.BillToCompanyID = bt.ID
INNER JOIN Projects pj ON o.ProjectID = pj.ID
INNER JOIN Brands bd ON pj.BrandID = bd.ID
INNER JOIN PrinterJobs j ON o.ID = j.CompanyOrderID
LEFT JOIN Articles a ON j.ArticleID = a.ID
LEFT JOIN Labels lb on a.LabelID = lb.ID
LEFT JOIN ERPCompanyLocations e ON o.BillToCompanyID = e.CompanyID AND o.LocationID = e.ProductionLocationID
LEFT JOIN CompanyProviders pv on pv.ID = o.ProviderRecordID
WHERE  
1=1
AND o.id = 745723
ORDER BY o.UpdatedDate ASC
            ";

            return q;
        }
    }


    public class IrisOrder
    {
        public string SOHNUM { get; set; }
        public string SALFCY { get; set; }
        public string STOFCY { get; set; }
        public string ORDDAT { get; set; }
        public string SHIDAT { get; set; }
        public string BPCORD { get; set; }
        public string BPCNAM { get; set; }
        public string CUSORDREF { get; set; }
        public string PJT { get; set; }
        public string ITMREF { get; set; }
        public string ITMDES { get; set; }
        public string QTY { get; set; }
        public string DATA_COMANDA { get; set; }
        public string DATA_ENTREGA { get; set; }
        public string ELIMINADO { get; set; }
        public string ESTADO_PROD { get; set; }
        public string FECHA_FIN_PROD { get; set; }
        public int PRINT_STATUS { get; set; }
        public string ERP_CONFIG_ID { get; set; }
        public int PRINT_ID { get; set; }
        public string PROJECT_CODE { get; set; }
        public string SEND_TO_CODE { get; set; }
        public string ORDER_NUMBER { get; set; }
        public string BILL_TO_CODE { get; set; }
        public string COMPANY_CODE { get; set; }
        public string SYNC_WITH_SAGE { get; set; }
        public string REGMOD { get; set; }
        public string MARCA { get; set; }
        public string COD_TALLER { get; set; }
        public string NOM_TALLER { get; set; }
        public string TEMPORADA { get; set; }
        public int ORDER_GROUP_ID { get; set; }
        public int? ID_PRINT { get; set; }
        public string ID_SAGE { get; set; }
        public int ID_SOH { get; set; }
        public string FECHA_ENTRADA { get; set; }
        public string CODART_CLIENTE { get; set; }
        public string PROPIETARIO { get; set; }
        public string FACTOR_MULT { get; set; } = "1";
        public int SEND_TO_COMPANY_ID { get; set; }
    }
}
