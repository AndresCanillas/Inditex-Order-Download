using SmartdotsPlugins.Inditex.Models;

namespace SmartdotsPlugins.Compostion.Implementations
{
    public class ZaraArticleCompositionConfiguration
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
                case "D-CLZCALL001SUR":
                case "CLZCALL001SUR":
                case "D-CLZCALL001":
                case "CLZCALL001":

                    //font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta                          
                    //materialsFaces = new RibbonFace(font, 3.7700f, 0.9374f);
                    heightInInches = 0.9374f;
                    widthInInches = 3.7700f;
                    widthAdditionalInInches = 3.7700f;
                    lineNumber = 9;
                    IsSimpleAdditional = true;

                    break;


                case "XXXX - 60x25":

                    // font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                    // materialsFaces = new RibbonFace(font, 1.8503f, 0.9634f);  //46x23mm compo
                    heightInInches = 0.9634f;
                    widthInInches = 1.8503f;

                    //materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9685f, 0.9055f);  //50x23mm adicionales V (Cristina)
                    //CalculateCompositionSmall(compo, indexCompo, sectionLanguageSeparator, fiberLanguageSeparator, ciSeparator, ciLanguageSeparator, od, orderData, materialsFaces, artCode,9);
                    lineNumber = 10;
                    IsSimpleAdditional = false;
                    widthAdditionalInInches = 1.9685f;
                    break;

                case "XXXX-ALL 60x40":
                    //font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                    // materialsFaces = new RibbonFace(font, 1.9732f, 1.4350f);  //46x36.45mm compo
                    heightInInches = 1.4350f;
                    widthInInches = 1.9732f;

                    // materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.4440f);  //50.16x36.68mm adicionales V (Eric)
                    lineNumber = 15;
                    IsSimpleAdditional = false;
                    widthAdditionalInInches = 1.9748f;


                    break;



                // CORRECION MEDIDAS 2023-10
                // 60x40
                case "CLZCALL021":
                case "CLZCALL021SUR":
                case "D-CLZCALL021":
                case "D-CLZCALL021SUR":
                case "CLZCALL023":
                case "CLZCALL023SUR":
                case "D-CLZCALL023":
                case "D-CLZCALL023SUR":
                case "CLZCALL024":
                case "CLZCALL024SUR":
                case "D-CLZCALL024":
                case "D-CLZCALL024SUR":

                case "CLZCALL020":
                case "CLZCALL020SUR":
                case "CLZCALL022":
                case "CLZCALL022SUR":
                case "CLZCALL025":
                case "CLZCALL025SUR":
                case "CLZCALL026":
                case "CLZCALL026SUR":
                case "D-CLZCALL020":
                case "D-CLZCALL020SUR":
                case "D-CLZCALL022":
                case "D-CLZCALL022SUR":
                case "D-CLZCALL025":
                case "D-CLZCALL025SUR":
                case "D-CLZCALL026":
                case "D-CLZCALL026SUR":

                    // ribbon face size updated at 2023-10-11
                    //  font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiqueta
                    //44.96(1.7701)x37.79 (1.5665)mm  - height is ignored, line numbers is the real height
                    // se aumento de 1.7701 hasta 1.825f para incrementar el ancho del rectangulo en 5 pixeles
                    //  materialsFaces = new RibbonFace(font, 1.825f, 2f);
                    heightInInches = 2f;
                    widthInInches = 1.825f;

                    //  materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 1.5665f);  //50.16x37.79mm adicionales V (Eric)
                    lineNumber = 15;
                    //  IsSimpleAdditional = false;
                    widthAdditionalInInches = 1.9748f;
                    IsSimpleAdditional = false;

                    break;

                // 60x25
                case "D-CLZCALL027":
                case "CLZCALL027":
                case "D-CLZCALL027SUR":
                case "CLZCALL027SUR":

                case "D-CLZCALL028":
                case "CLZCALL028":
                case "D-CLZCALL028SUR":
                case "CLZCALL028SUR":
                case "D-CLZCALL029":
                case "CLZCALL029":
                case "D-CLZCALL029SUR":
                case "CLZCALL029SUR":
                case "D-CLZCALL030":
                case "CLZCALL030":
                case "D-CLZCALL030SUR":
                case "CLZCALL030SUR":
                case "D-CLZCALL031":
                case "CLZCALL031":
                case "D-CLZCALL031SUR":
                case "CLZCALL031SUR":
                case "D-CLZCALL032":
                case "CLZCALL032":
                case "D-CLZCALL032SUR":
                case "CLZCALL032SUR":
                case "D-CLZCALL033":
                case "CLZCALL033":
                case "D-CLZCALL033SUR":
                case "CLZCALL033SUR":


                    //  font = new Font("Arial Unicode MS", 6, FontStyle.Regular); // la fuente esta incrustada en la etiquetam
                    //  materialsFaces = new RibbonFace(font, 1.825f, 2f);  //46x23mm compo 
                    heightInInches = 2f;
                    widthInInches = 1.825f;

                    //   materialsFaces2 = new RibbonFace(materialsFaces.Font, 1.9748f, 0.9055f);  //50x23mm 1.9685f adicionales V (Cristina)
                    lineNumber = 9;
                    widthAdditionalInInches = 1.895f;
                    IsSimpleAdditional = false;

                    //  IsSimpleAdditional = false;
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
