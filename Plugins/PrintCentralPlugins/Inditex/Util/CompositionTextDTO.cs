using System.Collections.Generic;

namespace SmartdotsPlugins.Inditex.Util
{
    public class CompositionTextDTO
    {
        public string Percent { get; set; }
        public string Text { get; set; }
        public string FiberType { get; set; } // Textile, Synthetic, Leather
        public TextType TextType { get; set; }
        public List<string> Langs { get; set; }
        public List<string> SectionFibersText { get; set; }
        public List<string> SectionFibersTextSelectedLanguage { get; set; } 
        public string TextSelectedLanguage { get; set; }    
    }
}
