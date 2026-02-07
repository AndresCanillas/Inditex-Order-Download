using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;
using System.Linq;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.OrderPlugins
{
    [FriendlyName("Jacadi - Composition Text Plugin")]
    [Description("Concatenate Jacadi Composition Sections and Care Instructions.")]
    public class JacadiCompoPlugin : IWizardCompositionPlugin
    {
        private IOrderUtilService orderUtilService;
        private readonly int MAX_SHOES_FIBERS = 5;
        private readonly int MAX_SHOES_SECTIONS = 3;
        private readonly string EMPTY_CODE = "0";
        private readonly string[] FibersLanguage = { "French", "English", "Chinese", "Italian", "Spanish", "Portuguese", "German", "Polish", "Albanian", "Croatian", "Romanian", "Czech", "Bulgarian", "Greek", "Catalan", "Russian", "Turkish", "Korean", "Japanese", "Arabic"  };


        public JacadiCompoPlugin(IOrderUtilService orderUtilService)
        {
            this.orderUtilService = orderUtilService;
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {
            var projectData = orderUtilService.GetProjectById(orderData[0].ProjectID);
            var sectionSeparator = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            var sectionLanguageSeparator = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            var fibersSeparator = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            var fiberLanguageSeparator = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            var ciSeparator = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            var ciLanguageSeparator = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;
            var removeDuplicates = projectData.RemoveDuplicateTextFromComposition;

            Dictionary<CompoCatalogName, IEnumerable<string>> JacadiLanguages = AddLanguagesDictionary();

            foreach(var od in orderData)
            {
                var composition = orderUtilService.GetComposition(od.OrderGroupID, true, JacadiLanguages);

                foreach(var c in composition)
                {
                    var compositionData = new Dictionary<string, string>();

                    // exist a fond called "SIMBOLOS CALZADO.otf" created by IDT designers
                    // 1->A, 2->B, 3->C
                    //StringBuilder ShoesSymbols = new StringBuilder(string.Empty, 15);
                    List<string> ShoesSymbols;
                    var validSymbolsValue = new Dictionary<char, string> { { '1', "A" }, { '2', "B" }, { '3', "C" } };
                    var sb = new StringBuilder();

                    for(var i = 0; i < c.Sections.Count; i++)
                    {
                        var title = "title_" + (i + 1);
                        var symbolShoe = "symbols_shoes_" + (i + 1);
                        var titleValue = string.Empty;
                        ShoesSymbols = new List<string>() { "", "", "", "", "" };

                        // AllLangs column was added manually in query using ',' as separator
                        var langsList = c.Sections[i].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                        if(removeDuplicates)
                            langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                        if(c.Sections[i].Code != EMPTY_CODE)
                            titleValue = langsList.Count > 1 ? $" {String.Join(sectionLanguageSeparator, langsList)} " : langsList[0];

                        compositionData.Add(title, titleValue);
                        sb.Append(titleValue).Append(sectionSeparator);

                        //section fibers
                        var fibers = c.Sections[i].Fibers != null ? c.Sections[i].Fibers : new List<Fiber>();
                        var fiber = "fibers_" + (i + 1);
                        var fiberValue = string.Empty;
                        var fibersStrings = new StringBuilder();

                        for(var f = 0; f < fibers.Count; f++)
                        {
                            if(!string.IsNullOrEmpty(fibers[f].FiberType) && validSymbolsValue.TryGetValue(fibers[f].FiberType[0], out var keyLetter))
                            {
                                if(f < MAX_SHOES_FIBERS)
                                    ShoesSymbols[f] = keyLetter;
                            }

                            langsList = fibers[f].AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                            if(removeDuplicates)
                                langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                            if(fibers[f].Code != EMPTY_CODE)
                            {
                                fiberValue += $"{fibers[f].Percentage}% " + (langsList.Count > 1 ? $" {String.Join(fiberLanguageSeparator, langsList)}" : langsList[0]);
                                if(f < fibers.Count - 1) // is not last, add fibers separator too
                                {
                                    fiberValue += fibersSeparator;
                                }

                            }
                        }

                        fibersStrings.Append(fiberValue);

                        if(i < c.Sections.Count - 1) // is not last, add fibers separator too
                        {
                            fiberValue += sectionSeparator;// TODO: check if use fibersSeparator or sectionsSeparator
                            fibersStrings.Append(sectionSeparator);
                        }

                        sb.Append(fibersStrings);

                        compositionData.Add(fiber, fiberValue);

                        // only 3 first sections, fibers contains shoes symbols
                        if(i < MAX_SHOES_SECTIONS)
                            compositionData.Add(symbolShoe, string.Join(";", ShoesSymbols));

                    }

                    compositionData.Add("FullComposition", sb.ToString().Trim());
                    sb.Clear();

                    StringBuilder careInstructions = new StringBuilder();
                    StringBuilder Symbols = new StringBuilder(string.Empty, 10);

                    foreach(var ci in c.CareInstructions)
                    {
                        var langsList = ci.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                        if(removeDuplicates)
                            langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();


                        Symbols.Append(ci.Symbol);// TODO: now, always use FONT 

                        var translations = langsList.Count > 1 ? string.Join(ciLanguageSeparator, langsList) : langsList[0];
                        careInstructions.Append(translations);
                        careInstructions.Append(ciSeparator);
                    }

                    orderUtilService.SaveComposition(orderData[0].ProjectID, c.ID, compositionData, careInstructions.ToString(), Symbols.ToString());
                }

            }
        }

        private Dictionary<CompoCatalogName, IEnumerable<string>> AddLanguagesDictionary()
        {
            Dictionary<CompoCatalogName, IEnumerable<string>> JacadiLanguages = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            JacadiLanguages.Add(CompoCatalogName.FIBERS, FibersLanguage);

            return JacadiLanguages;
        }

        public void Dispose(){}
        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return null;
        }
        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets){}
        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data){}
    }
}
