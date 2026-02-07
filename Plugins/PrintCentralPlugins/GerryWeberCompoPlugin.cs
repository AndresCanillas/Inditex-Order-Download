using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using System.Collections.Generic;
using WebLink.Services.Wizards.GerryWeber;
using WebLink.Services.Wizards.PuntoRoma;

namespace SmartdotsPlugins.GerryWeber.WizardPlugins
{
    [FriendlyName("GerryWeberCompoPlugin")]
    [Description("Concatenate Gerry Weber Composition Sections and Care Instructions.")]
    public class GerryWeberCompoPlugin : IWizardCompositionPlugin
    {
        private readonly IAppLog log;
        GerryWeberCompositionService gerryWeberCompositionService;

        public GerryWeberCompoPlugin(
            IAppLog log,
            GerryWeberCompositionService gerryWeberCompositionService
            )
        {
            this.gerryWeberCompositionService = gerryWeberCompositionService;
            this.log = log.GetSection("GerryWeberCompoPlugin - CompoPlugin");
        }
        public void GenerateCompositionText(List<OrderPluginData> orderDatas)
        {
            log.LogMessage("Initialise GerryWeberPluginCompo");
            var ordersList = new List<OrderDataDTO>();   
            foreach (OrderPluginData orderData in orderDatas)
            {
                var orderDataDTO = new OrderDataDTO(orderData.OrderGroupID, orderData.OrderID, orderData.ProjectID);
                ordersList.Add(orderDataDTO);
            }

            gerryWeberCompositionService.GenerateCompositionText(ordersList);    

            log.LogMessage("Finish GerryWeberPluginCompo");
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
