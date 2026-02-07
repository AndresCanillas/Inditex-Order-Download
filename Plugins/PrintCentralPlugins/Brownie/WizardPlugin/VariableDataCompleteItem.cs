using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;

namespace SmartdotsPlugins.Brownie.WizardPlugin
{

    // THIS CLASS HAS A DUPLICATED IN : Workflows/OrderProcessingPlugins/Brownie/VariableDataCompleteItem.cs
    public class VariableDataCompleteItem
    {
        public int ID { get; set; }
        public string Barcode { get; set; }
        public string TXT1 { get; set; }
        public string TXT2 { get; set;}
        public string TXT3 { get; set;}
        public string Size { get; set; }
        public string Color { get; set; }
        public string Price { get; set; }
        public string Currency { get; set; }
        public int? HasComposition { get; set; }
        public string Brand { get; set; }
        public string ArticleDescription { get; set;}
        public string ColorDescription { get; set;}
        public string SizeDescription { get; set;}
        public int? IsBaseData { get; set;}
        public int? MadeIn { get; set; }
        public string FullMadeIn { get;}
        public string CIF { get; set; }
        public string ArticleCode { get; set; }
        public string ClientArticle { get; set; }

    }
}
