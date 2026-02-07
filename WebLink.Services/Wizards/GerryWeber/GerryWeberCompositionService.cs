using EllipticCurve.Utils;
using LinqKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;
using WebLink.Services.Wizards.PuntoRoma;

namespace WebLink.Services.Wizards.GerryWeber
{
    public class GerryWeberCompositionService
    {
        private readonly IOrderUtilService orderUtilService;
        private readonly IPrinterJobRepository printerJobRepo;
        private readonly IArticleRepository articleRepo;
        private readonly ILabelRepository labelRepo;
        
        private readonly INotificationRepository notificationRepo;
        private readonly IDBConnectionManager connManager;
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
        //private readonly string BASEDATA_TABLENAME = "BaseData";

        string[] SectionsLanguageSortedAll = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "CATALAN", "GALICIAN", "BASQUE", "ARABIC" };
        string[] FibersLanguageSortedAll = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "CATALAN", "GALICIAN", "BASQUE", "ARABIC" };
        string[] CareInstructionsLanguageSortedAll = { "SPANISH", "English", "FRENCH", "GERMAN", "ITALIAN", "PORTUGUESE", "CATALAN", "GALICIAN", "BASQUE", "ARABIC" };



        // the names of fields has contain 2digit ISO
        // Código(ISO 3166 - 1) País
        // DE  Alemania(Deutschland)
        // GB Reino Unido(Gran Bretaña e Irlanda del Norte)
        // ES España
        // FR Francia
        // NL Países Bajos(Holanda)
        // PL Polonia
        // RU Rusia(Federación Rusa)
        // SA Arabia Saudita
        // CN China
        // BG Bulgaria
        // EE Estonia
        // LV Letonia
        // LT Lituania

        // HARCODE: manual mapping for every field for table Sections and Fibers
        private Dictionary<string,string> mappingLangs = new Dictionary<string, string>() {
                { "DE",  "GERMAN" },
                { "EN", "English" },
                { "ES", "Spanish" },
                { "FR", "FRENCH" },
                { "NL", "DUTCH" },
                { "RU", "RUSSIAN" },
                { "PL", "PORTUGUESE" },
            //{ "SA", "ARABIC" },
            //{ "CN", "NONE" },
            //{ "BG", "NONE" },
            //{ "EE", "NONE" },
            //{ "LV", "NONE" },
            //{ "LT", "NONE" },
            {"EA", "Spanish" }

            };

        

        private string langFieldSeparator = "__SEP__";

        private Dictionary<string, string> currentLangKeys = new Dictionary<string, string>();

        private GWCompoPluginConfig pluginConfig;


        public GerryWeberCompositionService(
            IOrderUtilService orderUtilService,
            IPrinterJobRepository printerJobRepo,
            IArticleRepository articleRepo,
            INotificationRepository notificationRepo,
            IDBConnectionManager connManager,
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
            this.pluginConfig = new GWCompoPluginConfig();
        }
        public void GenerateCompositionText(List<OrderDataDTO> orderData)
        {
            Dictionary<CompoCatalogName, IEnumerable<string>> languagesFull = null;
            var projectId = orderData[0].ProjectID;


            var catalogs = catalogRepo.GetByProjectID(projectId, true);
            

            ICatalog ordersCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
            ICatalog detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            ICatalog variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            ICatalog compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            ICatalog baseDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.BASEDATA_CATALOG));
            ICatalog madeInCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_MADEIN_CATALOG));
            ICatalog sectionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_SECTIONS_CATALOG));
            ICatalog fibersCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_FIBERS_CATALOG));
            ICatalog ciCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_CAREINSTRUCTIONS_CATALOG));
            ICatalog packsType = catalogs.First(f => f.Name.Equals(PACKTYPE_TABLENAME));
            
            var projectData = orderUtilService.GetProjectById(projectId);

            SetSeparators(projectData);


            var baseDataObject = GetPaisPedido(orderData[0].OrderID, catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG)), detailCatalog, variableDataCatalog, baseDataCatalog);
            var baseDataDTO = new BaseDataDTO
            {
                PAIS_PEDIDO = baseDataObject.GetValue("PAIS_PEDIDO").ToString(),
                MadeIn = baseDataObject.GetValue("MadeIn").ToString(),
                SetEtiquetas = baseDataObject.GetValue("SetEtiquetas").ToString()

            };


            UpdateLanguasSorting(baseDataObject, baseDataCatalog, sectionsCatalog, fibersCatalog, ciCatalog);



            if(string.IsNullOrEmpty(baseDataDTO.PAIS_PEDIDO))
                throw new Exception("The country of the order is null or incorrect");

            languagesFull = (Dictionary<CompoCatalogName, IEnumerable<string>>)DictionaryLanguagesFill(SectionsLanguageSortedAll, FibersLanguageSortedAll, CareInstructionsLanguageSortedAll);


            var pageCount = 0; // store the biggest composition

            foreach(var data in orderData)
            {
                var compositionsFull = orderUtilService.GetComposition(data.OrderGroupID, true, languagesFull, langFieldSeparator);
                int rowIndex = 0;
                foreach(var compo in compositionsFull)
                {
                    //var listfibersFull = CreateFiberList(compo, SECTION_LANGUAGESEPA, FIBER_LANGUAGESEPARA, baseDataDTO.PAIS_PEDIDO);

                    //var dataResult = ProcessRow(compo, listfibersFull, baseDataDTO.PAIS_PEDIDO);
                    //SaveCompo(compo, dataResult, orderDatas[0].ProjectID, baseDataDTO.PAIS_PEDIDO);


                    // create a full text with n characters by line without split words
                    var count = ProcessRowByLang(projectId,compo);

                    if(count > pageCount)
                        pageCount = count;


                }
            }

            //change article code

            UpdateArticleCode(orderData, pageCount);
        }

        private void UpdateArticleCode(List<OrderDataDTO> orderData, int pageCount)
        {
            orderData.ForEach(data =>
            {
                IPrinterJob job = printerJobRepo.GetByOrderID(data.OrderID, true).First();

                var article = articleRepo.GetByID(job.ArticleID);

                //var regexPages = new Regex(@"\d+$");
                var regexBase = new Regex(@"[^0-9]*");

                var newArticleCode = regexBase.Match(article.ArticleCode).Value + pageCount;

                var newArticle = articleRepo.GetByCodeInProject(newArticleCode, article.ProjectID.Value);

                //update printdata by articlecode
                printerJobRepo.UpdateArticle(job.ID, newArticle.ID);

                var printerDetails = printerJobRepo.GetAllJobDetails(job.ID, false); // false-> order by printerjobdetailid

                
                    var catalogs = catalogRepo.GetByProjectID(article.ProjectID.Value, true);
                    var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));

                    // bulk update
                    var allIds = printerDetails.Select(s => s.ProductDataID);

                    using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
                    {
                        dynamicDB.Conn.ExecuteNonQuery(
                        $@"UPDATE d SET                                       
                                    ArticleCode = @ArticleCode
                                FROM {detailCatalog.TableName} d
                                WHERE d.ID in  ({string.Join(',', allIds)})", newArticle.ArticleCode);
                    }
            });
            
        }

        private int ProcessRowByLang(int projectID, CompositionDefinition compo)
        {
            string symbols;
            Dictionary<string, string> fullTextByLang = JoinTextByLang(compo, out symbols);


            // text joined by lang inner dictionary fullTextByLang
            // next split in page by lines and format lines with space as necesary
            var formatedBlock = FormatText(fullTextByLang);

            var pages = SplitByPages(formatedBlock);

            var compositionData = new Dictionary<string, string>();

            for(var i = 0; i < pages.Count; i++)
            {
                compositionData[$"Page{i+1}"] = pages[i];
            }

            // void SaveComposition(int projectId, int rowId, Dictionary<string, string> composition, string careInstructions, string symbols);
            orderUtilService.SaveComposition(projectID, compo.ID, compositionData, string.Empty, symbols);

            return pages.Count;

        }

        private List<string> SplitByPages(List<string> formatedBlock)
        {
            var availableLinesByPage = pluginConfig.AvailableLinesByPage;
            var currentPage = 0;


            var pages = new List<string>();

            while(formatedBlock.Count > 0 && currentPage < availableLinesByPage.Count)
            {
                if(formatedBlock.Count >= availableLinesByPage[currentPage])
                {
                    pages.Add(string.Join(System.Environment.NewLine, formatedBlock.Take(availableLinesByPage[currentPage])));
                    //formatedBlock.RemoveRange(0, availableLinesByPage[currentPage]);
                }
                else
                {

                    //fill with blank lines to complete the lines available for page to allow horizontal text compression 
                    while(formatedBlock.Count < availableLinesByPage[currentPage])
                        formatedBlock.Add(" ");

                    pages.Add(string.Join(System.Environment.NewLine, formatedBlock));

                   
                    
                    //formatedBlock.RemoveRange(0, formatedBlock.Count);
                }

                formatedBlock.RemoveRange(0, availableLinesByPage[currentPage]);

                currentPage++;

            }

            // TODO: if formatedBlock keep elements, register error or throw an Exception
            return pages;

        }

        private Dictionary<string, string> JoinTextByLang(CompositionDefinition compo, out string symbols)
        {
            var fullTextByLang = new Dictionary<string, string>();
            symbols = string.Empty;

            //currentLangKeys.ForEach(s => fullTextByLang[s.Key] = new StringBuilder(512));

            var SectionAndFiberSeparator = " ";

            for(var currentLang = 0; currentLang < currentLangKeys.Keys.Count; currentLang++)
            {
                var langKey = currentLangKeys.Keys.ElementAt(currentLang);
                var sb = new StringBuilder(256);

                for(var currSection = 0; currSection < compo.Sections.Count; currSection++)
                {
                    //sb.Append(langKey);



                    var section = compo.Sections[currSection];
                    var sectionText = section.AllLangs.Split(langFieldSeparator)[currentLang];
                    sb.Append(sectionText);
                    sb.Append(SectionAndFiberSeparator);


                    for(var currFiber = 0; currFiber < section.Fibers.Count; currFiber++)
                    {
                        var fiber = section.Fibers[currFiber];
                        var fiberText = fiber.AllLangs.Split(langFieldSeparator)[currentLang];
                        sb.Append($"{fiber.Percentage}% {fiberText}");

                        // if last
                        if(currFiber == section.Fibers.Count - 1)
                            sb.Append(SECTION_SEPARATOR);// the next text will be CareInstructions
                        else
                            sb.Append(FIBERS_SEPARATOR);
                    }

                }

                symbols = string.Empty;// this value will be filled multiple times, all times will be the same value
                for(var currCi = 0; currCi < compo.CareInstructions.Count; currCi++)
                {
                    var ci = compo.CareInstructions[currCi];
                    var ciText = ci.AllLangs.Split(langFieldSeparator)[currentLang];

                    sb.Append(ciText);
                    
                    // if last
                    if(currCi != compo.CareInstructions.Count - 1)
                        sb.Append(CI_SEPARATOR);


                    symbols += ci.Symbol;
                }

                fullTextByLang[langKey] = sb.ToString();
            }

            return fullTextByLang;
        }



        /*
         LANG KEY FIRST LINE TEXT
             SECOND LINE TEXT
             ......
        LANG KEY FIRST LINE TEXT
             SECOND LINE TEXT
             ......
        LANG KEY FIRST LINE TEXT
             SECOND LINE TEXT
             ......
         */
        private List<string> FormatText(Dictionary<string, string> fullTextByLang)
        {
            //var formatedText = new StringBuilder(1024);
            var lineLen = pluginConfig.CharactersByLine;
            var maxLineLen = (int)Math.Floor(lineLen * (1.0 + pluginConfig.Compresion/100.0 ));
            var margin = "    ";

            var block = new List<string>();
            var currentLine = new StringBuilder(256);
            var lineNumberInblock = 0;

            fullTextByLang.ForEach(keyPair =>
            {
                // first line
                currentLine.Clear();
                currentLine.Append(keyPair.Key);
                
                // 2 blocks, fibers and care instructions
                var fibersAndCI = fullTextByLang[keyPair.Key].Split(SECTION_SEPARATOR);

                fibersAndCI.ForEach(fiber => {
                    var langText = fiber.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var pendingToProcdcess = langText.Length;

                    langText.ForEach(word =>
                    {
                        if(currentLine.Length + word.Length < maxLineLen)
                        {
                            if (currentLine.Length > 0 && currentLine[currentLine.Length - 1] != ' ')
                                currentLine.Append(" ");
                            currentLine.Append(word);
                        }
                        else
                        {
                            block.Add(currentLine.ToString());
                            lineNumberInblock++;
                            currentLine.Clear();
                            currentLine.Append(margin);
                            currentLine.Append(word);
                        }
                    });

                    block.Add(currentLine.ToString());
                    lineNumberInblock++;
                    currentLine.Clear();
                    currentLine.Append(margin);

                });
            });


            var allText = string.Join("\n", block);
            return block;
        }

        private void UpdateLanguasSorting(JObject baseData, ICatalog baseDataCatalog, ICatalog sectionsCatalog, ICatalog fibersCatalog, ICatalog ciCatalog)
        {

            /*
             Lang sort come from file - looking field that begining with "IdiomaN"  where N is a number with 1 or 2 digits
             */
            var regex = new Regex(@"\d+$");
            var idiomaFields = baseDataCatalog.Fields.Where(x => x.Name.StartsWith("Idioma"))
                                                    .Select(f => new { f.FieldID, f.Name, SortField = int.Parse(regex.Match(f.Name).Value) })
                                                    .OrderBy(o => o.SortField)
                                                    .ToList();



            

            var requestedLangs = new Dictionary<string, string>();

            idiomaFields.ForEach(field =>
            {

                var definedLang = baseData.GetValue(field.Name).ToString();
                
                if(mappingLangs.Keys.Contains(definedLang))
                {

                    var validLang = mappingLangs[definedLang];

                    requestedLangs.Add(definedLang, validLang);

                }

            });

            var requestedLangForSections = new List<string>();
            var requestedLangForFibers = new List<string>();
            var requestedLangForCi = new List<string>();

            requestedLangs.ForEach(key => {

                var sectionFiled = sectionsCatalog.Fields.FirstOrDefault(f => f.Name == key.Value);
                var fiberField = fibersCatalog.Fields.FirstOrDefault(f => f.Name == key.Value);
                var ciField = ciCatalog.Fields.FirstOrDefault(f => f.Name == key.Value);

                if(sectionFiled != null)
                    requestedLangForSections.Add(sectionFiled.Name);

                if(fiberField != null)
                    requestedLangForFibers.Add(fiberField.Name);
                

                if(ciField != null)
                    requestedLangForCi.Add(ciField.Name);
               



            });

            var commonLangs = requestedLangForSections
                .Intersect(requestedLangForFibers)
                .Intersect(requestedLangForCi);


            // dictionary with current langs requested
            commonLangs.ForEach(langFieldName => {

                var keyFound = mappingLangs.FirstOrDefault(ml => ml.Value == langFieldName).Key; // value always be found, because already validated in above code

                currentLangKeys.Add(keyFound, langFieldName);


            });

            


            // at this point the lags for sections and fibers must be the same, is some lang is not available, write 'XXXX'
            SectionsLanguageSortedAll = commonLangs.ToArray();

            FibersLanguageSortedAll = commonLangs.ToArray();

            CareInstructionsLanguageSortedAll = commonLangs.ToArray();




        }

        private JObject GetPaisPedido(int orderID, ICatalog ordersCatalog, ICatalog detailCatalog, ICatalog variableDataCatalog, ICatalog baseDataCatalog)
        {

            var relField = JsonConvert.DeserializeObject<List<FieldDefinition>>(ordersCatalog.Definition).First(w => w.Name == "Details");

            var order = orderRepo.GetByID(orderID);

            using(var dynamicDb = connManager.CreateDynamicDB())
            {
                //bd.PAIS_PEDIDO, bd.MadeIn, bd.SetEtiquetas
                return dynamicDb.Select(ordersCatalog.CatalogID,
                                        $@"SELECT bd.*
                                        FROM #TABLE o
                                        INNER JOIN [dbo].[REL_{ordersCatalog.CatalogID}_{detailCatalog.CatalogID}_{relField.FieldID}] rel ON o.ID = rel.SourceID
                                        INNER JOIN {detailCatalog.TableName} d ON rel.TargetID = d.ID
                                        INNER JOIN {variableDataCatalog.TableName} v ON d.Product = v.ID
                                        INNER JOIN {baseDataCatalog.TableName} bd ON bd.ID = v.IsBaseData
                                        WHERE o.ID = {order.OrderDataID}").First() as JObject;

            }
        }

        

        private void SaveCompo(CompositionDefinition compo, RowDataResult dataResult, int projectID, string countryOrder)
        {
            var compositionData = new Dictionary<string, string>();
            string careInstructionsSymbolAll = string.Empty;
            compositionData["FullComposition"] = dataResult.CompositionDataResult.Full;
            compositionData["FullCareInstructions"] = dataResult.CaresDataResult.Full;

            dataResult.CareSymbols.ForEach(x => { careInstructionsSymbolAll += x; });
            compositionData["Symbols"] = careInstructionsSymbolAll;

            orderUtilService.SaveComposition(projectID, compo.ID, compositionData, dataResult.CaresDataResult.Full, careInstructionsSymbolAll);
        }

        private RowDataResult ProcessRow(CompositionDefinition compo, List<FiberDTO> listfibers, string orderCountry)
        {
            var materialsAll = string.Empty;
            materialsAll = SplitComposition(listfibers);

            var careInstructionsAll = SplitCareInstructions(compo.CareInstructions);

            RowDataResult dataResult = new RowDataResult();
            dataResult = GetCareSymblos(dataResult, compo);
            dataResult.CompositionDataResult.Full = materialsAll.Replace("¬", SECTION_SEPARATOR);
            dataResult.CaresDataResult.Full = careInstructionsAll;

            return dataResult;
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

        private List<FiberDTO> CreateFiberList(CompositionDefinition compo, string sectionLanguageSeparator, string fiberLanguageSeparator, string orderCountry)
        {
            List<FiberDTO> list = new List<FiberDTO>();
            List<Section> sections = new List<Section>();

            sections = compo.Sections.OrderBy(c => c.Code).ToList();

            for(var i = 0; i < sections.Count; i++)
            {
                var langsListSection = sections[i].AllLangs.ToUpper().Split(',', StringSplitOptions.RemoveEmptyEntries);

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

                for(var f = 0; f < sections[i].Fibers.Count; f++)
                       list = AddFibers(fiberLanguageSeparator, orderCountry, list, sections, i, f);

            }
            return list.ToList();
        }

        private List<FiberDTO> AddFibers(string fiberLanguageSeparator, string orderCountry, List<FiberDTO> list, List<Section> sections, int section, int fiber)
        {
            var langsListFiber = sections[section].Fibers[fiber].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var fiberValue = langsListFiber.Length > 1 ? string.Join(",", sections[section].Fibers[fiber].AllLangs.Split(",").Where(x => x != string.Empty).ToArray()).Replace(",", $" {fiberLanguageSeparator} ") : langsListFiber[0];

            list.Add(GetFiber(sections, section, fiber, fiberValue, orderCountry));

            return list;
        }

        private static FiberDTO GetFiber(List<Section> sections, int section, int fiber, string fiberValue, string orderCountry)
        {
            var percentage = sections[section].Fibers[fiber].Percentage + "%";

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

        public void SetConfig(List<int> availableLinesByPage, int charactersByLine, int compresion, string langsCodeMappingColumns)
        {
            pluginConfig.AvailableLinesByPage = availableLinesByPage;
            pluginConfig.CharactersByLine = charactersByLine;
            pluginConfig.Compresion = compresion;

            if(string.IsNullOrEmpty(langsCodeMappingColumns)) return;

            mappingLangs.Clear();

            mappingLangs = JsonConvert.DeserializeObject<Dictionary<string, string>>(langsCodeMappingColumns);
        }
    }

    public class GWCompoPluginConfig
    {
        public List<int> AvailableLinesByPage;
        public int CharactersByLine;
        public int Compresion;// 1,2,3,4,5  Percentage value of compresion
    }
}

