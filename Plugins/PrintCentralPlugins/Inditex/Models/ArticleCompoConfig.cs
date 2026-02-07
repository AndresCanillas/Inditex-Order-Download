namespace SmartdotsPlugins.Inditex.Models
{
    public class ArticleCompoConfig
    {
        public float HeightInInches { get; set; }
        public float WidthInches { get; set; } = 0;
        public int LineNumber { get; set; }
        public bool WithSeparatedPercentage = true;
        public int DefaultCompresion = -1;
        public int PPI = 96;
        public bool IsSimpleAdditional = false;
        public float WidthAdditionalInInches = 0;
        public string SelectedLanguage = "English";  
        public int MaxPages = 0; 
    }
}
