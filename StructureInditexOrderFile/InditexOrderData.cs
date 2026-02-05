using System.Collections.Generic;

namespace StructureInditexOrderFile
{

    public class InditexOrderData
    {
        public Poinformation POInformation { get; set; }
        public Supplier supplier { get; set; }
        public Asset[] assets { get; set; }
        public Componentvalue[] componentValues { get; set; }
        public Label[] labels { get; set; }
    }

    public class Poinformation
    {
        public string productionOrderNumber { get; set; }
        public string campaign { get; set; }
        public int orderQty { get; set; }
        public string brand { get; set; }
        public string section { get; set; }
        public string productType { get; set; }
        public int model { get; set; }
        public int quality { get; set; }
        public Color[] colors { get; set; }
    }

    public class Color
    {
        public int color { get; set; }
        public Size[] sizes { get; set; }
    }

    public class Size
    {
        public int size { get; set; }
        public int qty { get; set; }
    }

    public class Supplier
    {
        public string supplierCode { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string phoneNumber { get; set; }
        public string email { get; set; }
    }

    public class Asset
    {
        public string name { get; set; }
        public string type { get; set; }
        public string value { get; set; }
    }

    public class Componentvalue
    {
        public string groupKey { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public object valueMap { get; set; }
    }

    public class Label
    {
        public string reference { get; set; }
        public string[] assets { get; set; }
        public string[] components { get; set; }
        public Childrenlabel[] childrenLabels { get; set; }
    }

    public class Childrenlabel
    {
        public string reference { get; set; }
        public string[] components { get; set; }
        public object[] childrenLabels { get; set; }
    }

}
