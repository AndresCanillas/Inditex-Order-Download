using Service.Contracts.PrintCentral;
using Services.Core;
using SmartdotsPlugins.Inditex.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services;

namespace SmartdotsPlugins.Compostion.Abstractions
{
    public abstract class CompositionCloneManagerBase
    {
        private IOrderUtilService orderUtilService;
        private InditexLanguageDictionaryManagerBase inditexLanguageDictionaryManager;
        private SeparatorsInitBase separatorsInit;
        private ArticleCodeExtractorBase articleCodeExtractor;
        private SymbolsBuilderBase symbolsBuilder;
        private ILogService log;



        private Separators Separators { get; set; }
        protected CompositionCloneManagerBase(IOrderUtilService orderUtilService, ILogService log)
        {
            this.orderUtilService = orderUtilService;
            this.log = log;
        }

        public virtual void SetCustomServices(InditexLanguageDictionaryManagerBase inditexLanguageDictionaryManager,
                                              SeparatorsInitBase separatorsInit,
                                              ArticleCodeExtractorBase articleCodeExtractor,
                                              SymbolsBuilderBase symbolsBuilder)
        {
            this.inditexLanguageDictionaryManager = inditexLanguageDictionaryManager;
            this.separatorsInit = separatorsInit;
            this.articleCodeExtractor = articleCodeExtractor;
            this.symbolsBuilder = symbolsBuilder;
        }

        public virtual void Clone(OrderPluginData od,
                                 int sourceId,
                                 Dictionary<string, string> compositionDataSource,
                                 List<int> targets)
        {
            CompositionDefinition compo = new CompositionDefinition();
            var projectData = orderUtilService.GetProjectById(od.ProjectID);
            var careInstructions = new StringBuilder();
            var Symbols = new StringBuilder();
            var Language = inditexLanguageDictionaryManager.GetInditexLanguageDictionary();
            Separators = separatorsInit.Init(projectData);
            string artCode = articleCodeExtractor.Extract(od.OrderID);

            var compositions = orderUtilService.GetComposition(od.OrderGroupID, true, Language, OrderUtilService.LANG_SEPARATOR);
            var compoSource = compositions.FirstOrDefault(c => c.ID == sourceId);
            if(compoSource == null)
            {
                return;
            }

            foreach(var composition in compositions)
            {
                if(targets.Any(t => t == composition.ID))
                {
                    List<OrderPluginData> orderData = new List<OrderPluginData> { od };
                    TargetCompositionMapping(compoSource, composition);
                    compositionDataSource["ID"] = composition.ID.ToString();
                    symbolsBuilder.Build(composition, Symbols, compositionDataSource, Separators);
                    var cleanedCi = Regex.Replace(careInstructions.ToString().Trim(), Separators.CI_SEPARATOR + "$", string.Empty);// PERSONALIZED TRIM END
                    if(!compositionDataSource.TryGetValue("Symbols", out string symbols) && string.IsNullOrEmpty(Symbols.ToString().Trim()))
                    {
                        throw new System.Exception($"Cloning Composition Symbols not found Project {orderData[0]} CompositionID = {composition.ID}");
                    }
                    var symbolsArray = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                    if(symbolsArray.Count() < 5)
                    {
                        throw new System.Exception($"Cloning Composition There are only {symbolsArray.Count()} symbols Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {composition.ID} ");
                    }
                    if(!compositionDataSource.TryGetValue("Page1_compo", out string page1_compo))
                    {
                        throw new System.Exception($"Cloning Composition Page1_compo not found Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {composition.ID}");
                    }

                    if(string.IsNullOrEmpty(page1_compo))
                    {
                        throw new System.Exception($"Cloning Composition Page1_compo is empty Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {composition.ID}");
                    }

                    int total_addcare_pages = 0;
                    if(compositionDataSource.TryGetValue("AdditionalsNumber", out string additionalnumber))
                        total_addcare_pages = int.Parse(additionalnumber);
                    else
                        compositionDataSource.TryAdd("AdditionalsNumber", "0");
                    compositionDataSource.TryAdd("OrderID", composition.OrderID.ToString());
                    orderUtilService.SaveComposition(orderData[0].ProjectID, composition.ID, compositionDataSource, cleanedCi, Symbols.ToString());
                    var checkComposition = orderUtilService.GetCompositionData(orderData[0].ProjectID, composition.ID);
                    if (checkComposition.TryGetValue("Symbols", out string symbolsCheck))
                    {
                        log.LogWarning($"Cloning Composition Symbols saved: {symbolsCheck} Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {composition.ID} "); 
                    }
                    else
                    {
                         throw new Exception ($"Cloning Composition Symbols saved: NotFound  Project {orderData[0].ProjectID} OrderNumber = {orderData[0].OrderNumber} CompositionID = {composition.ID}");
                    }   

                }
            }
        }

        private static void TargetCompositionMapping(CompositionDefinition compoSource, CompositionDefinition composition)
        {
            composition.CareInstructions = compoSource.CareInstructions;
            composition.Sections = compoSource.Sections;
            composition.ArticleCode = compoSource.ArticleCode;
            composition.EnableComposition = compoSource.EnableComposition;
            composition.Product = compoSource.Product;
            composition.OrderID = compoSource.OrderID;
            composition.EnableExceptions = compoSource.EnableExceptions;
            composition.ArticleID = compoSource.ArticleID;
            composition.TargetArticle = compoSource.TargetArticle;
        }
    }
}
