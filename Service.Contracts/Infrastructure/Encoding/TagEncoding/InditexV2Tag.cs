using System;
using System.Collections.Generic;
using System.Text;




/*
 * Specification
 * ==========================
 * 
 * This is a customer defined encoding scheme used exclusively for brands and companies
 * afiliated to Inditex Spain. 
 * 
 * Internal Structure
 * 
 *  https://indetgroup.sharepoint.com/:f:/s/support77/EssOSjFKZC1Cgkw2tLImno4BF67OnQddhJD0hvX6y3hA8g?e=YGc1SU
 *	  
 * URN format
 * ==========================
 * 
 *  In this case the URN is not standards based, it simply allows to quickly see the most important information in the tag at a glance.
 *  
 *		"urn:tempe:v1:Brand.PType.MCCT.SerialNumber"
 *		
 * Example:
 *		"urn:tempe:v1:1.0.122100102002.2157865"
 */
namespace Service.Contracts
{
    public class InditexV2Tag : TagEncoding, ITagEncoding
    {
        public override int TypeID
        {
            get { return 14; }
        }
        public override string Name
        {
            get { return "InditexV2"; }
        }
        public override string UrnNameSpace
        {
            get { return "urn:tempe:v1:Brand.ProdType.MCCT.SerialNumber"; }
        }
        protected override string UrnPattern
        {
            get { return @"^urn:tempe:v1:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){4}"; }
        }

        public InditexV2Tag()
        {
            fields = new BitFieldInfo[]{
                new BitFieldInfo("Version",             123,  5,  BitFieldFormat.Decimal, false, true, "2", null),
                new BitFieldInfo("BrandId",             117,  6,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("Section",             115,  2,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("ProductType",         111,  4,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("ActFlag",             110,  1,  BitFieldFormat.Decimal, false, false, "1", null),
                new BitFieldInfo("EncodeCheck",         104,  6,  BitFieldFormat.Decimal, false, false, "0", null),
                new BitFieldInfo("InventoryTAG",        103,  1,  BitFieldFormat.Decimal, false, false, "1", null),
                new BitFieldInfo("IdSupplier",           97,  6,  BitFieldFormat.Decimal, false, false, "4", null),
                new BitFieldInfo("Free",                 88,  9,  BitFieldFormat.Decimal, false, false, "0", null),
                new BitFieldInfo("S",                    81,  7,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("C",                    71, 10,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("Q",                    61, 10,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("M",                    47, 14,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("ProductComposition",   44,  3,  BitFieldFormat.Decimal, false, false, "0", null),
                new BitFieldInfo("TagType",              39,  5,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("TagSubType",           32,  7,  BitFieldFormat.Decimal, false, false, null, null),
                new BitFieldInfo("SerialNumber",          0, 32, BitFieldFormat.Decimal, false, false, null, null),


            };
        }

        public override string ToString()
        {
            return $"{Name}:{this["Section"]}.{this["BrandId"]}.{this["ProductType"]} {GetMCCT()} {this["SerialNumber"]}";
        }


        public void SetMCCT(int model, int quality, int color, int size)
        {
            
            fields[9].Value = size.ToString("D2");
            fields[10].Value = color.ToString("D3");
            fields[11].Value = quality.ToString("D3");
            fields[12].Value = model.ToString("D4");
        }

        /// <summary>
        /// Return MCCT string -> Modelo, Calida, Color, Talla 
        /// </summary>
        /// <returns></returns>
        public string GetMCCT()
        {
            long model = Int32.Parse(fields[12].Value);
            long quality = Int32.Parse(fields[11].Value);
            long color = Int32.Parse(fields[10].Value);
            long size = Int32.Parse(fields[9].Value);

            return $"{model.ToString("D4")}/{quality.ToString("D3")}/{color.ToString("D3")}/{size.ToString("D2")}";
        }
    }
}
