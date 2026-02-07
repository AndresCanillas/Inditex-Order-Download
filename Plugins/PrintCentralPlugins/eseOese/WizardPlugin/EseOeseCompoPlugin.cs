using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;
using WebLink.Services;

namespace SmartdotsPlugins.eseOese.WizardPlugin
{
    [FriendlyName("EseOese Composition Plugin")]
    [Description("EseOese Composition Plugin")]
    public class EseOeseCompoPlugin : IWizardCompositionPlugin
    {
        private readonly IOrderUtilService _orderUtilService;

        string[] SectionsLanguage = { "Spanish", "English", "Portuguese", "French" };
        string[] FibersLanguage = { "Spanish", "English", "French", "Catalan", "Basque", "Galician" };
        string[] CareInstructionsLanguage = { "Spanish", "English", "Portuguese", "French" };



        public string SECTION_SEPARATOR { get; set; }
        public string SECTION_LANG_SEPARATOR { get; set; }
        public string FIBER_SEPARATOR { get; set; }
        public string FIBER_LANG_SEPARATOR { get; set; }
        public string CI_SEPARATOR { get; set; }
        public string CI_LANG_SEPARATOR { get; set; }

        public EseOeseCompoPlugin(IOrderUtilService orderUtilService)
        {
            _orderUtilService = orderUtilService;
        }
        private void InitializceSeparator(IProject projectData)
        {
            SECTION_SEPARATOR = string.IsNullOrEmpty(projectData.SectionsSeparator) ? "\n" : projectData.SectionsSeparator;
            SECTION_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.SectionLanguageSeparator) ? "/" : projectData.SectionLanguageSeparator;
            FIBER_SEPARATOR = string.IsNullOrEmpty(projectData.FibersSeparator) ? "\n" : projectData.FibersSeparator;
            FIBER_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.FiberLanguageSeparator) ? "/" : projectData.FiberLanguageSeparator;
            CI_SEPARATOR = string.IsNullOrEmpty(projectData.CISeparator) ? "/" : projectData.CISeparator;
            CI_LANG_SEPARATOR = string.IsNullOrEmpty(projectData.CILanguageSeparator) ? "*" : projectData.CILanguageSeparator;
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

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {
            var projectData = _orderUtilService.GetProjectById(orderData[0].ProjectID);
            var removeDuplicates = projectData.RemoveDuplicateTextFromComposition;

            InitializceSeparator(projectData);
            Dictionary<CompoCatalogName, IEnumerable<string>> brandLangs = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            brandLangs.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            brandLangs.Add(CompoCatalogName.FIBERS, FibersLanguage);
            brandLangs.Add(CompoCatalogName.CAREINSTRUCTIONS, CareInstructionsLanguage);

            GenerateCompo(orderData, brandLangs, removeDuplicates, projectData);

        }

