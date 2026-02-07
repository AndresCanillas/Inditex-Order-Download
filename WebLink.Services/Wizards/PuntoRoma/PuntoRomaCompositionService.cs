using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Wizards.PuntoRoma
{
    public class PuntoRomaCompositionService
    {
        private readonly IOrderUtilService orderUtilService;
        private readonly IPrinterJobRepository printerJobRepo;
        private readonly IArticleRepository articleRepo;
        private readonly ILabelRepository labelRepo;
        
        private readonly INotificationRepository notificationRepo;
        private readonly IConnectionManager connManager;
        private readonly ICatalogRepository catalogRepo;
        private readonly IOrderRepository orderRepo;

        private string SECTION_SEPARATOR;
        private string SECTION_LANGUAGESEPA;
        private string FIBERS_SEPARATOR;
        private string FIBER_LANGUAGESEPARA;
        private string CI_SEPARATOR;
        private string CI_LANGUAGESEPARATOR;
        private int ENGLISHLANG_POSITION;
        private readonly string PACKTYPE_TABLENAME = "PacksType";
        private readonly string BASEDATA_TABLENAME = "BaseData";


        string[] SectionsLanguageSortedAllIsrael = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "DUTCH", "CATALAN", "GALICIAN", "BASQUE", "HEBREW" };
        string[] FibersLanguageSortedAllIsrael = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "DUTCH", "CATALAN", "GALICIAN", "BASQUE", "HEBREW" };
        string[] CareInstructionsLanguageSortedAllIsrael = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "DUTCH", "CATALAN", "GALICIAN", "BASQUE", "HEBREW" };

        string[] SectionsLanguageSortedAll = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "DUTCH", "CATALAN", "GALICIAN", "BASQUE", "ARABIC" };
        string[] FibersLanguageSortedAll = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "DUTCH", "CATALAN", "GALICIAN", "BASQUE", "ARABIC" };
        string[] CareInstructionsLanguageSortedAll = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN",  "PORTUGUESE", "DUTCH", "CATALAN", "GALICIAN", "BASQUE", "ARABIC" };

        public PuntoRomaCompositionService(
            IOrderUtilService orderUtilService,
            IPrinterJobRepository printerJobRepo,
            IArticleRepository articleRepo,
            INotificationRepository notificationRepo,
            IConnectionManager connManager,
            ILabelRepository labelRepo,
            IOrderRepository orderRepo,
            ICatalogRepository catalogRepo)
        {
            this.orderUtilService = orderUtilService;
            this.printerJobRepo = printerJobRepo;
            this.articleRepo = articleRepo;
            this.labelRepo = labelRepo;
            this.notificationRepo = notificationRepo;
            this.connManager = connManager;
            this.orderRepo = orderRepo;
            this.catalogRepo = catalogRepo;
        }
        public void GenerateCompositionText(List<OrderDataDTO> orderDatas)
        {
            Dictionary<CompoCatalogName, IEnumerable<string>> languagesFull = null;
            ICatalog ordersCatalog, detailCatalog, variableDataCatalog, compositionLabelCatalog, baseDataCatalog, madeInCatalog, packsType;

            var projectData = orderUtilService.GetProjectById(orderDatas[0].ProjectID);
            SetSeparators(projectData);
            GetCatalogs(orderDatas[0].ProjectID, out ordersCatalog, out detailCatalog, out variableDataCatalog, out compositionLabelCatalog, out baseDataCatalog, out madeInCatalog, out packsType);

            var baseDataDTO = GetPaisPedido(orderDatas[0].OrderID, ordersCatalog, detailCatalog, variableDataCatalog, baseDataCatalog);
            var packType = GetPackType(packsType, baseDataDTO.SetEtiquetas);

            if(string.IsNullOrEmpty(baseDataDTO.PAIS_PEDIDO))
                throw new Exception("The country of the order is null or incorrect");

            if(baseDataDTO.PAIS_PEDIDO == "HEB")
                languagesFull = (Dictionary<CompoCatalogName, IEnumerable<string>>)DictionaryLanguagesFill(SectionsLanguageSortedAllIsrael, FibersLanguageSortedAllIsrael, CareInstructionsLanguageSortedAllIsrael);
            else
                languagesFull = (Dictionary<CompoCatalogName, IEnumerable<string>>)DictionaryLanguagesFill(SectionsLanguageSortedAll, FibersLanguageSortedAll, CareInstructionsLanguageSortedAll);

            foreach(var orderData in orderDatas)
            {
                var compositionsFull = orderUtilService.GetComposition(orderData.OrderGroupID, true, languagesFull);
                int rowIndex = 0;
                foreach(var compo in compositionsFull)
                {
                    var listfibersFull = CreateFiberList(compo, SECTION_LANGUAGESEPA, FIBER_LANGUAGESEPARA, baseDataDTO.PAIS_PEDIDO, packType.Type);

                    var dataResult = ProcessRow(compo, listfibersFull, baseDataDTO.PAIS_PEDIDO, packType.Type);
                    SaveCompo(compo, dataResult, orderDatas[0].ProjectID, baseDataDTO.PAIS_PEDIDO);
                }
            }
        }

        private PackTypeDTO GetPackType(ICatalog packsType, string setEtiquetas)
        {
            using(var dynamicDb = connManager.OpenDB("CatalogDB"))
            {

                var packType = dynamicDb.SelectOne<PackTypeDTO>(
                                        $@"SELECT CODE,Type,TypeOfLabel
                                        FROM {packsType.TableName} p
                                        WHERE p.CODE = '{setEtiquetas}'");

                if(packType == null)
                    throw new Exception($"Packtype cannot be null.\n This label pack does not exist: {setEtiquetas} in PacksType Table");
                return packType;
            }

        }

        private BaseDataDTO GetPaisPedido(int orderID, ICatalog ordersCatalog, ICatalog detailCatalog, ICatalog variableDataCatalog, ICatalog baseDataCatalog)
        {

            var relField = JsonConvert.DeserializeObject<List<FieldDefinition>>(ordersCatalog.Definition).First(w => w.Name == "Details");

            var order = orderRepo.GetByID(orderID);

            using(var dynamicDb = connManager.OpenDB("CatalogDB"))
            {

                return dynamicDb.SelectOne<BaseDataDTO>(
                                        $@"SELECT bd.PAIS_PEDIDO, bd.MadeIn, bd.SetEtiquetas
                                        FROM {ordersCatalog.TableName} o
                                        INNER JOIN [dbo].[REL_{ordersCatalog.CatalogID}_{detailCatalog.CatalogID}_{relField.FieldID}] rel ON o.ID = rel.SourceID
                                        INNER JOIN {detailCatalog.TableName} d ON rel.TargetID = d.ID
                                        INNER JOIN {variableDataCatalog.TableName} v ON d.Product = v.ID
                                        INNER JOIN {baseDataCatalog.TableName} bd ON bd.ID = v.IsBaseData
                                        WHERE o.ID = {order.OrderDataID}");

            }
        }

        private void GetCatalogs(int projectId, out ICatalog ordersCatalog, out ICatalog detailCatalog, out ICatalog variableDataCatalog, out ICatalog compositionLabelCatalog, out ICatalog baseDataCatalog, out ICatalog madeInCatalog, out ICatalog packsTypeCatalog)
        {
            var catalogs = catalogRepo.GetByProjectID(projectId, true);
            ordersCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
            detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            baseDataCatalog = catalogs.First(f => f.Name.Equals(BASEDATA_TABLENAME));
            madeInCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_MADEIN_CATALOG));
            packsTypeCatalog = catalogs.First(f => f.Name.Equals(PACKTYPE_TABLENAME));
        }

        private void SaveCompo(CompositionDefinition compo, RowDataResult dataResult, int projectID, string countryOrder)
        {
            var compositionData = new Dictionary<string, string>();
            string careInstructionsSymbolAll = string.Empty;
            compositionData["FullComposition"] = dataResult.CompositionDataResult.Full;
            compositionData["FullCareInstructions"] = dataResult.CaresDataResult.Full;

            if(countryOrder == "MEX")
                compositionData["Type"] = "3";

            dataResult.CareSymbols.ForEach(x => { careInstructionsSymbolAll += x; });
            compositionData["Symbols"] = careInstructionsSymbolAll;

            orderUtilService.SaveComposition(projectID, compo.ID, compositionData, dataResult.CaresDataResult.Full, careInstructionsSymbolAll);


        }

        private RowDataResult ProcessRow(CompositionDefinition compo, List<FiberDTO> listfibers, string orderCountry, string packType)
        {
            var materialsAll = string.Empty;
            if(orderCountry != "MEX")
                materialsAll = SplitComposition(listfibers);
            else
                materialsAll = SplitCompositionMexico(listfibers, packType);
            var careInstructionsAll = SplitCareInstructions(compo.CareInstructions);

            RowDataResult dataResult = new RowDataResult();
            dataResult = GetCareSymblos(dataResult, compo);
            dataResult.CompositionDataResult.Full = materialsAll.Replace("¬", SECTION_SEPARATOR);
            dataResult.CaresDataResult.Full = careInstructionsAll;

            return dataResult;
        }

        private string SplitCompositionMexico(List<FiberDTO> list, string packType)
        {
            string concatFibers = string.Empty;

            if(packType != "BI")
            {
                for(var i = 0; i < list.Count; i++)
                    concatFibers += string.Format("{0}{1}{2}", i % 2 != 0 ? string.Concat(list[i].Percent, "") : "¬", list[i].Fiber, FIBERS_SEPARATOR);
            }
            else
            {
                for(var i = 0; i < list.Count; i++)
                    concatFibers += string.Format("{0}{1}{2}", list[i].Percent != string.Empty ? string.Concat(list[i].Percent, " ") : "¬", list[i].Fiber, FIBERS_SEPARATOR);
            }

            return concatFibers;
        }

        private string SplitComposition(List<FiberDTO> lst)
        {
            string concatFibers = string.Empty;

            for(var i = 0; i < lst.Count; i++)
                concatFibers += string.Format("{0}{1}{2}", lst[i].Percent != string.Empty ? string.Concat(lst[i].Percent, " ") : "¬", lst[i].Fiber, FIBERS_SEPARATOR);
            return concatFibers;
        }

        private string SplitCareInstructions(IList<CareInstruction> lst)
        {
            string concatFibers = string.Empty;


            string[] langs = new string[CareInstructionsLanguageSortedAll.Count()];

            for(var i = 0; i < lst.Count; i++)
            {
                var langsListFiber = lst[i].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                for(var j = 0; j < langsListFiber.Count(); j++)
                    langs[j] += langsListFiber[j];

            }
            for(var i = 0; i < langs.Count(); i++)
            {
                if(!string.IsNullOrEmpty(langs[i]))
                    concatFibers += langs.Length > 1 ? string.Concat(langs[i], CI_SEPARATOR) : langs[0];
            }
            return string.Concat(concatFibers.Substring(0, concatFibers.Length - 2), ".");
        }

        private RowDataResult GetCareSymblos(RowDataResult dataResult, CompositionDefinition compo)
        {
            for(var index = 0; index < 5; index++)
                dataResult.CareSymbols.Add(compo.CareInstructions[index].Symbol);

            return dataResult;
        }

        private List<FiberDTO> CreateFiberList(CompositionDefinition compo, string sectionLanguageSeparator, string fiberLanguageSeparator, string orderCountry, string packType)
        {
            List<FiberDTO> list = new List<FiberDTO>();
            List<Section> sections = new List<Section>();

            if(orderCountry == "MEX" && packType == "ZA")
                sections = compo.Sections.OrderBy(c => c.ID).ToList();
            else
                sections = compo.Sections.OrderBy(c => c.Code).ToList();

            for(var i = 0; i < sections.Count; i++)
            {
                var langsListSection = sections[i].AllLangs.ToUpper().Split(',', StringSplitOptions.RemoveEmptyEntries);

                if(orderCountry == "MEX" && langsListSection[0] == "EMPEINE")
                {
                    langsListSection[0] = "CORTE";
                    sections[i].AllLangs = string.Join(",", langsListSection);
                }


                var sectionValue = langsListSection.Length > 0 ? langsListSection.Length > 1 ? string.Join(",", sections[i].AllLangs.ToUpper().Split(",").Where(x => x != string.Empty).ToArray()).Replace(",", $" {sectionLanguageSeparator} ") : langsListSection[0] : string.Empty;

                //if composition have one section
                if(compo.Sections.Count > 1)
                {
                    list.Add(new FiberDTO
                    {
                        Percent = string.Empty,
                        Fiber = sectionValue,
                        FiberType = string.Empty
                    });
                }

                if(orderCountry == "MEX" && packType == "ZA")
                    list = AddFibers(fiberLanguageSeparator, orderCountry, list, sections, i, 0, packType);
                else
                {
                    for(var f = 0; f < sections[i].Fibers.Count; f++)
                        list = AddFibers(fiberLanguageSeparator, orderCountry, list, sections, i, f, packType);

                }


            }
            return list.ToList();
        }

        private List<FiberDTO> AddFibers(string fiberLanguageSeparator, string orderCountry, List<FiberDTO> list, List<Section> sections, int section, int fiber, string packtype)
        {
            var langsListFiber = sections[section].Fibers[fiber].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var fiberValue = langsListFiber.Length > 1 ? string.Join(",", sections[section].Fibers[fiber].AllLangs.Split(",").Where(x => x != string.Empty).ToArray()).Replace(",", $" {fiberLanguageSeparator} ") : langsListFiber[0];

            list.Add(GetFiber(sections, section, fiber, fiberValue, orderCountry, packtype));

            return list;
        }

        private static FiberDTO GetFiber(List<Section> sections, int section, int fiber, string fiberValue, string orderCountry, string packtype)
        {
            //var percentage = (orderCountry == "MEX" && packtype == "ZA")
            //     ? string.Empty
            //     : sections[section].Fibers[fiber].Percentage + "%";


            var percentage = sections[section].Fibers[fiber].Percentage + "%";

            if(orderCountry == "MEX" && packtype == "ZA")
                percentage = string.Empty;

            return new FiberDTO
            {
                Percent = percentage,
                Fiber = fiberValue,
                FiberType = sections[section].Fibers[fiber].FiberType
            };
        }

        private IDictionary<CompoCatalogName, IEnumerable<string>> DictionaryLanguagesFill(string[] sectionsLanguageSortedAll, string[] fibersLanguageSortedAll, string[] careInstructionsLanguageSortedAll)
        {
            IDictionary<CompoCatalogName, IEnumerable<string>> languagesFull = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            languagesFull.Add(CompoCatalogName.SECTIONS, sectionsLanguageSortedAll);
            languagesFull.Add(CompoCatalogName.FIBERS, fibersLanguageSortedAll);
            languagesFull.Add(CompoCatalogName.CAREINSTRUCTIONS, careInstructionsLanguageSortedAll);
            return languagesFull;

        }
        private void SetSeparators(IProject projectData)
        {
            SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            SECTION_LANGUAGESEPA = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            FIBERS_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            FIBER_LANGUAGESEPARA = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            CI_LANGUAGESEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "•" : projectData.CILanguageSeparator;
            ENGLISHLANG_POSITION = 0;
        }

        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets) { }

        public void Dispose() { }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return null;
        }

        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data) { }
    }
}

