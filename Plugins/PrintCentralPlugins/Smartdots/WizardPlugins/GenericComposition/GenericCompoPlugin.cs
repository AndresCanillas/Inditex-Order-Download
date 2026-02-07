using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.WizardPlugins
{
	[FriendlyName("Smartdots - Generic Composition Text Plugin")]
	[Description("Concatenate Composition Sections and Care Instructions.")]
	public class GenericCompoPlugin: IWizardCompositionPlugin
	{
		private IEventQueue events;
		private ILogSection log;
        private IOrderUtilService orderUtilService;


        public GenericCompoPlugin(IEventQueue events, ILogService log, IOrderUtilService orderUtilService)
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


            foreach (var od in orderData)
            {
                var composition = orderUtilService.GetComposition(od.OrderGroupID);

                foreach (var c in composition)
                {
                    //add composition
                    var compo = string.Empty;

                    foreach (var s in c.Sections)
                    {
                        var langsList = s.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        compo += langsList.Length > 1 ? String.Join(sectionLanguageSeparator,langsList) : langsList[0];
                        compo += sectionSeparator;

                        var fibers = s.Fibers != null ? s.Fibers : new List<Fiber>();

                        foreach (var f in s.Fibers)
                        {
                            langsList = f.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            compo += $"{ f.Percentage }% " + (langsList.Length > 1 ?  $" {String.Join(fiberLanguageSeparator, langsList)} " : langsList[0]);
                            compo += fibersSeparator;
                        }

                    }

                    //add care instructions
                    var careInstructions = string.Empty;
                    var onlyAdditionals = string.Empty;
                    var onlyExceptions = string.Empty;
                    var onlyMainCareInstructions = string.Empty;
                    StringBuilder symbols = new StringBuilder(string.Empty, 10);
                    

                    foreach (var ci in c.CareInstructions)
                    {
                        var langsList = ci.AllLangs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        symbols.Append(ci.Symbol);

                        var languangeString = String.Join(ciLanguageSeparator, langsList);

                        careInstructions += langsList.Length > 1 ? languangeString : langsList[0];
                        careInstructions += $" { ciSeparator } ";

                        if (ci.Category == CareInstructionCategory.ADDITIONAL)
                        {
                            onlyAdditionals += langsList.Length > 1 ? languangeString : langsList[0];
                            onlyAdditionals += $" {ciSeparator} ";
                        }else if(ci.Category == CareInstructionCategory.EXCEPTION)
                        {
                            onlyExceptions += langsList.Length > 1 ? languangeString : langsList[0];
                            onlyExceptions += $" {ciSeparator} ";
                        }else
                        {
                            onlyMainCareInstructions += langsList.Length > 1 ? languangeString : langsList[0];
                            onlyMainCareInstructions += $" {ciSeparator} ";
                        }
                    }

                    log.LogMessage($"Save Generic Compo for OrderGroupID: {od.OrderGroupID}, ( CompositionLabelID: {c.ID} )");

                    //orderUtilService.SaveComposition(orderData[0].ProjectID, c.ID, compo, careInstructions,symbols.ToString());

                    var compoData = new Dictionary<string, string>
                    {
                        { "FullComposition", compo },
                        { "FullCareInstructions", careInstructions },
                        { "Symbols", symbols.ToString() },
                        // new fields, save method from DynamicDB will be ignore it if CompositionLabel table don't have fields 
                        { "AdditionalsCareInstructions", onlyAdditionals },
                        { "ExceptionsCareInstructions", onlyAdditionals },
                        { "MainCareInstructions", onlyAdditionals }
                    };

                    orderUtilService.SaveComposition(orderData[0].ProjectID, c.ID, compoData, string.Empty, string.Empty);
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
        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {
        }
        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {
        }

    }
}
