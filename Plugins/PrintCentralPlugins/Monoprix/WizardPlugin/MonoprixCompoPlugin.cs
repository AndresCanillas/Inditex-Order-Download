using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;


namespace SmartdotsPlugins.OrderPlugins
{
    [FriendlyName("Monoprix - Composition Text Plugin")]
    [Description("Concatenate Monoprix Composition Sections and Care Instructions.")]
    public class MonoprixCompoPlugin : IWizardCompositionPlugin
    {
        private IEventQueue events;
        private ILogSection log;
        private IOrderUtilService orderUtilService;
        private readonly int MAX_SHOES_FIBERS = 5;
        private readonly int MAX_SHOES_SECTIONS = 3;
        private readonly string EMPTY_CODE = "0";

        Dictionary<char, string> validSymbolsValue = new Dictionary<char, string> { { '1', "A" }, { '2', "B" }, { '3', "C" } };
        List<string> ShoesSymbols = new List<string>() { "", "", "", "", "" };
        string fibersIndex = "fiber_";
        string shoesIndex = "symbols_shoes_";
        string title = "title_";
        string fullComposition = "FullComposition";
        string fullCompositionInRows = "FullCompositionInRows";
        char shoeSymbolsConcatenatedChar = ',';
        string rowSeparator = "@";
        string columnSeparator = "#";

        //use language French because the English lang do not exist
        string[] SectionsLanguage = { "French" };
        string[] FibersLanguage = { "French" };
        string[] CareInstructionsLanguage = { "French" };
        string[] additionalLanguage = { "French" };
        string[] exceptionLanguage = { "French" };

        //Sections Titles
        Dictionary<string, string> CompoData = new Dictionary<string, string>();
        StringBuilder fullCompositionWithColumns = new StringBuilder();
        StringBuilder fullCompositionSb = new StringBuilder();
        StringBuilder fibers_in_sections = new StringBuilder();

