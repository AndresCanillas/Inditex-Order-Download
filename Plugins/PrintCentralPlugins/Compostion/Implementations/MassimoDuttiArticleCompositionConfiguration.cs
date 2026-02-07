using SmartdotsPlugins.Inditex.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class MassimoDuttiArticleCompositionConfiguration
    {

        public ArticleCompoConfig Retrieve(string artCode)
        {
            float heightInInches = 0;
            float widthInInches = 0;
            float widthAdditionalInInches = 0;
            int lineNumber = 0;
            bool withSeparatedPercentage = true;
            bool IsSimpleAdditional = false;
            int ppivalue = 0;
            string SelectedLanguage = "English";
            int defaultCompression = 0;




            switch(artCode)
            {
                case "COMPO-110x25-BLACK":
                case "COMPO-110x25-WHITE":

                    lineNumber = 9;
                    widthInInches = 3.6466f;
                    heightInInches = 0.92756f;
                    withSeparatedPercentage = false;
                    ppivalue = 96;
                    SelectedLanguage = "English";
                    break;

                case "COMPO-60x40-BLACK":
                case "COMPO-60x40-WHITE":


                    lineNumber = 15;
                    widthInInches = 1.9732f;
                    heightInInches = 1.4350f;
                    ppivalue = 96;
                    widthAdditionalInInches = 1.895f;
                    SelectedLanguage = "English";
                    IsSimpleAdditional = false;
                    break;

                case "24MDC-PP-CARE":

                    widthInInches = 1.6433f;
                    //widthInInches = 1.9733f;
                    heightInInches = 0.5283f;
                    lineNumber = 19;
                    withSeparatedPercentage = false;
                    defaultCompression = 70;
                    ppivalue = 163;
                    SelectedLanguage = "English";
                    break;
                case "24MDH-0201-CW":
                    lineNumber = 19;
                    widthInInches = 1.773f;
                    heightInInches = 0.92756f;
                    withSeparatedPercentage = false;
                    ppivalue = 163;
                    break;

            }

            return new ArticleCompoConfig()
            {
                LineNumber = lineNumber,
                HeightInInches = heightInInches,
                WidthInches = widthInInches,
                WithSeparatedPercentage = withSeparatedPercentage,
                IsSimpleAdditional = IsSimpleAdditional,
                WidthAdditionalInInches = widthAdditionalInInches,
                PPI = ppivalue,
                SelectedLanguage = SelectedLanguage,
                DefaultCompresion = defaultCompression

            };

        }
    }
}
