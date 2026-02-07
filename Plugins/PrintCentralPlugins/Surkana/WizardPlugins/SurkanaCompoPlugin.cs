using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.OrderPlugins
{
    [FriendlyName("Surkana - Composition Text Plugin")]
    [Description("Surkana - Composition WizardPlugin")]
    public class SurkanaCompoPlugin : IWizardCompositionPlugin
    {
        private IEventQueue events;
        private ILogSection log;
        private IOrderUtilService orderUtilService;

        

        public SurkanaCompoPlugin(IEventQueue events, ILogService log, IOrderUtilService orderUtilService)
        {
            this.events = events;
            this.log = log.GetSection("Smartdots-GenericCompoPlugin");
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


            string[] surkanaLangs = { "Spanish", "English", "Italian", "French", "Portuguese", "Japanese", "Czech", "German", "Slovak", "Greek" };

            string[] SectionsLanguage = { "ShortName" };
            //string[] FibersLanguage = { "Spanish", "English", "Italian", "French", "Portuguese", "Japanese", "Czech", "German", "Slovak", "Greek" };

            //string[] AdditionalsLanguage = { "Spanish", "English", "Italian", "French", "Portuguese", "Japanese", "Czech", "German", "Slovak", "Greek" };
            //string[] ExceptionsLanguage = { "Spanish", "English", "Italian", "French", "Portuguese", "Japanese", "Czech", "German", "Slovak", "Greek" };

            //string[] CareInstructionsLanguage = { "Spanish", "English", "Italian", "French", "Portuguese", "Japanese", "Czech", "German", "Slovak", "Greek" };


            Dictionary<CompoCatalogName, IEnumerable<string>> languages = new Dictionary<CompoCatalogName, IEnumerable<string>>();
            languages.Add(CompoCatalogName.SECTIONS, SectionsLanguage);
            languages.Add(CompoCatalogName.FIBERS, surkanaLangs);
            languages.Add(CompoCatalogName.CAREINSTRUCTIONS, surkanaLangs);

            foreach (var od in orderData)
            {
                var composition = orderUtilService.GetComposition(od.OrderGroupID, true, languages);

                foreach (var c in composition)
                {
                    //add composition
                    var compo = string.Empty;

                    foreach (var s in c.Sections)
                    {
                        var langsList = s.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        if (c.Sections.First().ID == s.ID) // the first
                            langsList = (new List<string>()).ToArray();// no show the first title

                         if (c.Sections.First().ID != s.ID) // not first
                            compo += sectionSeparator;

                        compo += langsList.Length < 1 ? string.Empty : String.Join(sectionLanguageSeparator, langsList);

                       

                        //foreach (var f in s.Fibers)
                        for(var j = 0; j < s.Fibers.Count; j++)
                        {
                            var f = s.Fibers[j];
                            langsList = f.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            compo += $"{f.Percentage}% " + (langsList.Length > 1 ? $" {String.Join(fiberLanguageSeparator, langsList)} " : langsList[0]);
                            if (j < s.Fibers.Count - 1) // is last
                            {
                                compo += fibersSeparator;
                            }
                        }


                    }

                    //add care instructions
                    var careInstructions = string.Empty;
                    StringBuilder symbols = new StringBuilder(string.Empty, 10);

                    foreach (var ci in c.CareInstructions)
                    {
                        var langsList = ci.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        symbols.Append(ci.Symbol);

                        var languangeString = String.Join(ciLanguageSeparator, langsList);

                        careInstructions += langsList.Length > 1 ? languangeString : langsList[0];
                        careInstructions += $" {ciSeparator} ";
                    }

                    log.LogMessage($"Save Surkana for OrderGroupID: {od.OrderGroupID}, ( CompositionLabelID: {c.ID} )");

                    orderUtilService.SaveComposition(orderData[0].ProjectID, c.ID, compo, careInstructions, symbols.ToString());
                }

            }
        }

        public void Dispose()
        {
            
        }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return null;
        }
        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        { 
        }
        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {
        }
    }
}
