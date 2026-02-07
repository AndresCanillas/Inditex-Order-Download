using System;
using System.Collections.Generic;
using System.Text;

namespace SmartdotsPlugins.Brownie.WizardPlugin
{
    public class ArticleDto : ICloneable
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string CIF { get; set; }
        public string LabelColor { get; set; }
        public string ArticleCode { get; set; }
        public int? HasComposition { get; set; }
        public string Brand { get; set; }
        public string Supplier { get; set; }
        public string ColorCode { get; set; }
        public int? IsMadeIn { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? OrderGroupID { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