        public MonoprixCompoPlugin(IEventQueue events, ILogService log, IOrderUtilService orderUtilService)
        {
            this.events = events;
            this.log = log.GetSection("Monoprix - CompoPlugin");
            this.orderUtilService = orderUtilService;
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {
            var projectID = orderData[0].ProjectID;
            var projectData = orderUtilService.GetProjectById(projectID);
            var sectionSeparator = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            var sectionLanguageSeparator = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            var fibersSeparator = string.IsNullOrEmpty(projectData.FibersSeparator) ? ";" : projectData.FibersSeparator;
            var fiberLanguageSeparator = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            var ciSeparator = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            var ciLanguageSeparator = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;


            Dictionary<CompoCatalogName, IEnumerable<string>> MonoprixLanguages = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            MonoprixLanguages.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            MonoprixLanguages.Add(CompoCatalogName.FIBERS, FibersLanguage);
            MonoprixLanguages.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            MonoprixLanguages.Add(CompoCatalogName.ADDITIONALS, additionalLanguage);
            MonoprixLanguages.Add(CompoCatalogName.EXCEPTIONS, exceptionLanguage);

            foreach(var dataFromOrder in orderData)
            {
                var composition = orderUtilService.GetComposition(dataFromOrder.OrderGroupID, true, MonoprixLanguages);

                foreach(var compoDefinition in composition)
                {

                    CompoData = new Dictionary<string, string>();
                    fullCompositionWithColumns = new StringBuilder();
                    fullCompositionSb = new StringBuilder();
                    fibers_in_sections = new StringBuilder();

                    SectionFibers(compoDefinition, sectionLanguageSeparator, fiberLanguageSeparator, fibersSeparator);

                    FullCompositionInRows(compoDefinition, sectionLanguageSeparator, fiberLanguageSeparator);

                    FulComposition();

                    var careInstructionsAndSymbols = ObtainCareInstructions(compoDefinition, ciLanguageSeparator, ciSeparator);
                    var careInstructions = careInstructionsAndSymbols.Item1;
                    var SymbolsCareInstructions = careInstructionsAndSymbols.Item2;

                    log.LogMessage($"Save Generic Compo for OrderGroupID: {dataFromOrder.OrderGroupID}, ( CompositionLabelID: {compoDefinition.ID} )");
                    orderUtilService.SaveComposition(projectID, compoDefinition.ID, CompoData, careInstructions, SymbolsCareInstructions);
                }
            }
        }

        #region Algorithm Methods
        private void SectionFibers(CompositionDefinition compoDefinition, string sectionLanguageSeparator, string fiberLanguageSeparator, string fibersSeparator)
        {
            for(var i = 0; i < compoDefinition.Sections.Count; i++)
            {

                //sections
                var title = "title_" + (i + 1);
                var titleValue = string.Empty;

                // AllLangs column was added manually in query using ',' as separator
                var langList = compoDefinition.Sections[i].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                if(compoDefinition.Sections[i].Code != EMPTY_CODE)
                    titleValue = langList.Length > 1 ? $" {String.Join(sectionLanguageSeparator, langList)} " : langList[0];

                CompoData.Add(title, titleValue);

                //fibers
                var fibers = compoDefinition.Sections[i].Fibers != null ? compoDefinition.Sections[i].Fibers : new List<Fiber>();
                var fiber = "fibers_" + (i + 1);
                var fiberValue = string.Empty;
                var fibersStrings = new StringBuilder();

                for(var f = 0; f < fibers.Count; f++)
                {
                    langList = fibers[f].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if(fibers[f].Code == EMPTY_CODE) continue;

                    var percentage = $"{fibers[f].Percentage}% ";
                    // for cuir and raphia remove percentage value
                    var fiberText = fibers[f].AllLangs.ToLower();

                    if(fiberText.Contains("cuir") || fiberText.Contains("raphia"))
                        percentage = string.Empty;

                    fiberValue = percentage + (langList.Length > 1 ? $" {String.Join(fiberLanguageSeparator, langList)}" : langList[0]);

                    //if (f < fibers.Count - 1) // is not last, add fibers separator too
                    //{
                    //    fiberValue += fibersSeparator;
                    //}

                    if(fibersStrings.Length > 0)
                        fibersStrings.Append(fibersSeparator);

                    fibersStrings.Append(fiberValue);

                }
                CompoData.Add(fiber, fibersStrings.ToString().Trim());
            }
        }

        private void FullCompositionInRows(CompositionDefinition compoDefinition, string sectionLanguageSeparator, string fiberLanguageSeparator)
        {
            fullCompositionWithColumns = new StringBuilder();
            List<string> Sections = new List<string>();
            List<string> Fibers = new List<string>();
            List<SectionFiber> SectionsFibers = new List<SectionFiber>();
            var Final = new List<ExpandoObject>();

            for(var i = 0; i < compoDefinition.Sections.Count; i++)
            {
                //Sections
                var titleValue = string.Empty;

                // AllLangs column was added manually in query using ',' as separator
                var langList = compoDefinition.Sections[i].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                if(compoDefinition.Sections[i].Code != EMPTY_CODE)
                    titleValue = langList.Length > 1 ? $"{String.Join(sectionLanguageSeparator, langList)}" : langList[0];

                Sections.Add(titleValue);

                //Fibers
                var fibers = compoDefinition.Sections[i].Fibers;
                var fiberValue = string.Empty;

                for(var f = 0; f < fibers.Count; f++)
                {
                    langList = fibers[f].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    fiberValue = (langList.Length > 1 ? $" {String.Join(fiberLanguageSeparator, langList)}" : langList[0]);
                    Fibers.Add(fiberValue);

                    //SectionsFibers
                    SectionFiber o1 = new SectionFiber();
                    o1.Section = titleValue;
                    o1.Fiber = fiberValue;
                    o1.Percentage = $"{fibers[f].Percentage}%";
                    SectionsFibers.Add(o1);
                }
            }

            //Final
            foreach(var fiber in Fibers.Distinct())
            {
                var found = SectionsFibers.Where(x => x.Fiber == fiber);

                if(found != null)
                {
                    var obj = new ExpandoObject();
                    var likeDictionary = obj as IDictionary<string, Object>;

                    //Create dynamic Columns                           
                    foreach(var section in Sections.Distinct())
                    {
                        likeDictionary[section] = "-";
                    }

                    likeDictionary["Fiber"] = fiber;

                    //Set Percentage
                    for(var i = 0; i < found.Count(); i++)
                    {
                        foreach(var f in found)
                        {
                            likeDictionary[f.Section] = f.Percentage;
                        }
                    }

                    Final.Add(obj);
                }
            }

            //Convert to DataTable
            var dt = Final.ToDataTable();

            //concatenate Sections
            for(var j = 0; j < dt.Columns.Count; j++)
            {
                var col = dt.Columns[j].ColumnName;

                if(col != "Fiber")
                {
                    fullCompositionWithColumns.Append(col + columnSeparator);
                }
            }

            //remove and add last @
            fullCompositionWithColumns.Length = fullCompositionWithColumns.Length - 1;
            fullCompositionWithColumns.Append("@");

            //concatenate Fibers
            for(var l = 0; l < dt.Rows.Count; l++)
            {

                var fibers_sections = "fibers_" + (l + 1) + "_in_sections";

                for(var j = 0; j < dt.Columns.Count; j++)
                {
                    var col = dt.Columns[j].ColumnName;

                    var row = dt.Rows[l][col].ToString();

                    if(row.Contains('%') || row.Contains('-'))
                    {
                        fullCompositionWithColumns.Append(row + columnSeparator);
                        fibers_in_sections.Append(row + columnSeparator);
                    }
                    else
                    {
                        fullCompositionWithColumns.Append(row + rowSeparator);
                        fibers_in_sections.Append(row);
                    }
                }

                CompoData.Add(fibers_sections, fibers_in_sections.ToString().Trim());
                fibers_in_sections.Clear();

            }

            CompoData.Add(fullCompositionInRows, fullCompositionWithColumns.ToString().Trim());
        }

        private void FulComposition()
        {
            var lst = CompoData.Keys.Where(x => x.Contains("title"));

            foreach(var item in lst)
            {
                int charsearch = item.IndexOf("_");
                char idx = item[charsearch + 1];

                var section = CompoData["title_" + idx];
                var fiber_percentage = CompoData["fibers_" + idx];

                fullCompositionSb.Append(section + ":" + fiber_percentage.Replace(";", "#") + "@");
            }

            CompoData.Add(fullComposition, fullCompositionSb.ToString().Trim());
        }
        #endregion


        private (string, string) ObtainCareInstructions(CompositionDefinition compositionDefinition, string CareInstructionLanguageSeparator, string careInstructionSeparator)
        {
            var careInstructionsStringBuilder = new StringBuilder();
            var SymbolsCareInstructionsStringBuilder = new StringBuilder();

            // Monoprix rules, only show text for additionals in all labels

            foreach(var careInstruction in compositionDefinition.CareInstructions.Where(w => w.Category == "Additional"))
            {
                var langsList = careInstruction.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                //SymbolsCareInstructionsStringBuilder.Append(careInstruction.Symbol);

                var translations = langsList.Length > 1 ? string.Join(CareInstructionLanguageSeparator, langsList) : langsList[0];

                if(careInstructionsStringBuilder.Length > 0)
                    careInstructionsStringBuilder.Append(careInstructionSeparator);

                careInstructionsStringBuilder.Append(translations);
            }

            var careInstructions = careInstructionsStringBuilder.ToString().Trim();
            var SymbolsCareInstructionsArr = compositionDefinition.CareInstructions.Select(ci => ci.Symbol).ToArray();
            var SymbolsCareInstructions = string.Join(string.Empty, SymbolsCareInstructionsArr);

            careInstructionsStringBuilder.Clear();
            SymbolsCareInstructionsStringBuilder.Clear();

            return (careInstructions, SymbolsCareInstructions);
        }

        public void Dispose()
        {

        }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return null;
        }
        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {
        }
        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {
        }

    }

    public class SectionFiber
    {
        public string Section { get; set; }
        public string Fiber { get; set; }
        public string Percentage { get; set; }
    }
}
