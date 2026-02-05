
using System.Collections.Generic;


namespace Service.Contracts.PrintServices.Plugins
{
    public class PluginCompoPreviewData
    {
        public Dictionary<string,string> CompoData { get; set; }
        
        public string Symbols { get; set; }
        public string CareInstructions { get; set; }
        public int Lines { get; set; }
        public float Width { get; set; }
        public float Heigth { get; set; }

        public float WidthAdditional { get; set; }
        public int AdditionalsCompress { get; set; } = 0;
        public int FiberCompress { get; set; } = 0;
        public bool WithSeparatedPercentage { get; set; } = true;

        public List<PluginCompositionTextPreviewData> CompositionText { get; set; }
        public List<PluginCompositionTextPreviewData> CareInstructionsText { get; set; }
        
        public int PPIValue { get; set; }   

        public string FibersInSpecificLang { get; set; }

        public int ExceptionsLocation { get; set; } = 0;

        public int ArticleID { get; set; } = 0; 

        


    }
}
