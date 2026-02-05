using Service.Contracts.PrintServices.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.PrintCentral
{
	public interface IWizardCompositionPlugin : IDisposable
	{

        void GenerateCompositionText(List<OrderPluginData> orderData);
        List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id,   bool isLoad);
        void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data);
        void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets);

    }
}
