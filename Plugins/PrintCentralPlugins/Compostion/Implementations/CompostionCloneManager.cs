using Service.Contracts.PrintCentral;
using Services.Core;
using SmartdotsPlugins.Compostion.Abstractions;
using System.Collections.Generic;
using WebLink.Contracts;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class CompositionCloneManager : CompositionCloneManagerBase
    {
        public CompositionCloneManager(IOrderUtilService orderUtilService, ILogService log) : base(orderUtilService, log)
        {
        }

        public override void SetCustomServices(InditexLanguageDictionaryManagerBase inditexLanguageDictionaryManager, SeparatorsInitBase separatorsInit, ArticleCodeExtractorBase articleCodeExtractor, SymbolsBuilderBase symbolsBuilder)
        {
            base.SetCustomServices(inditexLanguageDictionaryManager, separatorsInit, articleCodeExtractor, symbolsBuilder);
        }
        public override void Clone(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets)
        {
            base.Clone(od, sourceId, compositionDataSource, targets);
        }
    }
}
