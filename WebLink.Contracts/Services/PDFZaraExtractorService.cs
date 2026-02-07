using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Workflows;

namespace WebLink.Contracts.Services
{
    public class PDFZaraExtractorService : IPDFZaraExtractorService
    {

        public class ManualOrderData
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

        private readonly IUserData userData;
        private readonly ILocalizationService g;
        private readonly ILogService log;
        private readonly ITempFileService temp;
        private readonly IBrandRepository brandRepo;
        private readonly IProjectRepository projectRepo;
        private readonly IAutomatedProcessManager apm;
        private readonly IRemoteFileStore workflowStore;
        private readonly IFactory factory;
        private readonly ICatalogRepository catalogRepo;
        private readonly ICatalogDataRepository catalogDataRepository;
        private readonly IFileStoreManager storeManager;
        private readonly PrintDB printDB;
        private IConnectionManager connManager;
        private IOrderUtilService orderUtilService;


        public PDFZaraExtractorService(IUserData userData,
                                  //                            IAppConfig config,
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
                                  IFileStoreManager storeManager,
                                  IConnectionManager connManager,
                                  IOrderUtilService orderUtilService)
        {
            this.userData = userData;
            this.g = g;
            this.log = log;
            this.temp = temp;
            this.brandRepo = brandRepo;
            this.projectRepo = projectRepo;
            this.apm = apm;
            this.workflowStore = workflowStore;
            this.factory = factory;
            this.catalogRepo = catalogRepo;
            this.catalogDataRepository = catalogDataRepository;
            this.storeManager = storeManager;
            this.printDB = factory.GetInstance<PrintDB>();
            this.storeManager = storeManager;
            this.workflowStore = storeManager.OpenStore("WorkflowStore");
            this.connManager = connManager;
            this.orderUtilService = orderUtilService;
        }
        private string GetPrecioSafe(List<PrecioEtiqueta> precios, int index)
        {
            if(precios == null || index < 0 || index >= precios.Count)
                return "0";

            return precios[index]?.PrecioDivisa.Replace(",",".") ?? "0";
        }

        private string GetDivisaSafe(List<PrecioEtiqueta> precios, int index)
        {
            if(precios == null || index < 0 || index >= precios.Count)
                return string.Empty;

            return precios[index]?.TipoDivisa ?? string.Empty;
        }

        private string SanitizeQuantity(string quantity)
        {
            if (string.IsNullOrEmpty(quantity))
                return "0";

            // Remove dots and commas
            return quantity.Replace(".", "").Replace(",", "").Trim();
        }

