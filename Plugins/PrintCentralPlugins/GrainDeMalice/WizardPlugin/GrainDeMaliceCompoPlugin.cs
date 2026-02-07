using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.GrainDeMalice.WizardPlugin
{
    [FriendlyName("Grain de Malice Composition Text Plugin")]
    [Description("Grain de Malice - Concatenate Composition Sections and Care Instructions.")]
    public class GrainDeMaliceCompoPlugin : IWizardCompositionPlugin
    {
        private ILogSection _log;
        private IOrderUtilService _orderUtilService;

        string[] SectionsLanguage = { "French", "English", "German", "Dutch", "Italian", "Spanish", "Portuguese" };
        string[] FibersLanguage = { "French", "English", "German", "Dutch", "Italian", "Spanish", "Portuguese" };

        string[] AdditionalsLanguage = { "French", "English", "German", "Dutch", "Italian", "Spanish", "Portuguese" };
        string[] ExceptionsLanguage = { "French", "English", "German", "Dutch", "Italian", "Spanish", "Portuguese" };

        string[] CareInstructionsLanguage = { "French", "English", "German", "Dutch", "Italian", "Spanish", "Portuguese" };

        public GrainDeMaliceCompoPlugin(ILogService log, IOrderUtilService orderUtilService)
        {
            _log = log.GetSection("GrainDeMalice-CompoPlugin");
            _orderUtilService = orderUtilService;
        }

        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {

        }

        public void Dispose()
        {

        }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return null;
        }

        private Dictionary<CompoCatalogName, IEnumerable<string>> GetGDMLanguageDictionary()
        {
            Dictionary<CompoCatalogName, IEnumerable<string>> GDMLanguages = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            GDMLanguages.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            GDMLanguages.Add(CompoCatalogName.FIBERS, FibersLanguage);
            GDMLanguages.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);
            GDMLanguages.Add(CompoCatalogName.EXCEPTIONS, ExceptionsLanguage);
            GDMLanguages.Add(CompoCatalogName.ADDITIONALS, AdditionalsLanguage);
            return GDMLanguages;
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {
            _log.LogMessage("Initialized Grain de Malice Composition Plugin");

            var projectData = _orderUtilService.GetProjectById(orderData[0].ProjectID);
            var (sectionSeparator, sectionLanguageSeparator, fibersSeparator, fiberLanguageSeparator, ciSeparator, ciLanguageSeparator) = GetSeparators(projectData);

            Dictionary<CompoCatalogName, IEnumerable<string>> GDMLanguages = GetGDMLanguageDictionary();

            foreach(var order in orderData)
            {
                var compositions = _orderUtilService.GetComposition(order.OrderGroupID,true, GDMLanguages, "/");

                foreach(var composition in compositions)
                {
                    //add composition
                    var fullComposition = string.Empty;

                    if(composition.Sections.Count > 1)
                        fullComposition = GenerateComposition(sectionSeparator, sectionLanguageSeparator, fibersSeparator, fiberLanguageSeparator, composition, fullComposition, false);
                    else
                        fullComposition = GenerateComposition(sectionSeparator, sectionLanguageSeparator, fibersSeparator, fiberLanguageSeparator, composition, fullComposition, true);


                    //add care instructions
                    var careInstructions = string.Empty;
                    var onlyAdditionals = string.Empty;
                    string onlyExceptions = string.Empty;
                    var onlyMainCareInstructions = string.Empty;
                    StringBuilder symbols = new StringBuilder(string.Empty, 10);

                    GenerateFullCareInstructions(ciSeparator, ciLanguageSeparator, composition, ref careInstructions, ref onlyAdditionals, ref onlyExceptions, ref onlyMainCareInstructions, symbols);

                    _log.LogMessage($"Save GDM Compo for OrderGroupID: {order.OrderGroupID}, ( CompositionLabelID: {composition.ID} )");

                    var compoData = new Dictionary<string, string>
                    {
                        { "FullComposition", fullComposition },
                        { "FullCareInstructions", careInstructions.Length > 3 ? careInstructions.Substring(0,careInstructions.Length - 3) : careInstructions },
                        { "Symbols", symbols.ToString() },
                        // new fields, save method from DynamicDB will be ignore it if CompositionLabel table don't have fields 
                        { "AdditionalsCareInstructions", onlyAdditionals.Length > 3 ? onlyAdditionals.Substring(0, onlyAdditionals.Length - 3) : onlyAdditionals },
                        { "ExceptionsCareInstructions", onlyExceptions.Length > 3 ? onlyExceptions.Substring(0, onlyExceptions.Length - 3) : onlyExceptions },
                        { "MainCareInstructions", onlyMainCareInstructions.Length > 3 ? onlyMainCareInstructions.Substring(0, onlyMainCareInstructions.Length - 3) : onlyMainCareInstructions }
                    };

                    _orderUtilService.SaveComposition(orderData[0].ProjectID, composition.ID, compoData, string.Empty, string.Empty);

                }
            }
            _log.LogMessage("Finished Grain de Malice Composition Plugin");

        }

        public void GenerateFullCareInstructions(string ciSeparator, string ciLanguageSeparator, CompositionDefinition composition, ref string careInstructions, ref string onlyAdditionals, ref string onlyExceptions, ref string onlyMainCareInstructions, StringBuilder symbols)
        {
            foreach(var ci in composition.CareInstructions)
            {
                var langsList = ci.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                //var arraySorted = SortArrayLanguages(langsList);
                symbols.Append(ci.Symbol);

                var languageString = String.Join(ciLanguageSeparator, langsList);

                careInstructions += langsList.Length > 1 ? languageString : langsList[0];
                careInstructions += $" {ciSeparator} ";

                if(ci.Category == CareInstructionCategory.ADDITIONAL)
                {
                    //No concatena el valor de la columna USES
                    //languageString = String.Join(ciLanguageSeparator, langsList.Where((_, index) => index != 0));
                    onlyAdditionals = GenerateAdditionals(ciSeparator, onlyAdditionals, langsList, languageString);
                }
                else if(ci.Category == CareInstructionCategory.EXCEPTION)
                {
                    onlyExceptions = GenerateExceptions(ciSeparator, onlyExceptions, langsList, languageString);
                }
                else
                {
                    onlyMainCareInstructions = GenerateMainCareInstructions(ciSeparator, onlyMainCareInstructions, langsList, languageString);
                }
            }
        }

        public string GenerateMainCareInstructions(string ciSeparator, string onlyMainCareInstructions, string[] langsList, string languangeString)
        {
            onlyMainCareInstructions += langsList.Length > 1 ? languangeString : langsList[0];
            onlyMainCareInstructions += $" {ciSeparator} ";
            return onlyMainCareInstructions;
        }

        public string GenerateExceptions(string ciSeparator, string onlyExceptions, string[] langsList, string languangeString)
        {
            //var arraySorted = SortArrayLanguages(langsList);
            onlyExceptions += langsList.Length > 1 ? languangeString : langsList[0];
            onlyExceptions += $" {ciSeparator} ";
            return onlyExceptions;
        }

        public string GenerateAdditionals(string ciSeparator, string onlyAdditionals, string[] langsList, string languangeString)
        {
            //var arraySorted = SortArrayLanguages(langsList);
            onlyAdditionals += langsList.Length > 1 ? languangeString : langsList[0];
            onlyAdditionals += $" {ciSeparator} ";
            return onlyAdditionals;
        }

        private string GenerateComposition(string sectionSeparator, string sectionLanguageSeparator, string fibersSeparator, string fiberLanguageSeparator, CompositionDefinition composition, string compo, bool oneSection)
        {
            foreach(var s in composition.Sections)
            {

                var langsList = s.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                if(!oneSection)
                {
                    //var arraySorted = SortArrayLanguages(langsList);
                    compo += langsList.Length > 1 ? String.Join(sectionLanguageSeparator, langsList) : langsList[0];
                    compo += sectionSeparator;
                }
                else
                    compo = string.Empty;

                var fibers = s.Fibers != null ? s.Fibers : new List<Fiber>();

                foreach(var f in s.Fibers)
                {
                    langsList = f.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    //var arraySorted = SortArrayLanguages(langsList);

                    compo += $"{f.Percentage}% " + (langsList.Length > 1 ? $" {String.Join(fiberLanguageSeparator, langsList)} " : langsList[0]);
                    compo += fibersSeparator;
                }

            }

            return compo;
        }

        private string[] SortArrayLanguages(string[] langsList)
        {
            var temp = langsList[0];
            langsList[0] = langsList[1];
            langsList[1] = temp;

            return langsList;
        }

        private (string sectionSeparator, string sectionLanguageSeparator, string fibersSeparator, string fiberLanguageSeparator, string ciSeparator, string ciLanguageSeparator) GetSeparators(IProject projectData)
        {
            var sectionSeparator = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            var sectionLanguageSeparator = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            var fibersSeparator = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            var fiberLanguageSeparator = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            var ciSeparator = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            var ciLanguageSeparator = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;

            return (sectionSeparator, sectionLanguageSeparator, fibersSeparator, fiberLanguageSeparator, ciSeparator, ciLanguageSeparator);
        }


        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {
        }
    }
}
