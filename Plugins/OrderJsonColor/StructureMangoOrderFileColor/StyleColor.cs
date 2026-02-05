
using Newtonsoft.Json;
using StructureMangoOrderFileColor;
using System.Collections.Generic;
namespace StructureMangoOrderFileColor
{
    public class StyleColor
    {
        public string ReferenceID { get; set; }
        public string StyleID { get; set; }
        public string MangoColorCode { get; set; }
        public string Color { get; set; }
        public string GenericMaterial { get; set; }
        public string Line { get; set; }
        public string Age { get; set; }
        public string Gender { get; set; }
        public string Packaging { get; set; }
        public string GenName { get; set; }
        public string Generic { get; set; }
        public string FAMILYID { get; set; }
        public string FAMILY { get; set; }
        public string ProductTypeCode { get; set; }
        public string ProductTypeCodeLegacy { get; set; }
        public string ProductType { get; set; }
        public string ProductTypeES { get; set; }
        public string RFIDMark { get; set; }
        public string SizeGroupLegay { get; set; }
        public string Iconic { get; set; }
        public string MinLegalData { get; set; }
        [JsonProperty("Set")]
        public string StyleColorSet { get; set; }
        public Destination Destination { get; set; }
        public Origin Origin { get; set; }
        public List<Itemdata> ItemData { get; set; }
        public List<Labeldata> LabelData { get; set; }
        public List<Composition> Composition { get; set; }
        public List<Careinstruction> CareInstructions { get; set; }
        public List<Sizerange> SizeRange { get; set; }
        [JsonProperty("PVP")]
        public List<PVP> Pvps { get; set; }
    }
}