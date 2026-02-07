using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using SmartdotsPlugins.Inditex.Models;
using SmartdotsPlugins.Inditex.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class CompositionRepositoryBase
    {
        private IOrderUtilService orderUtilService;
        private Separators Separators;
        private SeparatorsInitBase SeparatorsInitializator;
        private ArticleCodeExtractorBase artcleCodeExtractor;
        private Dictionary<CompoCatalogName, IEnumerable<string>> Language;
        private InditexLanguageDictionaryManagerBase languageDictionaryManager;
        private IPrinterJobRepository printerJobRepo;
        private SymbolsBuilderBase symbolsBuilder;
        private AdditionalsCompoManagerBase additionalsCompoManager;
        private ArticleCalculatorBase articleCalculator;
        private ILogService log;
        private IArticleRepository articleRepo;
        private IFactory factory;
        private ICatalogRepository catalogRepo;
        private IDBConnectionManager connManager;
        private ArticleCompositionConfigurationBase articleCompositionConfiguration;

        public virtual void SetCustomServices(SeparatorsInitBase separatorsInitializator,
                                            ArticleCodeExtractorBase artcleCodeExtractor,
                                            InditexLanguageDictionaryManagerBase languageDictionaryManager,
                                            SymbolsBuilderBase symbolsBuilder,
                                            AdditionalsCompoManagerBase additionalsCompoManager,
                                            ArticleCalculatorBase articleCalculator,
                                            ArticleCompositionConfigurationBase articleCompositionConfiguration)
        {
            SeparatorsInitializator = separatorsInitializator;
            this.artcleCodeExtractor = artcleCodeExtractor;
            this.languageDictionaryManager = languageDictionaryManager;
            this.symbolsBuilder = symbolsBuilder;
            this.additionalsCompoManager = additionalsCompoManager;
            this.articleCalculator = articleCalculator;
            this.articleCompositionConfiguration = articleCompositionConfiguration;
        }

        protected CompositionRepositoryBase(IOrderUtilService orderUtilService,
                                            IPrinterJobRepository printerJobRepo,
                                            ILogService log,
                                            IArticleRepository articleRepo,
                                            IFactory factory,
                                            ICatalogRepository catalogRepo,
                                            IDBConnectionManager connManager
                                            )
        {
            this.orderUtilService = orderUtilService;
            this.printerJobRepo = printerJobRepo;
            this.log = log;
            this.articleRepo = articleRepo;
            this.factory = factory;
            this.catalogRepo = catalogRepo;
            this.connManager = connManager;

        }

        public void AdditionalByPage(CompositionDefinition compo, StringBuilder careInstructions, StringBuilder additionals, StringBuilder Symbols, Dictionary<string, string> compositionData, RibbonFace materialsFaces, int linenumber, Separators separators)
        {
            var symbols = new List<string>();
            var addExc = new List<string>(); // additionals and exceptions
            var basic = new List<string>();

            var addExcTable = new Dictionary<int, List<string>>();

            //foreach (var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
            foreach(var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(separators.CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if(ci.Category != "Additional" && ci.Category != "Exception")
                {
                    // symbols.Add(ci.Symbol.Trim());
                    basic.Add(translated.Trim());

                }
                else
                {
                    if(ci.Category == "Additional")
                    {
                        addExc.Add(translated);
                        addExcTable[ci.ID] = langsList;
                    }
                }

            }

            // split by page additionals and exceptions
            List<List<string>> filledPages;
            var strategy = 1;
            if(!FillCareInstructionsPagesOneByOne(addExc, materialsFaces, linenumber, out filledPages, separators))
            {
                if(!FillCareInstructionsPagesByTranslations(addExcTable, materialsFaces, linenumber, out filledPages, separators))
                {
                    throw new Exception("ZaraCompoPlugin ERROR - Can't set Additional text");
                }

                strategy = 2;
            }


            // set where page text will be saved
            for(int j = 0; j < filledPages.Count; j++)
            {
                string f = string.Empty;

                if(strategy == 1)
                    f = string.Join(Environment.NewLine, filledPages[j]);
                else
                    f = string.Join(String.Empty, filledPages[j]);

                var add = "AdditionalPage" + (j + 1);
                compositionData.Add(add, f);
            }



            // the keys of the composition data is a hardcode definition for CompositionLabel table for the current project

            careInstructions.Append(string.Join(separators.CI_SEPARATOR, basic));

            //Symbols.Append(string.Join(",", symbols)); // TODO: save symbol separator inner configuration
            //compositionData.Add("Symbols", Symbols.ToString());
            compositionData.Add("FullCareInstructions", string.Join(separators.CI_SEPARATOR, basic));

            compositionData.Add("FullAdditionals", Regex.Replace(additionals.ToString().Trim(), separators.CI_SEPARATOR + "$", string.Empty));
            compositionData.Add("AdditionalsNumber", filledPages.Count.ToString());


        }


        private bool FillCareInstructionsPagesOneByOne(IList<string> joinedTranslations, RibbonFace materialsFaces, int allowedLines, out List<List<string>> filledPages, Separators separator)
        {
            filledPages = new List<List<string>>();
            var currentPage = new List<string>();
            var fit = true;

            filledPages.Add(currentPage);

            for(int pos = 0; pos < joinedTranslations.Count; pos++)
            {
                var text = joinedTranslations[pos];
                var pageText = string.Join(separator.CI_SEPARATOR, currentPage.Append(text));

                FitObj obj = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                if(obj.Fit == false)
                {
                    // check if make fit inner blank page the current text
                    if(currentPage.Count > 0 && ContentFitsByLines(materialsFaces.Font, materialsFaces, text, allowedLines).Fit == true)
                    {
                        currentPage = new List<string>(); // new page
                        filledPages.Add(currentPage);
                    }
                    else
                    {
                        // no fit inner blank page
                        fit = false;
                        break;
                    }
                }

                currentPage.Add(text);

            }


            if(currentPage.Count < 1 && filledPages.Count > 0)
                filledPages.RemoveAt(filledPages.Count - 1);

            //if (!fit)
            //    filledPages = null;

            return fit;
        }


        public FitObj ContentFitsByLines(Font font, RibbonFace materials, string text, int linenumber)
        {
            StringFormat sfFmt = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
            Graphics g = Graphics.FromImage(new Bitmap(100, 100));

            var iHeight = (decimal)g.MeasureString(text, font, new SizeF(materials.WidthInPixels, materials.HeightInPixels), sfFmt, out var chracterFitted, out int linesFilled).Height;

            var iOneLineHeight = (decimal)g.MeasureString("Z", font, (int)Math.Ceiling(materials.WidthInPixels), sfFmt).Height;

            int iNumLines = (int)Math.Ceiling(((decimal)iHeight / iOneLineHeight));

            return new FitObj { Fit = iNumLines <= linenumber, Lines = iNumLines };
        }

        private bool FillCareInstructionsPagesByTranslations(Dictionary<int, List<string>> ciText, RibbonFace materialsFaces, int allowedLines, out List<List<string>> filledPages, Separators separator)
        {
            filledPages = new List<List<string>>();
            var currentPage = new List<string>();
            var fit = true;

            filledPages.Add(currentPage);

            foreach(var ciAllLangs in ciText.Values)
            {

                foreach(var text in ciAllLangs)
                {
                    var testingText = currentPage.Count > 0 ? separator.CI_LANG_SEPARATOR + text : text;
                    var pageText = string.Join("", currentPage.Append(testingText));

                    FitObj obj = ContentFitsByLines(materialsFaces.Font, materialsFaces, pageText, allowedLines);

                    var isLastASeparator = currentPage.Count < 1 ? false : currentPage.Last() == separator.CI_SEPARATOR || currentPage.Last() == separator.CI_LANG_SEPARATOR;

                    if(obj.Fit == false)
                    {
                        // check if make fit inner blank page the current text
                        if(currentPage.Count > 0 && ContentFitsByLines(materialsFaces.Font, materialsFaces, text, allowedLines).Fit == true)
                        {
                            if(isLastASeparator)
                                currentPage.RemoveAt(currentPage.Count - 1);

                            currentPage = new List<string>(); // new page
                            filledPages.Add(currentPage);
                        }
                        else
                        {
                            // no fit inner blank page
                            fit = false;
                            break;
                        }
                    }

                    if(currentPage.Count > 0 && isLastASeparator == false)
                        currentPage.Add(separator.CI_LANG_SEPARATOR);
                    currentPage.Add(text);
                }

                currentPage.Add(separator.CI_SEPARATOR);

            }

            if(currentPage.Count < 1 && filledPages.Count > 0)
                filledPages.RemoveAt(filledPages.Count);

            return fit;
        }


        public virtual void Save(OrderPluginData od,
                                PluginCompoPreviewInputData data)
        {

            string[] compoArray = data.compoArray;
            string[] percentArray = data.percentArray;
            string[] leatherArray = data.leatherArray;
            string[] additionalArray = data.additionalArray;
            string[] justifyCompo = data.JustifyCompo;
            string[] justifyAdditional = data.JustifyAdditional;
            int labelLines = data.labelLines;
            int ID = data.ID;

            CompositionDefinition compo = new CompositionDefinition();
            Dictionary<string, string> compositionData = new Dictionary<string, string>();
            var careInstructions = new StringBuilder();
            var Symbols = new StringBuilder();
            var additionals = new StringBuilder();
            int totalPages = 10;
            int lineNumber = 0;
            int indexCompo = 0;
            int page1_totallines = 0;

            var projectData = orderUtilService.GetProjectById(od.ProjectID);

            Separators = SeparatorsInitializator.Init(projectData);
            string artCode = artcleCodeExtractor.Extract(od.OrderID);
            Language = languageDictionaryManager.GetInditexLanguageDictionary();

            var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, Language, OrderUtilService.LANG_SEPARATOR);

            compo = compositions.FirstOrDefault(c => c.ID == ID);

            if(compo == null)
            {
                return;
            }

            int index, acumLabels;
            GetCompositionDataLeatherLines(leatherArray, labelLines, compositionData, out index, out acumLabels);
            GetCompositionDataCompoLines(compoArray, labelLines, compositionData, out index, out acumLabels);
            GetCompositionDataPercentLines(percentArray, labelLines, compositionData, out index, out acumLabels);
            GetFullCompositionLines(justifyCompo, labelLines, compositionData);
            GetFullAdditionalsLines(justifyAdditional, labelLines, compositionData);
            List<OrderPluginData> orderData = new List<OrderPluginData> { od };
            compositionData.Add("AdditionalCompress", data.AdditionalsCompress.ToString());
            compositionData.Add("FiberCompress", data.FiberCompress.ToString());
            compositionData.Add("ExceptionsLocation", od.ExceptionsLocation.ToString());
            
            if (od.FiberConcatenation != null && !string.IsNullOrEmpty(od.FiberConcatenation.FiberID))
            {
                compositionData.Add("FiberIDConcatenedToException", od.FiberConcatenation.FiberID);
            }

            if((od.ExceptionsComposition != null && od.ExceptionsComposition.Count > 0) && od.UsesFreeExceptionComposition)
            {
                var exceptionCompostion = JsonConvert.SerializeObject(od.ExceptionsComposition);
                compositionData.Add("ExceptionsComposition", exceptionCompostion);
                compositionData.Add("UsesFreeExceptionComposition", od.UsesFreeExceptionComposition.ToString());
                
            }





            var labelsCount = acumLabels - 1 == 0 ? 1 : acumLabels - 1;
            IEnumerable<IPrinterJob> printer_job = printerJobRepo.GetByOrderID(od.OrderID, true);
            compositionData.Add("ComposNumber", (acumLabels - 1).ToString());

            // Clear Additional Pages
            additionalsCompoManager.ClearAdditionalPages(od.ProjectID, ID, totalPages);
            var articleConfig = articleCompositionConfiguration.Retrieve(artCode, od.ProjectID);
            symbolsBuilder.Build(compo, Symbols, compositionData, Separators);
            if(additionalArray.Count() > 0)
            {

                GetAdditionalTextLines(additionalArray, labelLines, compositionData, out index, out acumLabels);
                GetCareInstructions(compo, careInstructions, compositionData, labelLines);
            }
            else
            {
                var font = new Font("Arial Unicode MS", 6, FontStyle.Regular);
                var materialsFaces = new RibbonFace(font, articleConfig.WidthInches, articleConfig.HeightInInches);

                AdditionalByPage(compo, careInstructions, additionals, Symbols, compositionData, materialsFaces, labelLines, Separators);
            }

            int total_compo_pages;

            compositionData.TryGetValue("ComposNumber", out string componumber);
            total_compo_pages = int.Parse(componumber);

            total_compo_pages = total_compo_pages == 0 ? 1 : total_compo_pages;

            int total_addcare_pages = 0;
            if(compositionData.TryGetValue("AdditionalsNumber", out string additionalnumber))
                total_addcare_pages = int.Parse(additionalnumber);
            if(acumLabels - 1 == 1)
            {
                page1_totallines = compoArray.Where(s => !string.IsNullOrEmpty(s.Trim())).Count();
            }
            // var articleConfig = articleCompositionConfiguration.Retrieve(artCode, od.ProjectID);
            var articleCalculatorParams = new ArticleCalculatorBase.ArticleCalulatorParams()
            {
                ArticleCompositionConfig = articleConfig,
                Compo = compo,
                CompoIndex = indexCompo,
                Od = od,
                Additionals = additionals.Append(string.Join("", additionalArray)),
                AllCompoPages = total_compo_pages,
                AllAdditionalPages = total_addcare_pages,
                Page1_totallines = page1_totallines
            };
            var articleCategoryLst = articleCalculator.Calculate(articleCalculatorParams);
            if(!compositionData.TryGetValue("Symbols", out string symbols) && string.IsNullOrEmpty(Symbols.ToString().Trim()))
            {
                throw new System.Exception($"Saving Composition Symbols not found Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {compo.ID}");
            }
            var symbolsArray = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if(symbolsArray.Count() < 5)
            {
                throw new System.Exception($"Saving Composition There are only {symbolsArray.Count()} symbols Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {compo.ID} ");
            }
            if(!compositionData.TryGetValue("Page1_compo", out string page1_compo))
            {
                throw new System.Exception($"Saving Composition Page1_compo not found Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {compo.ID}");
            }

            if(string.IsNullOrEmpty(page1_compo))
            {
                throw new System.Exception($"Saving Composition Page1_compo is empty Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {compo.ID}");
            }
            compositionData.Add("OrderID", od.OrderID.ToString());
            SaveComposition(compo, od, compositionData, Symbols, careInstructions, orderData, Separators.CI_SEPARATOR, articleCategoryLst);
            ChangeArticle(od, printer_job.FirstOrDefault(), articleCategoryLst);
            var checkComposition = orderUtilService.GetCompositionData(orderData[0].ProjectID, compo.ID);
            if(checkComposition.TryGetValue("Symbols", out string symbolsCheck))
            {
                log.LogWarning($"Saving Composition Symbols saved: {symbolsCheck} Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {compo.ID} ");
            }
            else
            {
                throw new Exception($"Saving Composition Symbols saved: NotFound Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {compo.ID}");
            }
        }

        private void SaveComposition(CompositionDefinition compo,
                                    OrderPluginData od,
                                    Dictionary<string, string> compositionData,
                                    StringBuilder Symbols,
                                    StringBuilder careInstructions,
                                    List<OrderPluginData> orderData,
                                    string ciSeparator,
                                    List<ArticleSizeCategory> articleCategoryLst)
        {
            log.LogMessage($"Save Generic Compo for OrderGroupID: {od.OrderGroupID}, ( CompositionLabelID: {compo.ID} )");
            compositionData.Add("ArticleCodeSelected", articleCategoryLst.Last().ArticleCode);
            var cleanedCi = Regex.Replace(careInstructions.ToString().Trim(), ciSeparator + "$", string.Empty);// PERSONALIZED TRIM END
            orderUtilService.SaveComposition(orderData[0].ProjectID, compo.ID, compositionData, cleanedCi, Symbols.ToString());

        }

        private void ChangeArticle(OrderPluginData od, IPrinterJob job, List<ArticleSizeCategory> ArticleCategoryLst)
        {
            IArticle newarticle = null;

            var lst = ArticleCategoryLst.OrderByDescending(x => x.PageQuantity).Distinct().FirstOrDefault();

            newarticle = articleRepo.GetByCodeInProject(lst.ArticleCode, od.ProjectID);

            if(newarticle != null)
            {
                //update printdata by articlecode
                printerJobRepo.UpdateArticle(job.ID, newarticle.ID);

                using(var ctx = factory.GetInstance<PrintDB>())
                {
                    var printerDetails = ctx.PrinterJobDetails
                       .Join(ctx.PrinterJobs, ptjd => ptjd.PrinterJobID, ptj => ptj.ID, (pjd, pj) => new { PrinterJobDetail = pjd, PrinterJob = pj })
                       .Where(w => w.PrinterJob.CompanyOrderID == job.CompanyOrderID)
                       .Select(s => s.PrinterJobDetail)
                       .ToList();

                    var catalogs = catalogRepo.GetByProjectID(ctx, od.ProjectID, true);
                    var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));

                    // bulk update
                    var allIds = printerDetails.Select(s => s.ProductDataID);

                    using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
                    {
                        dynamicDB.Conn.ExecuteNonQuery(
                        $@"UPDATE d SET                                       
                                        ArticleCode = @ArticleCode
                                    FROM {detailCatalog.TableName} d
                                    WHERE d.ID in  ({string.Join(',', allIds)})", newarticle.ArticleCode);
                    }
                }
            }
        }

        private void GetCareInstructions(CompositionDefinition compo, StringBuilder careInstructions, Dictionary<string, string> compositionData, int linenumber)
        {
            var symbols = new List<string>();
            var addExc = new List<string>(); // additionals and exceptions
            var basic = new List<string>();

            var addExcTable = new Dictionary<int, List<string>>();

            //foreach (var ci in compo.CareInstructions.Where(w => w.Category == CareInstructionCategory.ADDITIONAL))
            foreach(var ci in compo.CareInstructions)
            {
                // ci individual translations
                var langsList = ci.AllLangs
                    .Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                var translated = langsList.Count > 1 ? string.Join(Separators.CI_LANG_SEPARATOR, langsList.Distinct()) : langsList[0];

                // TODO: create enumerate with ci categories or a catalog to avoid harcde category names
                if(ci.Category != "Additional" && ci.Category != "Exception")
                {
                    symbols.Add(ci.Symbol.Trim());
                    basic.Add(translated.Trim());

                }
                else
                {
                    if(ci.Category == "Additional")
                    {
                        addExc.Add(translated);
                        addExcTable[ci.ID] = langsList;
                    }
                }

            }

            // the keys of the composition data is a hardcode definition for CompositionLabel table for the current project
            careInstructions.Append(string.Join(Separators.CI_SEPARATOR, basic));
            compositionData.Add("FullCareInstructions", string.Join(Separators.CI_SEPARATOR, basic));


        }


        private void GetAdditionalTextLines(string[] additionalArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;

            while(index < additionalArray.Length)
            {
                var additionalLabel = additionalArray.Skip(index).Take(labelLines);
                StringBuilder additionalLabelText = new StringBuilder();
                foreach(var item in additionalLabel)
                {
                    additionalLabelText.Append($"{item}{Environment.NewLine}");
                }
                compositionData.Add($"AdditionalPage{acumLabels.ToString()}", additionalLabelText.ToString());
                acumLabels++;
                index += labelLines;
            }

            compositionData.Add($"AdditionalsNumber", (acumLabels - 1).ToString());
        }

        private static void GetCompositionDataPercentLines(string[] percentArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;
            while(index < percentArray.Length)
            {
                var percentLabel = percentArray.Skip(index).Take(labelLines);
                StringBuilder percentLabelText = new StringBuilder();
                foreach(var item in percentLabel)
                {
                    percentLabelText.Append($"{item}{Environment.NewLine}");
                }
                compositionData.Add($"Page{acumLabels.ToString()}_percent", percentLabelText.ToString());
                acumLabels++;
                index += labelLines;
            }
        }

        private static void GetCompositionDataLeatherLines(string[] leatherArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;
            var quantityOfPages = leatherArray.Length / labelLines;
            int lines = 0;
            for(int i = 0; i < quantityOfPages; i++)
            {
                var leatherPositionStrings = string.Empty;
                int lineNumber = 0;
                for(int j = lines; j < i * labelLines + labelLines; j++)
                {
                    if(leatherArray[j] == "1")
                    {
                        if(string.IsNullOrEmpty(leatherPositionStrings))
                        {
                            leatherPositionStrings = lineNumber.ToString();
                        }
                        else
                        {
                            leatherPositionStrings += $",{lineNumber.ToString()}";
                        }
                    }
                    lineNumber++;

                }
                lines = lines + labelLines;
                acumLabels = i + 1;
                compositionData.Add($"Page{acumLabels.ToString()}_leather", leatherPositionStrings);
            }
        }

        private static void GetCompositionDataCompoLines(string[] compoArray, int labelLines, Dictionary<string, string> compositionData, out int index, out int acumLabels)
        {
            index = 0;
            acumLabels = 1;
            while(index < compoArray.Length)
            {
                var compolabel = compoArray.Skip(index).Take(labelLines);
                StringBuilder labelText = new StringBuilder();
                foreach(var item in compolabel)
                {
                    //  labelText.Append(item.ToString().Concat(Environment.NewLine));
                    labelText.Append($"{item}{Environment.NewLine}");
                }
                compositionData.Add($"Page{acumLabels.ToString()}_compo", labelText.ToString());
                acumLabels++;
                index += labelLines;
            }
        }



        private static void GetFullCompositionLines(string[] fullCompoArray, int labelLines, Dictionary<string, string> compositionData)
        {
            if(fullCompoArray == null || fullCompoArray.Length == 0 || labelLines <= 0 || compositionData == null)
                return;

            var compoLabel = fullCompoArray;
            StringBuilder labelText = new StringBuilder();

            foreach(var item in compoLabel)
            {
                labelText.AppendLine($"{item}$Line");
            }
            var textoSinSaltosDeLinea = Regex.Replace(labelText.ToString(), @"\r\n?|\n", string.Empty);
            compositionData["FullComposition"] = textoSinSaltosDeLinea;
        }

        private static void GetFullAdditionalsLines(string[] fullAdditionalsArray, int labelLines, Dictionary<string, string> compositionData)
        {
            if(fullAdditionalsArray == null || fullAdditionalsArray.Length == 0 || labelLines <= 0 || compositionData == null)
                return;

            var compoLabel = fullAdditionalsArray;
            StringBuilder labelText = new StringBuilder();

            foreach(var item in compoLabel)
            {
                labelText.AppendLine($"{item}$Line");
            }
            var textoSinSaltosDeLinea = Regex.Replace(labelText.ToString(), @"\r\n?|\n", string.Empty);
            compositionData["FullAdditionals"] = textoSinSaltosDeLinea;
            var saltosDeLinea = labelText.ToString().Count(c => c == '\n');

        }

    }
}