        private void GenerateCompo(List<OrderPluginData> orderData, Dictionary<CompoCatalogName, IEnumerable<string>> brandLangs, bool removeDuplicates, IProject projectData)
        {
            foreach(var order in orderData)
            {
                var composition = _orderUtilService.GetComposition(order.OrderGroupID, true, brandLangs, OrderUtilService.LANG_SEPARATOR);

                foreach(var compo in composition)
                {
                    var compositionData = new Dictionary<string, string>();
                    var fiberValue = new List<string>();
                    var compoTextAllLangs = string.Empty;
                    var compoTextSpanish = string.Empty;
                    var compoTextEnglish = string.Empty;
                    var firstSection = true;

                    foreach(var section in compo.Sections.Where(w => !w.IsBlank && w.Fibers != null && w.Fibers.Count > 0))
                    {
                        List<string> langsList = ExtractTranslations(removeDuplicates, section.AllLangs);

                        if(langsList.Count == 1 && langsList[0] == " ")
                            langsList[0] = "";

                        var spanishCompo = ExtractSpanishTranslations(section.AllLangs);
                        var englishCompo = ExtractEnglishTranslations(section.AllLangs);

                        compoTextSpanish = GenerateCompoOneLanguage(spanishCompo, section, "spanish", compoTextSpanish,firstSection);
                        compoTextEnglish = GenerateCompoOneLanguage(englishCompo, section, "english", compoTextEnglish, firstSection);
                        compoTextAllLangs = GenerateCompoAllLangs(removeDuplicates, compoTextAllLangs, section, langsList, firstSection);
                        firstSection = false;
                    }
                    compositionData.Add("FullComposition", compoTextAllLangs);
                    compositionData.Add("SpanishComposition", compoTextSpanish);
                    compositionData.Add("EnglishComposition", compoTextEnglish);

                    StringBuilder careInstructions = new StringBuilder();
                    StringBuilder fullAdditionals = new StringBuilder();
                    StringBuilder Symbols = new StringBuilder(string.Empty, 10);

                    for(int i = 0; i < compo.CareInstructions.Count; i++)
                    {
                        var ci = compo.CareInstructions[i];
                        var langsList = ci.AllLangs.Split("__S__", StringSplitOptions.RemoveEmptyEntries).ToList();

                        if(removeDuplicates)
                            langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

                        Symbols.Append(ci.Symbol);
                        var translations = langsList.Count > 1 ? string.Join(CI_LANG_SEPARATOR, langsList) : langsList[0];

                        if(i < 5)
                        {
                            careInstructions.Append(translations);
                            careInstructions.Append(CI_SEPARATOR);
                        }
                        else
                        {
                            fullAdditionals.Append(translations);
                            fullAdditionals.Append(CI_SEPARATOR);
                        }
                        
                    }
                    compositionData.Add("FullAdditionals", fullAdditionals.ToString());

                    _orderUtilService.SaveComposition(projectData.ID, compo.ID, compositionData, careInstructions.ToString(), Symbols.ToString());
                }
            }
        }

        private string GenerateCompoAllLangs(bool removeDuplicates, string compoTextAllLangs, Section section, List<string> langsList,bool firstSection)
        {
            var sectionsText = langsList.Count > 1 ? $"{String.Join(SECTION_LANG_SEPARATOR, langsList)}" : langsList[0];
            compoTextAllLangs = compoTextAllLangs + $"{sectionsText}";

            var fibers = section.Fibers != null ? section.Fibers : new List<Fiber>();

            foreach(var fiber in fibers)
            {
                langsList = ExtractTranslations(removeDuplicates, fiber.AllLangs);
                var fibersText = langsList.Count > 1 ? $"{String.Join(FIBER_LANG_SEPARATOR, langsList)}" : langsList[0];
                compoTextAllLangs = compoTextAllLangs + $"{FIBER_SEPARATOR}{fiber.Percentage}% {fibersText}";
            }
            if(sectionsText == "" && firstSection)
                return compoTextAllLangs.Substring(1) + $"{SECTION_SEPARATOR}";

            return compoTextAllLangs + $"{SECTION_SEPARATOR}";
        }

        private string GenerateCompoOneLanguage(string sectionTitle, Section section, string language, string compoText,bool firstSection)
        {
            var fiberText = string.Empty;
            var partialCompoText = string.Empty;

            var fibers = section.Fibers != null ? section.Fibers : new List<Fiber>();

            foreach(var fiber in fibers)
            {
                if(language == "spanish")
                    fiberText = ExtractSpanishTranslations(fiber.AllLangs);
                else
                    fiberText = ExtractEnglishTranslations(fiber.AllLangs);

                partialCompoText = partialCompoText + $"{FIBER_SEPARATOR}{fiber.Percentage}% {fiberText}";

            }

            if(sectionTitle == "" && firstSection)
                return compoText + partialCompoText.Substring(1) + $"{SECTION_SEPARATOR}";

            return compoText + sectionTitle + partialCompoText + $"{SECTION_SEPARATOR}";

           
        }

        private List<string> ExtractTranslations(bool removeDuplicates, string allLangs)
        {
            List<string> langsList = allLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).ToList();
            if(removeDuplicates)
                langsList = langsList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            return langsList;
        }

        private string ExtractSpanishTranslations(string allLangs)
        {
            List<string> langsList = allLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).ToList();
            if(langsList.Count <= 1) return string.Empty;
            return langsList[0];
        }

        private string ExtractEnglishTranslations(string allLangs)
        {
            List<string> langsList = allLangs.Split(OrderUtilService.LANG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries).ToList();

            if(langsList.Count <= 1) return string.Empty;
            return langsList[1];
        }

        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {
        }
    }
}
