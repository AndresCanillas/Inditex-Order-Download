using Newtonsoft.Json;

namespace OrderDonwLoadService.Model
{
    public class LabelOrderRequest
    {
        [JsonProperty("productionOrderNumber")]
        public string ProductionOrderNumber { get; set; }

        [JsonProperty("campaign")]
        public string Campaign { get; set; }

        [JsonProperty("supplierCode")]
        public string SupplierCode { get; set; }
    }
}
