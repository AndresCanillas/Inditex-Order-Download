using System.Collections.Generic;

namespace StructureMangoOrderFileColor
{

    public class MangoOrderData
    {
        public LabelOrder LabelOrder { get; set; }
        public Supplier Supplier { get; set; }
        public List<StyleColor> StyleColor { get; set; }
        public List<Prepackdata> PrepackData { get; set; }
       
    }


}
