using Newtonsoft.Json;
using System.Collections.Generic;

namespace StructureInditexOrderFile
{

    public class InditexOrderData
    {

        public Poinformation POInformation { get; set; }
        public Supplier Supplier { get; set; }
        public Asset[] Assets { get; set; }
        public Componentvalue[] ComponentValues { get; set; }
        public Label[] labels { get; set; }
    }

    public class Poinformation
    {
        [JsonProperty("productionOrderNumber")]
        public string PONumber { get; set; }
        public string Campaign { get; set; }
        public int OrderQty { get; set; }
        [JsonProperty("brand")]
        public string BrandRfid { get; set; }
        [JsonProperty("section")]
        public string SectionRfid { get; set; }
        [JsonProperty("ProductType")]
        public string ProductTypeRfid { get; set; }
        [JsonProperty("model")]
        public int ModelRfid { get; set; }
        [JsonProperty("quality")]
        public int QualityRfid { get; set; }
        public Color[] Colors { get; set; }
    }

    public class Color
    {
        [JsonProperty("color")]
        public int ColorRfid { get; set; }
        public Size[] Sizes { get; set; }
    }

    public class Size
    {
        [JsonProperty("size")]
        public int SizeRfid { get; set; }
        public int Qty { get; set; }
    }

    public class Supplier
    {
        public string SupplierCode { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }

    public class Asset
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class Componentvalue
    {
        public string GroupKey { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public object ValueMap { get; set; }
    }

    public class Label
    {
        public string Reference { get; set; }
        public string[] Assets { get; set; }
        public string[] Components { get; set; }
        public Childrenlabel[] ChildrenLabels { get; set; }
    }

    public class Childrenlabel
    {
        public string Reference { get; set; }
        public string[] Sssets { get; set; }
        public string[] Components { get; set; }
        public object[] ChildrenLabels { get; set; }
    }

}
