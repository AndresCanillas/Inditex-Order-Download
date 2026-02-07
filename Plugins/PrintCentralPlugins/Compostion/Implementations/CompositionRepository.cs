using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using SmartdotsPlugins.Compostion.Abstractions;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public sealed class CompositionRepository : CompositionRepositoryBase
    {
        public CompositionRepository(IOrderUtilService orderUtilService,
                                    IPrinterJobRepository printerJobRepo,
                                    ILogService log,
                                    IArticleRepository articleRepo,
                                    IFactory factory,
                                    ICatalogRepository catalogRepo,
                                    IDBConnectionManager connManager)
                                   : base(orderUtilService, printerJobRepo, log, articleRepo, factory, catalogRepo, connManager)
        {
        }

        public override void SetCustomServices(SeparatorsInitBase separatorsInitializator, ArticleCodeExtractorBase artcleCodeExtractor, InditexLanguageDictionaryManagerBase languageDictionaryManager, SymbolsBuilderBase symbolsBuilder, AdditionalsCompoManagerBase additionalsCompoManager, ArticleCalculatorBase articleCalculator, ArticleCompositionConfigurationBase articleCompositionConfig)
        {
            base.SetCustomServices(separatorsInitializator, artcleCodeExtractor, languageDictionaryManager, symbolsBuilder, additionalsCompoManager, articleCalculator, articleCompositionConfig);
        }
        public override void Save(OrderPluginData od, PluginCompoPreviewInputData data)
        {
            base.Save(od, data);
        }
    }
}