        private string SanitizeObservaciones(string observaciones)
        {
            if (string.IsNullOrEmpty(observaciones))
                return string.Empty;

            // Replace line breaks, tabs, and other special characters with a space
            // Then trim extra spaces and limit to 63 characters if needed
            return string.Join(" ", 
                observaciones.Replace("\r\n", " ")
                            .Replace("\n", " ")
                            .Replace("\r", " ")
                            .Replace("\t", " ")
                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    .Trim();
        }

        private string PadColorWithZeros(string color)
        {
            if (string.IsNullOrEmpty(color))
                return "000";

            // Trim the color and pad with leading zeros to ensure 3 characters
            return color.Trim().PadLeft(3, '0');
        }

        private List<PrecioEtiqueta> ReorderPriceLabel(List<PrecioEtiqueta> precios)
        {
            if (precios == null)
                return null;

            return precios
                .OrderByDescending(p => p.EtiquetaColor?.ToUpper() == "BLUE")
                .ThenByDescending(p => p.EtiquetaColor?.ToUpper() == "RED")
                .ToList();
        }

        public async Task<OperationResult> SendOrder(PDFZaraExtractor entry)
        {
            int projectid = 0;
            int.TryParse(entry.Pedido.BusinessInformation.ProjectID, out projectid);
            int brandid = 0;
            int.TryParse(entry.Pedido.BusinessInformation.BrandID, out brandid);

            int companyid = 0;
            int.TryParse(entry.Pedido.BusinessInformation.CompanyID, out companyid);


            string ComposicionSerializada = string.Empty;
            if(entry.DatosAdicionales?.EtiquetaComposicion != null)
            {
                ComposicionSerializada = JsonConvert.SerializeObject(entry.DatosAdicionales?.EtiquetaComposicion)
                                          .Replace("\"", "'");
            }
            // ComposicionSerializada = "La historia de la humanidad es un entramado complejo de decisiones, descubrimientos y transformaciones que han moldeado el presente que hoy habitamos. Desde los albores de la civilización, el ser humano ha buscado comprender el mundo que lo rodea, desarrollar herramientas, construir comunidades y dejar una huella duradera en el tiempo. Las antiguas civilizaciones, como Sumeria, Egipto, Grecia y Roma, sentaron las bases del pensamiento, el arte, la política y la ciencia. Cada una de ellas aportó una pieza esencial al gran rompecabezas de la evolución social y cultural.";

            var dto = new ZaraManualEntryHelper.ManualOrderData()
            {
                CompanyID = companyid,
                BrandID = brandid,
                ProjectID = projectid,
                ProductionType = 1,  //IDTLocation
                IsBillable = true
            };



            var path = temp.GetTempDirectory();
            var fileName = $"Order-{entry.Pedido.BusinessInformation.PedidoZara}.csv";
            var tempFileName = Path.Combine(path, fileName);
            using(FileStream fs = new FileStream(tempFileName, FileMode.OpenOrCreate))
            {
                fs.SetLength(0L);
                string delimiterChar = ";";
                var listOfSortPrices = ReorderPriceLabel(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios);
                using(StreamWriter writer = new StreamWriter(fs))
                {
                    foreach(var articulo in entry.Pedido.Articulos)
                    {

                        foreach(var talla in articulo.Tallas)
                        {
                            var sanitizedObservaciones = SanitizeObservaciones(entry.Pedido.ObservacionesGenerales);
                            var observaciones = sanitizedObservaciones.Length < 64 ? sanitizedObservaciones + delimiterChar : sanitizedObservaciones.Substring(0, 63) + delimiterChar;
                            writer.WriteLine(
                                    entry.Pedido.BusinessInformation.PedidoZara + delimiterChar
                                    + entry.Pedido.BusinessInformation.IdProveedor + delimiterChar
                                    + entry.Pedido.BusinessInformation.ProjectID + delimiterChar
                                    + entry.Pedido.BusinessInformation.SeccionID + delimiterChar
                                    + entry.Pedido.BusinessInformation.MadeInID + delimiterChar
                                    + articulo.NormalizeMCC.Modelo + delimiterChar
                                    + articulo.NormalizeMCC.Calidad + delimiterChar
                                    + observaciones 
                                    + entry.Main.Fecha.ToString() + delimiterChar
                                    + GetPrecioSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 0) + delimiterChar
                                    + GetDivisaSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 0) + delimiterChar
                                    + GetPrecioSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 1) + delimiterChar
                                    + GetDivisaSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 1) + delimiterChar
                                    + GetPrecioSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 2) + delimiterChar
                                    + GetDivisaSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 2) + delimiterChar
                                    + GetPrecioSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 3) + delimiterChar
                                    + GetDivisaSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 3) + delimiterChar
                                    + GetPrecioSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 4) + delimiterChar
                                    + GetDivisaSafe(entry.DatosAdicionales?.EtiquetaPrecio?.InfoPrecio?.Precios, 4) + delimiterChar
                                    + articulo.ArticleCode + delimiterChar
                                    + entry.Pedido.BusinessInformation.IdSizeset + delimiterChar
                                    + talla.TallaID + delimiterChar // TallaID ?? 
                                    + entry.Pedido.SizeRange + delimiterChar
                                    + PadColorWithZeros(articulo.NormalizeMCC.Color) + delimiterChar
                                    + SanitizeQuantity(talla.Cantidad) + delimiterChar
                                    + entry.Pedido.BusinessInformation.SubSeccionID + delimiterChar
                                    + "" + delimiterChar
                                    + ComposicionSerializada + delimiterChar
                                    + "bar" + delimiterChar
                                    + articulo.NormalizeMCC.Temporada

                                );
                        }
                    }
                }
            }

            string orderData = JsonConvert.SerializeObject(dto);
            var result = await IntakeFtpUpload2(orderData, tempFileName);
            return result;
        }

        

        private async Task<OperationResult> IntakeFtpUpload2(string orderData, string filepath)
        {
            OperationResult result = new OperationResult();


            try
            {
                var dto = JsonConvert.DeserializeObject<UploadOrderDTO>(orderData);
                var project = await projectRepo.GetByIDAsync(dto.ProjectID, true);
                var brand = await brandRepo.GetByIDAsync(project.BrandID, true);
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

        public async Task<CompositionDefinition> GetCompositionDefinitionFromCompositionJSON(int projectid, int[] orders)
        {
            CompositionDefinition result = null; 

            foreach(var orderid in orders)
            {
                try
                {
                    var (companyOrder, error) = await GetCompanyOrder(orderid);
                    if(error != null)
                        continue;

                    var listBaseDataInfo = GetBaseDataCompositionInfo(projectid, orderid, companyOrder);

                    if(listBaseDataInfo == null) continue;


                    var compositionData = GetCompositionData(listBaseDataInfo
                                                                        .FirstOrDefault(b => !string.IsNullOrEmpty(b.CompositionJSON)),
                                                              orderid);
                    if(compositionData == null)
                       continue;

                    var (allowedDetails, flowControl, compositionValue) = await GetAndValidateAllowedDetails(orderid, companyOrder);
                    if(!flowControl)
                        return compositionValue;

                    var compositionDefinition = GetCompositionDefinition(companyOrder, compositionData, allowedDetails);
                    if(compositionDefinition == null || !compositionDefinition.Any())
                    {
                        log.LogMessage($"Failed to create composition definition for orderid: {orderid}");
                        continue;
                    }

                    foreach(var item in compositionDefinition)
                    {
                        await SaveCompositionDefinitionAsync(item);
                    }

                    UpdateCompositionJson(listBaseDataInfo, projectid);
                    return  compositionDefinition.FirstOrDefault();
                }
                
                catch(Exception ex)
                {
                    log.LogException($"Error processing composition for projectid: {projectid}, orderid: {orderid}", ex);
                    return null;
                }
                
            }
            return null;
        }

        private void UpdateCompositionJson(List<BaseDataCompositionJSON> listBaseDataInfo, int projectid)
        {
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                var baseDataCatalog = ctx.Catalogs
                    .FirstOrDefault(c => c.Name.Equals("BaseData")
                    && c.ProjectID.Equals(projectid));

                using(var dynDb = connManager.OpenDB("CatalogDB"))
                {
                    var listOfIDs = string.Join(",", listBaseDataInfo.Select(x => x.ID));
                    dynDb.ExecuteNonQuery($"UPDATE {baseDataCatalog.TableName} SET CompositionJSON = '' WHERE ID IN ({listOfIDs}) "); 
                }
            }
        }

        private async Task<(Order order, string error)> GetCompanyOrder(int orderid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                var order = ctx.CompanyOrders.FirstOrDefault(o => o.ID == orderid);
                var errorMessage = string.Empty;

                if (order == null)
                {
                    errorMessage = $"CompanyOrder not found for orderid: {orderid}";
                    log.LogMessage(errorMessage);
                    return await Task.FromResult((order, errorMessage));
                }
                return await Task.FromResult((order, errorMessage));
            }
        }

        private List<BaseDataCompositionJSON> GetBaseDataCompositionInfo(int projectid, int orderid, Order companyOrder)
        {
            var listBaseDataInfo = GetCompositionJSON(projectid, orderid, companyOrder);

            if(listBaseDataInfo == null)
            {
                return null;
            }

            var baseDataInfo = listBaseDataInfo.FirstOrDefault(b => !string.IsNullOrEmpty(b.CompositionJSON));
            if(baseDataInfo == null || string.IsNullOrEmpty(baseDataInfo.CompositionJSON))
            {
                log.LogMessage($"No composition data found for orderid: {orderid}");
                return null;
            }
            return listBaseDataInfo;
        }

        private EtiquetaComposicion GetCompositionData(BaseDataCompositionJSON baseDataInfo, int orderid)
        {
            var compositionData = JsonConvert.DeserializeObject<EtiquetaComposicion>(baseDataInfo.CompositionJSON);
            if (compositionData == null)
            {
                log.LogMessage($"Failed to deserialize composition for orderid: {orderid}");
                return null;
            }

            return compositionData;
        }

        private async Task<(IEnumerable<OrderGroupSelectionDTO> details, bool flowControl, CompositionDefinition value)> 
            GetAndValidateAllowedDetails(int orderid, Order companyOrder)
        {
            IEnumerable<OrderGroupSelectionDTO> allowedDetails;
            var (flowControl, value) = GetAllowedDetails(orderid, companyOrder, out allowedDetails);
            return await Task.FromResult((allowedDetails, flowControl, value));
        }

        private async Task<CompositionDefinition> SaveCompositionDefinitionAsync(CompositionDefinition compositionDefinition)
        {
            try
            {
                var compo = orderUtilService.SaveCompositionDefinition(compositionDefinition);

                return await Task.FromResult(compo);
            }
            catch (Exception ex)
            {
                log.LogException("Error saving composition definition", ex);
                return null;
            }
        }

        private (bool flowControl, CompositionDefinition value) GetAllowedDetails(int orderid, Order companyOrder, out IEnumerable<OrderGroupSelectionDTO> allowedDetails)
        {
            OrderGroupSelectionDTO orderGroupSelectionDTO = new OrderGroupSelectionDTO()
            {
                OrderGroupID = companyOrder.OrderGroupID,
                OrderNumber = companyOrder.OrderNumber,
                WizardStepPosition = 2,
                Orders = new int[] { orderid },
            };
            var listOfOrderGroupSelectionDTO = new List<OrderGroupSelectionDTO>() { orderGroupSelectionDTO };

            allowedDetails = orderUtilService.CurrentOrderedLablesGroupBySelectionV2(listOfOrderGroupSelectionDTO);
            if(allowedDetails == null)
            {
                log.LogMessage($"Imposible to reach allowedDetails form orderid {orderid}");
                return (flowControl: false, value: null);
            }

            return (flowControl: true, value: null);
        }

        private List<CompositionDefinition> GetCompositionDefinition(Order order, 
                                                               EtiquetaComposicion etiqueta_composicion,
                                                               IEnumerable<OrderGroupSelectionDTO> allowedDetails
                                                               )
        {
            var result = new List<CompositionDefinition>();
            if (etiqueta_composicion?.InfoComposicion == null) return null;

            var sections = ProcessSections(etiqueta_composicion.InfoComposicion.ComposicionPrenda?.Secciones);
            if (sections == null) return null;

            var cares = ProcessCareInstructions(etiqueta_composicion.InfoComposicion.InstruccionesConservacion, order.ProjectID);
            
            // Procesar excepciones y cuidados adicionales
            ProcessAdditionalCareInstructions(etiqueta_composicion.InfoComposicion, cares);

            foreach(var detail in allowedDetails.FirstOrDefault().Details)
            {
                result.Add(new CompositionDefinition()
                {
                    ArticleID = detail.ArticleID,
                    CareInstructions = cares,
                    KeyName = "Color",
                    KeyValue =detail.Color ?? string.Empty,
                    OrderGroupID = order.OrderGroupID ,
                    OrderDataID = order.OrderDataID,
                    ProjectID = order.ProjectID,
                    ProductDataID = detail.ProductDataID,
                    Sections = sections,
                    OrderID = order.ID, 
                    Quantity = detail.Quantity
                }); 
            }

            return result;
        }

        private List<Section> ProcessSections(IEnumerable<Seccion> secciones)
        {
            if (secciones == null) return null;

            return secciones
                .Where(seccion => !string.IsNullOrEmpty(seccion?.ID))
                .Select(seccion => new
                {
                    SeccionId = int.TryParse(seccion.ID, out var id) ? (int?)id : null,
                    Fibers = ProcessFibers(seccion.Fibras)
                })
                .Where(x => x.SeccionId.HasValue && x.Fibers != null && x.Fibers.Any())
                .Select(x => new Section
                {
                    SectionID = x.SeccionId.Value,
                    Fibers = x.Fibers
                })
                .ToList();
        }

        private List<Fiber> ProcessFibers(IEnumerable<Fibra> fibras)
        {
            if (fibras == null) return null;

            return fibras
                .Where(fibra => fibra != null && !string.IsNullOrEmpty(fibra.ID))
                .Select(fibra => new
                {
                    FiberId = int.TryParse(fibra.ID, out var id) ? (int?)id : null,
                    Porcentaje = SanitizePorcentaje(fibra.Porcentaje)
                })
                .Where(x => x.FiberId.HasValue)
                .Select(x => new Fiber
                {
                    FiberID = x.FiberId.Value,
                    Percentage = x.Porcentaje
                })
                .ToList();
        }

        private string SanitizePorcentaje(string porcentaje)
        {
            if (string.IsNullOrEmpty(porcentaje))
                return string.Empty;

            // Eliminar cualquier símbolo % del texto
            return porcentaje.Replace("%", string.Empty).Trim();
        }

        private void ProcessAdditionalCareInstructions(InfoComposicion infoComposicion, List<CareInstruction> cares)
        {
            if (infoComposicion == null) return;

            // Procesar excepciones de composición
            if (infoComposicion.ComposicionPrenda?.Excepciones != null)
            {
                var excepciones = infoComposicion.ComposicionPrenda.Excepciones
                    .Where(excepcion => !string.IsNullOrEmpty(excepcion?.ID))
                    .Select(excepcion => new
                    {
                        ID = excepcion.ID,
                        Parsed = int.TryParse(excepcion.ID, out var id) ? (int?)id : null
                    })
                    .Where(x => x.Parsed.HasValue)
                    .Select(x => new CareInstruction 
                    { 
                        Category = "Exception", 
                        Instruction = x.Parsed.Value 
                    });

                cares.AddRange(excepciones);
            }

            // Procesar instrucciones adicionales
            if (infoComposicion.OtrasInstruccionesConservacion != null)
            {
                var adicionales = infoComposicion.OtrasInstruccionesConservacion
                    .Where(adicional => !string.IsNullOrEmpty(adicional?.ID))
                    .Select(adicional => new
                    {
                        ID = adicional.ID,
                        Parsed = int.TryParse(adicional.ID, out var id) ? (int?)id : null
                    })
                    .Where(x => x.Parsed.HasValue)
                    .Select(x => new CareInstruction 
                    { 
                        Category = "Additional", 
                        Instruction = x.Parsed.Value 
                    });

                cares.AddRange(adicionales);
            }
        }

        private List<CareInstruction> ProcessCareInstructions(List<InstruccionConservacionPDF> instrucciones, int projectid)
        {
            if (instrucciones == null || instrucciones.Count != 5) 
                return new List<CareInstruction>();



            var careInstructionMap = new Dictionary<int, string>
            {
                { 0, "Wash" },
                { 1, "Bleach" },
                { 2, "Dry" },
                { 3, "Iron" },
                { 4, "DryCleaning" }
            };

            return instrucciones
                .Select((instruccion, index) => new 
                { 
                    Code = careInstructionMap[index],
                    ID = instruccion?.ID
                  
                })
                .Where(x => !string.IsNullOrEmpty(x.ID))
                .Select(x => new CareInstruction 
                { 
                    Code = x.Code,
                    Instruction = int.TryParse(x.ID, out int id) ? id : 0,
                    Category = x.Code
                })
                .Where(x => x.Instruction != 0)
                .ToList();
        }

        private List<BaseDataCompositionJSON> GetCompositionJSON(int projectid, int orderid, Order companyOrder)
        {
           
            using(PrintDB ctx = factory.GetInstance<PrintDB>())
            {
                var baseDataCatalog = ctx.Catalogs
                                    .FirstOrDefault(c => c.Name.Equals("BaseData")
                                    && c.ProjectID.Equals(companyOrder.ProjectID));

                var OrderDataCatalog = ctx.Catalogs
                    .FirstOrDefault(c => c.Name.Equals("Orders")
                    && c.ProjectID.Equals(companyOrder.ProjectID));

                var OrderDetailsDataCatalog = ctx.Catalogs
                            .FirstOrDefault(c => c.Name.Equals("OrderDetails")
                            && c.ProjectID.Equals(companyOrder.ProjectID));

                var VariableDataCatalog = ctx.Catalogs
                                            .FirstOrDefault(c => c.Name.Equals("VariableData")
                                                && c.ProjectID.Equals(companyOrder.ProjectID));

                if (baseDataCatalog == null || OrderDataCatalog == null || 
                    OrderDetailsDataCatalog == null || VariableDataCatalog == null)
                {
                    log.LogMessage($"One or more required catalogs not found for projectid: {projectid}");
                    return null;
                }

                var relField = OrderDataCatalog.Fields.FirstOrDefault(w => w.Name == "Details");
                if (relField == null)
                {
                    log.LogMessage($"Details field not found in Orders catalog");
                    return null;
                }

                if(baseDataCatalog.Fields.FirstOrDefault(f => f.Name == "CompositionJSON") == null)
                    return null;

                using(var dynDb = connManager.OpenDB("CatalogDB"))
                {
                        
                    var variableDataItem = dynDb.Select<BaseDataCompositionJSON>(
                                $"SELECT  l.ID AS ID, l.CompositionJSON as CompositionJSON "
                            + $"FROM {OrderDataCatalog.TableName} AS o "
                            + $"INNER JOIN REL_{OrderDataCatalog.CatalogID}_{OrderDetailsDataCatalog.CatalogID}_{relField.FieldID} AS r ON r.SourceID = o.ID "
                            + $"INNER JOIN {OrderDetailsDataCatalog.TableName} AS d ON r.TargetID = d.ID "
                            + $"INNER JOIN {VariableDataCatalog.TableName} AS v ON d.Product = v.ID "
                            + $"INNER JOIN {baseDataCatalog.TableName} AS l ON v.IsBaseData = l.ID "
                            + $"WHERE o.ID = {companyOrder.OrderDataID}");
                    return variableDataItem;
                        
                }
            }
            
        }
         
        public class BaseDataCompositionJSON
        {
            public int ID { get; set; } 
            public string CompositionJSON { get; set; }
        }

    }
}
