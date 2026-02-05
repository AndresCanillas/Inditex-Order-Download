using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.PrintServices.PrintCentral.OrderPlugins
{
    public interface IWizardCompositionPreviewPlugin
    {
        List<PluginCompoPreviewData> GenerateCompoPreview (List<OrderPluginData> orderData);
    }
}
