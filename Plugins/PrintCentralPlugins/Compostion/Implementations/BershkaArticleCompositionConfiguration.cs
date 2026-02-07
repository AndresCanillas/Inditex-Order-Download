using SmartdotsPlugins.Inditex.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class BershkaArticleCompositionConfiguration
    {
        public ArticleCompoConfig Retrieve(string artCode)
        {
            float heightInInches = 0;
            float widthInInches = 0;
            float widthAdditionalInInches = 0;
            int lineNumber = 0;
            bool withSeparatePercent = true;
            bool IsSimpleAdditional = false;

            switch(artCode)
            {

                case "COMPO-BLACK":
                case "COMPO-WHITE":
                    lineNumber = 10;
                    heightInInches = 2f;
                    widthInInches = 1.825f;
                    widthAdditionalInInches = 1.825f;
                    IsSimpleAdditional = false;
                    break;
            }

            return new ArticleCompoConfig()
            {
                LineNumber = lineNumber,
                HeightInInches = heightInInches,
                WidthInches = widthInInches,
                WithSeparatedPercentage = withSeparatePercent,
                IsSimpleAdditional = IsSimpleAdditional,
                WidthAdditionalInInches = widthAdditionalInInches
            };

        }
    }
}
