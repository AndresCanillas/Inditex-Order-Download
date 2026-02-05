using System.Collections.Generic;

namespace Service.Contracts.PrintServices.Plugins
{
    public  class PluginCompositionTextPreviewData
    {
            public string Percent { get; set; }
            public string Text { get; set; }
            public string FiberType { get; set; } // Textile, Synthetic, Leather
            public List<string> Langs { get; set; }
        public bool IsTitle { get; set; }
        public List<string> SectionFibersText { get; set; }

    }
}
