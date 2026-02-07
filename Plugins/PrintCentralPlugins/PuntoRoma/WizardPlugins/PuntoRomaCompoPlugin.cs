using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using System.Collections.Generic;
using WebLink.Services.Wizards.PuntoRoma;

namespace SmartdotsPlugins.PuntoRoma.WizardPlugins
{
    [FriendlyName("PuntoRomaCompoPlugin")]
    [Description("Concatenate PuntoRoma Composition Sections and Care Instructions.")]
    public class PuntoRomaCompoPlugin : IWizardCompositionPlugin
    {
        private readonly ILogSection log;
        PuntoRomaCompositionService puntoRomaCompositionService;

        public PuntoRomaCompoPlugin(
            ILogService log,
            PuntoRomaCompositionService puntoRomaCompositionService
            )
        {
            this.puntoRomaCompositionService = puntoRomaCompositionService;
            this.log = log.GetSection("PuntoRomaCompoPlugin - CompoPlugin");
        }
        public void GenerateCompositionText(List<OrderPluginData> orderDatas)
        {
            log.LogMessage("Initialise PuntoRomaPluginCompo");
            var ordersList = new List<OrderDataDTO>();   
            foreach (OrderPluginData orderData in orderDatas)
            {
                var orderDataDTO = new OrderDataDTO(orderData.OrderGroupID, orderData.OrderID, orderData.ProjectID);
                ordersList.Add(orderDataDTO);
            }

            puntoRomaCompositionService.GenerateCompositionText(ordersList);    

            log.LogMessage("Finish PuntoRomaPluginCompo");
        }

        public void CloneCompoPreview(OrderPluginData od, int sourceId, Dictionary<string, string> compositionDataSource, List<int> targets) { }

        public void Dispose() { }

        public List<PluginCompoPreviewData> GenerateCompoPreviewData(List<OrderPluginData> orderData, int id, bool isLoad)
        {
            return null;
        }

        public void SaveCompoPreview(OrderPluginData od, PluginCompoPreviewInputData data) { }
    }
}
