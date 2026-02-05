using System.Collections.Generic;

namespace StructureMangoOrderFileColor
{
    public class Itemdata
    {
        public string itemQty { get; set; }
        public string EAN13 { get; set; }
        public string Material { get; set; }
        public string COLOR { get; set; }
        public string Phase_IN { get; set; }
        public string MangoColorCode { get; set; }
        public string MangoSizeCode { get; set; }
        public string MangoSAPSizeCode { get; set; }
        public string SizeName { get; set; }
        public string SizeNameES { get; set; }
        public string SizeNameFR { get; set; }
        public string SizeNameDE { get; set; }
        public string SizeNameIT { get; set; }
        public string SizeNameUK { get; set; }
        public string SizeNameUS { get; set; }
        public string SizeNameMX { get; set; }
        public string SizeNameCN { get; set; }
        public string FillingWeight { get; set; }
        public string stocksegment { get; set; }
        public string PVP { get; set; }
        public string CURRENCY { get; set; }
        public string PVP_ES { get; set; }
        public string CURRENCY_ES { get; set; }
        public string PVP_EU { get; set; }
        public string CURRENCY_EU { get; set; }
        public string PVP_IN { get; set; }
        public string CURRENCY_IN { get; set; }
        public Sizepack SizePack { get; set; }
        public List<Garmentmeasurement> GarmentMeasurements { get; set; }
        public List<Sizedescription> SizeDescriptions { get; set; }
    }
}
