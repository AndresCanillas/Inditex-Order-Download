using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Inditex.Models;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class AdditionalsCompoManager : AdditionalsCompoManagerBase
    {
        public AdditionalsCompoManager(IOrderUtilService orderUtilService) : base(orderUtilService)
        {
        }
        public override void GenerateAdditional(ArticleCompoConfig article,
                                                CompositionDefinition compo,
                                                StringBuilder careInstructions,
                                                StringBuilder additionals,
                                                StringBuilder Symbols,
                                                Dictionary<string, string> compositionData,
                                                int projectId,
                                                int compoId,
                                                int totalPages,
                                                int allowedLinesByPage,
                                                Separators separators)
        {
            base.GenerateAdditional(article, compo, careInstructions, additionals, Symbols, compositionData, projectId, compoId, totalPages, allowedLinesByPage, separators);
        }
    }
}
