using Newtonsoft.Json;

namespace StructureMangoOrderFileColor
{
    public class Sizedescription
    {
        [JsonProperty("Id")]
        public string SizeId { get; set; }
        public string DescriptionId { get; set; }
        public string Country { get; set; }
        public string SizeDescription { get; set; }
    }


}
