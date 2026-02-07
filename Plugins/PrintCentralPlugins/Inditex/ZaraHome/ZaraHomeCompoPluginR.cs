using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Compostion.Implementations;
using System.Collections.Generic;

namespace SmartdotsPlugins.Inditex.ZaraHome
{
    [FriendlyName("ZaraHomeR - Composition Text Plugin")]
    [Description("Inditex.ZaraHomeR - Composition Plugin")]
    public class ZaraCompoHomePluginR : IWizardCompositionPlugin
    {
        private CompoPreviewDataBuilderBase compoPreviewDataBuilder;
        private CompositionRepositoryBase compositionRepository;
        private CompositionCloneManagerBase compositionCloneManager;
        private ZaraArticleCalculator zaraArticleCalculator;
        private IFactory factory;

        public ZaraCompoHomePluginR(IFactory factory)
        {
            this.factory = factory;
            this.compoPreviewDataBuilder = factory.GetInstance<CompoPreviewDataBuilder>();  
            this.compositionRepository = factory.GetInstance<CompositionRepository>();   
            this.compositionCloneManager = factory.GetInstance<CompositionCloneManager>();   
            ConfigureCustomServices();
        }

        private void ConfigureCustomServices()
        {
            this.compoPreviewDataBuilder.SetCustomServices(articleCodeExtractor: factory.GetInstance<ArticleCodeExtractor>(),
                                                          articleCompositionConfiguration: factory.GetInstance<ArticleCompositionConfigManager>(),
                                                          inditexLanguageDictionaryManager: factory.GetInstance<ZaraHomeLanguageDictionaryManager>(),
                                                          fiberListBuilder: factory.GetInstance<FiberListBuilder>(),
                                                          separatorsInitializator: factory.GetInstance<SeparatorsInit>(),
                                                          careInstructionsBuilder: factory.GetInstance<CareInstructionsBuilderBase>());
            this.compositionRepository.SetCustomServices(separatorsInitializator: factory.GetInstance<SeparatorsInit>(),
                                                            artcleCodeExtractor: factory.GetInstance<ArticleCodeExtractor>(),
                                                            languageDictionaryManager: factory.GetInstance<ZaraHomeLanguageDictionaryManager>(),
                                                            symbolsBuilder: factory.GetInstance<SymbolsBuilder>(),
                                                            additionalsCompoManager: factory.GetInstance<AdditionalsCompoManager>(),
                                                            articleCalculator: factory.GetInstance<ArticleCalculatorManager>(), 
                                                            articleCompositionConfiguration: factory.GetInstance<ArticleCompositionConfigManager>()); 
            this.compositionCloneManager.SetCustomServices(inditexLanguageDictionaryManager: factory.GetInstance<ZaraHomeLanguageDictionaryManager>(),
                                                              separatorsInit: factory.GetInstance<SeparatorsInit>(),      
                                                              articleCodeExtractor: factory.GetInstance<ArticleCodeExtractor>(),
                                                              symbolsBuilder: factory.GetInstance<SymbolsBuilder>());

        }

        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {
            compositionCloneManager.Clone(od, sourceId, compositionDataSource, targets); 
        }

        public void Dispose()
        {
            
        }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return  compoPreviewDataBuilder.Generate(orderData, id, isLoad);    
        }

        public void GenerateCompositionText(List<OrderPluginData> orderData)
        {
            compoPreviewDataBuilder.Generate(orderData, 0, false);
        }

        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data)
        {
        
            compositionRepository.Save(od, data); 
        }
    }
}
