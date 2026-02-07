using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Compostion.Implementations;
using SmartdotsPlugins.Tempe.CompoPlugin.Implementations;
using System.Collections.Generic;

namespace SmartdotsPlugins.Tempe.CompoPlugin
{
    [FriendlyName("Accesories - Composition Text Plugin")]
    [Description("Tempe.Accesories - Composition Plugin")]
    public class MassimoDuttiCompoPluginR : IWizardCompositionPlugin
    {
        private CompoPreviewDataBuilderBase compoPreviewDataBuilder;
        private CompositionRepositoryBase compositionRepository;
        private CompositionCloneManagerBase compositionCloneManager;
        private ZaraArticleCalculator zaraArticleCalculator;
        private IFactory factory;

        public MassimoDuttiCompoPluginR(IFactory factory)
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
                                                          inditexLanguageDictionaryManager: factory.GetInstance<InditexLanguageDictionaryManager>(),
                                                          fiberListBuilder: factory.GetInstance<TempeFiberListBuilder>(),
                                                          separatorsInitializator: factory.GetInstance<SeparatorsInit>(),
                                                          careInstructionsBuilder: factory.GetInstance<TempeCareInstructionsBuilder>());
            this.compositionRepository.SetCustomServices(separatorsInitializator: factory.GetInstance<SeparatorsInit>(),
                                                            artcleCodeExtractor: factory.GetInstance<ArticleCodeExtractor>(),
                                                            languageDictionaryManager: factory.GetInstance<InditexLanguageDictionaryManager>(),
                                                            symbolsBuilder: factory.GetInstance<TempeSymbolsBuilder>(),
                                                            additionalsCompoManager: factory.GetInstance<AdditionalsCompoManager>(),
                                                            articleCalculator: factory.GetInstance<ArticleCalculatorManager>(),
                                                            articleCompositionConfiguration: factory.GetInstance<ArticleCompositionConfigManager>());
            this.compositionCloneManager.SetCustomServices(inditexLanguageDictionaryManager: factory.GetInstance<InditexLanguageDictionaryManager>(),
                                                              separatorsInit: factory.GetInstance<SeparatorsInit>(),
                                                              articleCodeExtractor: factory.GetInstance<ArticleCodeExtractor>(),
                                                              symbolsBuilder: factory.GetInstance<TempeSymbolsBuilder>());

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
            return compoPreviewDataBuilder.Generate(orderData, id, isLoad);
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
