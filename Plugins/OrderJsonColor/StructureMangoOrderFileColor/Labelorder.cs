using Newtonsoft.Json;

namespace StructureMangoOrderFileColor
{
    public class LabelOrder
    {
        [JsonProperty("Id")]
        public string LabelOrderId { get; set; }
        public string Temporada { get; set; }
        public string OrderQty { get; set; }
        public string TypePO { get; set; }
        public string TypePOdesc { get; set; }
        public string Status { get; set; }
        public string ProductionDate { get; set; }
        public string Version { get; set; }
        public string TimeStamp { get; set; }
        public string Idoc { get; set; }
    }


}
