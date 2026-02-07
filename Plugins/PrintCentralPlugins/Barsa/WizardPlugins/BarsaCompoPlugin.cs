using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Barsa.WizardPlugins
{
    [FriendlyName("BarçaCompoPlugin")]
    [Description("Barça - Concanate composition Sections and Care Instructions.")]
    public class BarsaCompoPlugin : IWizardCompositionPlugin
    {
        private readonly ICatalogRepository _catalogRepo;
        private readonly IOrderUtilService _orderUtilService;
        private readonly ILogService _log;

        private string SECTION_SEPARATOR;
        private string SECTION_LANGUAGESEPA;
        private string FIBERS_SEPARATOR;
        private string FIBER_LANGUAGESEPARA;
        private string CI_SEPARATOR;
        private string CI_LANGUAGESEPARATOR;

        string[] CareInstructionsLanguageSortedAll = { "English", "ES", "FR", "CAT", "PT", "IT", "DE", "NL", "FI", "NO", "RU", "CS", "PL", "HU", "HR", "SK", "BG", "RO", "LT", "TR" };

        public BarsaCompoPlugin(IOrderUtilService orderUtilService, ICatalogRepository catalogRepo, ILogService log)
        {

            _orderUtilService = orderUtilService;
            _catalogRepo = catalogRepo;
            _log = log;
        }

        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {

        }

        public void Dispose()
        {

        }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad) => null;

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {

            ICatalog ordersCatalog, detailCatalog, variableDataCatalog, compositionLabelCatalog, baseDataCatalog, madeInCatalog, careInstructionsCatalog;
            GetCatalogs(orderData[0].ProjectID, out ordersCatalog, out detailCatalog, out variableDataCatalog, out compositionLabelCatalog, out madeInCatalog, out careInstructionsCatalog);
            var projectData = _orderUtilService.GetProjectById(orderData[0].ProjectID);
            SetSeparators(projectData);
            ReadOrderData(orderData);
        }

        private void ReadOrderData(List<OrderPluginData> orderDatas)
        {

            var projectData = _orderUtilService.GetProjectById(orderDatas[0].ProjectID);
            var removeDuplicates = projectData.RemoveDuplicateTextFromComposition;

            foreach(var orderData in orderDatas)
            {
                var compositionsFull = _orderUtilService.GetComposition(orderData.OrderGroupID, true, GetBarsaLanguageDictionary());
                int rowIndex = 0;
                foreach(var compo in compositionsFull)
                {
                    var dataResult = ProcessRow(compo, removeDuplicates);
                    SaveCompo(compo, dataResult, orderDatas[0].ProjectID);
                }
            }
        }

        private BarsaComposition ProcessRow(CompositionDefinition compo, bool removeDuplicates)
        {
            var newCompo = GenerateFullComposition(compo, removeDuplicates);
            //ar careInstructionsAll = SplitCareInstructions(compo.CareInstructions);
            var careInstructionsIntoBox = SplitCareInstructionsIntoBoxes(compo.CareInstructions);
            var symbols = GetAllSymbols(compo.CareInstructions);

            BarsaComposition dataResult = new BarsaComposition();
            dataResult.FullComposition = newCompo;
            dataResult.CareInstructionsSplit = careInstructionsIntoBox;
            dataResult.FullCareInstructions = string.Join(string.Empty, careInstructionsIntoBox);
            dataResult.Symbols = symbols;

            return dataResult;
        }

        private string GetAllSymbols(IList<CareInstruction> careInstructions)
        {
            StringBuilder symbols = new StringBuilder(string.Empty, 10);
            foreach(var careIntructions in careInstructions)
            {
                var langsList = careIntructions.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                symbols.Append(careIntructions.Symbol);
            }

            return symbols.ToString();
        }

        private string GenerateFullComposition(CompositionDefinition compo, bool removeDuplicates)
        {
            var newCompo = string.Empty;

            if(compo.Sections.Count == 1)
                return GenerateCompoOneSection(compo.Sections[0], removeDuplicates);

            foreach(var section in compo.Sections)
            {
                var langsList = section.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                if(removeDuplicates)
                    langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                if(newCompo.Length > 0)
                    newCompo += SECTION_SEPARATOR;

                newCompo += langsList[0];
                newCompo += SECTION_SEPARATOR;

                var fibers = section.Fibers != null ? section.Fibers : new List<Fiber>();

                foreach(var fiber in section.Fibers)
                {
                    langsList = fiber.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                    if(removeDuplicates)
                        langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                    newCompo += $"{fiber.Percentage}% " + (langsList.Count > 1 ? $" {String.Join(FIBER_LANGUAGESEPARA, langsList)} " : langsList[0]);
                    newCompo += FIBERS_SEPARATOR;
                }

            }

            return newCompo;
        }

        private string GenerateCompoOneSection(Section section, bool removeDuplicates)
        {
            var newCompo = string.Empty;

            foreach(var f in section.Fibers)
            {
                var langsList = f.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                if(removeDuplicates)
                    langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                newCompo += $"{f.Percentage}% " + (langsList.Count > 1 ? $" {String.Join(FIBER_LANGUAGESEPARA, langsList)} " : langsList[0]);
                newCompo += FIBERS_SEPARATOR;
            }

            return newCompo;
        }

        private void SaveCompo(CompositionDefinition compo, BarsaComposition dataResult, int projectID)
        {
            var compositionData = new Dictionary<string, string>();
            string careInstructionsSymbolAll = string.Empty;
            compositionData["FullComposition"] = dataResult.FullComposition;
            compositionData["FullCareInstructions"] = dataResult.FullCareInstructions;
            compositionData["Symbols"] = dataResult.Symbols;
            compositionData["CareInstructionsWarning"] = dataResult.CareInstructionsWarning ? "1" : "0";// store in database as int


            for(var ciBoxNumber = 0; ciBoxNumber < dataResult.CareInstructionsSplit.Count; ciBoxNumber++)
                compositionData[$"CareInstructions{ciBoxNumber + 1}"] = dataResult.CareInstructionsSplit[ciBoxNumber];

            _orderUtilService.SaveComposition(projectID, compo.ID, compositionData, dataResult.FullCareInstructions, careInstructionsSymbolAll);

        }

        private Dictionary<CompoCatalogName, IEnumerable<string>> GetBarsaLanguageDictionary()
        {
            Dictionary<CompoCatalogName, IEnumerable<string>> AvailableLangs = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            AvailableLangs.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguageSortedAll);
            AvailableLangs.Add(CompoCatalogName.EXCEPTIONS, CareInstructionsLanguageSortedAll);
            AvailableLangs.Add(CompoCatalogName.ADDITIONALS, CareInstructionsLanguageSortedAll);
            return AvailableLangs;
        }
        private IList<string> SplitCareInstructionsIntoBoxes(IList<CareInstruction> userCareInstructions)
        {
            Dictionary<string, List<string>> languagesCareInstructionsDictionary = new Dictionary<string, List<string>>();

            InitzialicerDictionaryCareInstructions(languagesCareInstructionsDictionary);

            string concatFibers = string.Empty;
            string[] langs = new string[CareInstructionsLanguageSortedAll.Count()];

            for(var i = 0; i < userCareInstructions.Count; i++)
            {
                var langsCareInstructions = userCareInstructions[i].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                for(var j = 0; j < langsCareInstructions.Count(); j++)
                {
                    var languageKey = CareInstructionsLanguageSortedAll[j];
                    languagesCareInstructionsDictionary[languageKey].Add(langsCareInstructions[j]);
                }

            }

            //var fullCareInstructions = AddAllLangsString(languagesCareInstructionsDictionary);

            return SplitCareInstructionsIntoBoxesByCharacteres(languagesCareInstructionsDictionary);

        }

        // put text into 3 Containers, Designer team make test and require the next
        // first box size: 26.71mm Width X  84.62mm Height -> 37 lines -> 1500 characters
        // second box size: 26.71mm Width X  88.69mm Height -> ?? lines -> 1650 characters
        // third box size : 26.71mm Width X 61.00mm Hight -> 25 Lines
        //
        // Size Strategy - the with of the box will be duplicated because inner nicelabel the font is transform horizontally to 50% less
        // Character Count Strategy

        private IList<string> SplitCareInstructionsIntoBoxesByCharacteres(Dictionary<string, List<string>> languagesCareInstructionsDictionary)
        {
            int currentBox = 0;
            List<int> maxBoxSize = new List<int>() { 1100, 1300, 1300, 5000 };
            List<string> boxContent = new List<string>();


            StringBuilder sb = new StringBuilder(1650);


            foreach(var entry in languagesCareInstructionsDictionary)
            {
                var languageKey = entry.Key;
                var instructionsList = entry.Value;

                var combinedInstructions = string.Join(CI_LANGUAGESEPARATOR, instructionsList);

                if(languageKey == "English")
                    combinedInstructions = $"EN: {combinedInstructions}{CI_SEPARATOR}";
                else
                    combinedInstructions = $"{languageKey}: {combinedInstructions}{CI_SEPARATOR}";
                
                if(sb.Length + combinedInstructions.Length > maxBoxSize[currentBox])
                {
                    
                    boxContent.Add(Regex.Replace(sb.ToString(), $"{CI_SEPARATOR}$", string.Empty));
                    sb.Clear();
                    currentBox++;
                }

                sb.Append(combinedInstructions);
                

            }

            boxContent.Add(Regex.Replace(sb.ToString(), $"{CI_SEPARATOR}$", string.Empty));

            return boxContent;

        }

        private string SplitCareInstructions(IList<CareInstruction> lst)
        {
            Dictionary<string, List<string>> languagesCareInstructionsDictionary = new Dictionary<string, List<string>>();

            InitzialicerDictionaryCareInstructions(languagesCareInstructionsDictionary);

            string concatFibers = string.Empty;
            string[] langs = new string[CareInstructionsLanguageSortedAll.Count()];

            for(var i = 0; i < lst.Count; i++)
            {
                var langsCareInstructions = lst[i].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                for(var j = 0; j < langsCareInstructions.Count(); j++)
                {
                    var languageKey = CareInstructionsLanguageSortedAll[j];
                    languagesCareInstructionsDictionary[languageKey].Add(langsCareInstructions[j]);
                }

            }

            var fullCareInstructions = AddAllLangsString(languagesCareInstructionsDictionary);

            return fullCareInstructions;
        }

        private string AddAllLangsString(Dictionary<string, List<string>> languagesCareInstructionsDictionary)
        {
            var resultStringBuilder = string.Empty;

            foreach(var entry in languagesCareInstructionsDictionary)
            {
                var languageKey = entry.Key;
                var instructionsList = entry.Value;

                var combinedInstructions = string.Join(CI_LANGUAGESEPARATOR, instructionsList);

                if(languageKey == "English")
                    resultStringBuilder += $"EN: {combinedInstructions} {CI_SEPARATOR}";
                else
                    resultStringBuilder += $"{languageKey}: {combinedInstructions} {CI_SEPARATOR}";

            }

            return resultStringBuilder.ToString();
        }

        private void InitzialicerDictionaryCareInstructions(Dictionary<string, List<string>> languagesCareInstructions)
        {
            foreach(string language in CareInstructionsLanguageSortedAll)
            {
                //if (language == "English")
                //    languagesCareInstructions["English"] = new List<string>();
                //else
                languagesCareInstructions[language] = new List<string>();
            }

        }

        private void SetSeparators(IProject projectData)
        {
            SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            SECTION_LANGUAGESEPA = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            FIBERS_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            FIBER_LANGUAGESEPARA = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "\n" : projectData.CISeparator;
            CI_LANGUAGESEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "/" : projectData.CILanguageSeparator;
        }

        private void GetCatalogs(int projectId, out ICatalog ordersCatalog, out ICatalog detailCatalog, out ICatalog variableDataCatalog, out ICatalog compositionLabelCatalog, out ICatalog madeInCatalog, out ICatalog careInstructionsCatalog)
        {
            var catalogs = _catalogRepo.GetByProjectID(projectId, true);
            ordersCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
            detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            compositionLabelCatalog = catalogs.First(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));
            careInstructionsCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_CAREINSTRUCTIONS_CATALOG));
            madeInCatalog = catalogs.First(f => f.Name.Equals(Catalog.BRAND_MADEIN_CATALOG));
        }

        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {


        }


    }
}
