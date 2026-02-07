using SmartdotsPlugins.Compostion.Abstractions;
using SmartdotsPlugins.Inditex.Models;
using SmartdotsPlugins.Inditex.Util;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class CareInstructionsBuilderManager : CareInstructionsBuilderBase 
    {
        public override List<CompositionTextDTO> Build(CompositionDefinition compo, Separators separators)
        {
            return base.Build(compo, separators);
        }
    }
}
