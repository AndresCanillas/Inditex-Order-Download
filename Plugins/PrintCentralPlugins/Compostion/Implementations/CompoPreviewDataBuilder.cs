using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using SmartdotsPlugins.Compostion.Abstractions;
using System.Collections.Generic;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class CompoPreviewDataBuilder : CompoPreviewDataBuilderBase
    {
        public CompoPreviewDataBuilder(IOrderUtilService orderUtilService,
                                       IPrinterJobRepository printerJobRepo,
                                       IArticleRepository articleRepo)
                                       : base(orderUtilService, printerJobRepo, articleRepo)
        {
        }

        public override void SetCustomServices(ArticleCodeExtractor articleCodeExtractor, ArticleCompositionConfigurationBase articleCompositionConfiguration, InditexLanguageDictionaryManagerBase inditexLanguageDictionaryManager, FiberListBuilderBase fiberListBuilder, SeparatorsInitBase separatorsInitializator, CareInstructionsBuilderBase careInstructionsBuilder)
        {
            base.SetCustomServices(articleCodeExtractor, articleCompositionConfiguration, inditexLanguageDictionaryManager, fiberListBuilder, separatorsInitializator, careInstructionsBuilder);
        }

        public override List<PluginCompoPreviewData> Generate(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return base.Generate(orderData, id, isLoad);
        }
    }

}
